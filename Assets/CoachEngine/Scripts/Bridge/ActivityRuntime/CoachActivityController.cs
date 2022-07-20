// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

#if COACH_AI_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
#endif

#if PLATFORM_ANDROID
using CoachAiEngine.Android;
#endif

namespace CoachAiEngine {

    public abstract class CoachActivityController : CoachBaseController {
        [Header("Coach-AI Activity")] [Space(15)] [Tooltip("Activity definition object")]
        public CoachActivity CoachActivityConfiguration;

        [Tooltip("Whether to start the activity when the game object was loaded (on Start)")]
        public bool StartActivityOnStartup = true;

        [Tooltip("If you are using AR in the activity, this should be activated. If the activity is started " +
                 "on startup, use this flag to control whether start is delayed until the AR Session is ready")]
        public bool DelayStartupUntilARIsReady = true;

        [Space(15)] [Header("Activity Lifecycle")]
        public UnityEvent<string> OnActivityStarted;

        public UnityEvent OnActivityStopped;

        /// <summary>
        /// Is triggered with the raw (json) events as coming directly from the Coach-AI engine.
        /// This can, for example, be used to implement recording functionality.
        /// </summary>
        public UnityEvent<string> OnRawEvent;

        private int _oldScreenSleep;
        private bool _screenSleepSet;
        protected bool LifecycleHandlersAdded { private set; get; }

        public CoachActivityRuntime Runtime { get; private set; }

        public bool IsInitialized => Runtime.State > CoachActivityRuntime.ActivityState.Initializing;

        #region Lifecycle

        protected virtual void Start() {
            Runtime = new CoachActivityRuntime();
            if (StartActivityOnStartup) {
                StartCoroutine(StartActivity());
            }
        }

        protected abstract void InitializeSubscriptions();

        protected virtual void RegisterLifecycleEventHandlers() {
            if (LifecycleHandlersAdded) return;
            Runtime.OnFinish += _ => OnActivityStopped.Invoke();
            Runtime.OnFinish += _ => RestoreScreenSleep();
            Runtime.OnError += error => Debug.LogError(error.Message, this);
            Runtime.OnInit += InitializeSubscriptions;
            Runtime.OnInit += () => {
                Debug.Log($"Activity {CoachActivityConfiguration.ActivityId} initialized!");
                OnActivityStarted.Invoke(CoachActivityConfiguration.ActivityId);
            };
            LifecycleHandlersAdded = true;
        }

        [Conditional("PLATFORM_ANDROID")]
        private void AddCoachAndroidBridge() {
            Debug.Log("Adding Coach AR Bridge");
#if COACH_AI_AR_FOUNDATION && PLATFORM_ANDROID
            if (!automaticallyAddAndroidARBridgeIfNecessary) return;
            var arSessionPresent = FindObjectOfType<ARSession>(true) != null;
            var bridgeAbsent = FindObjectOfType<CoachAndroidArBridge>(true) == null;
            if (arSessionPresent && bridgeAbsent) {
                gameObject.AddComponent<CoachAndroidArBridge>();
            }
#endif
        }

        public void StopActivity() {
            if (Runtime.State == CoachActivityRuntime.ActivityState.Running) {
                Runtime.StopSdkActivity();
            }
        }

        public void StartActivityAsync() {
            if (Runtime.State == CoachActivityRuntime.ActivityState.Stopped) {
                StartCoroutine(StartActivity());
            }
        }

        public IEnumerator StartActivity() {
            RegisterLifecycleEventHandlers();
            AddCoachAndroidBridge();
#if COACH_AI_AR_FOUNDATION
            if (DelayStartupUntilARIsReady) {
                // TODO: Check whether activity actually requires AR
                var enumerator = ARSessionUtils.WaitForARSession();
                if (enumerator == null) {
                    Debug.LogWarning($"{nameof(CoachActivityController)}: " +
                                     "Not waiting for an ARSession can cause undefined behaviour in activity");
                } else {
                    yield return enumerator;
                }
            }
#endif
            Debug.Log("Start SDK Activity");

            Runtime.StartSdkActivity(CoachActivityConfiguration);
            if (disableScreenSleep) {
                _screenSleepSet = true;
                _oldScreenSleep = ScreenUtils.AllowSleep(false);
            }
#if !COACH_AI_AR_FOUNDATION
            yield break;
#endif
        }

        private void OnApplicationQuit() => StopActivity();

        private void OnDestroy() {
            StopActivity();
            RestoreScreenSleep();
        }

        protected virtual void FixedUpdate() {
            if (Runtime.State != CoachActivityRuntime.ActivityState.Running) return;

            foreach (var @event in Runtime.PollEvents()) {
                OnRawEvent.Invoke(@event);
            }
        }

        public virtual void LateUpdate() {
            if (Runtime.State != CoachActivityRuntime.ActivityState.Running) return;

            Runtime.UpdatePublicEvents();
        }

        #endregion

        /// <summary>
        /// Allows to send a command to the activity
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="data"></param>
        public void SendCommand(string commandName, object data = null) {
            Runtime.SendCommand(commandName, data);
        }

        private void RestoreScreenSleep() {
            if (disableScreenSleep && _screenSleepSet) {
                ScreenUtils.AllowSleep(_oldScreenSleep);
            }
        }
    }
}
