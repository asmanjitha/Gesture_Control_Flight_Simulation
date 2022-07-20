// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoachAiEngine.Analytics;
using CoachAiEngine.Android;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
#if COACH_AI_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
#endif

namespace CoachAiEngine {

    [AddComponentMenu("Coach-AI Engine/Component Controller")]
    public class CoachComponentController : CoachBaseController {

        [Header("Coach-AI Component Runtime Configuration")]
        [Tooltip("Whether to start the components when the game object was loaded (on Start).")]
        public bool StartComponentsOnStartup = true;

        [Space(15)]
        [Tooltip("List of Coach-AI components to start")]
        public List<CoachComponent> Components;

        [Tooltip("List of events to subscribe to")]
        public List<EventSubscription> EventSubscriptions;

        [Space(15)]
        [Tooltip("Handler that is invoked whenever any event is observed in this component runtime.")]
        public UnityEvent<PublicEvent> OnAnyEvent;

        private int _oldScreenSleep;
        private bool _screenSleepSet;
        private ConcurrentQueue<Action> _mainQueue = new ConcurrentQueue<Action>();

        public CoachComponentRuntime Runtime { get; private set; }

        private void Start() {
            if (StartComponentsOnStartup) {
                StartCoroutine(StartComponents());
            }
        }

        [Conditional("UNITY_ANDROID")]
        private void AddAndroidArBridge() {
#if COACH_AI_AR_FOUNDATION
            if (automaticallyAddAndroidARBridgeIfNecessary && FindObjectOfType<CoachAndroidArBridge>(true) == null) {
                gameObject.AddComponent<CoachAndroidArBridge>();
            }
#endif
        }

        private IEnumerator StartComponents() {
#if COACH_AI_AR_FOUNDATION
            // Delay startup of components until AR is ready. This is, in particular,
            // necessary on iOS as otherwise swizzling will fail.
            var arPublisherToBeLoaded = Components.Any(component => component is ARPublisher);
            if (arPublisherToBeLoaded) {
                AddAndroidArBridge();
                var enumerator = ARSessionUtils.WaitForARSession();
                if (enumerator == null) {
                    Debug.LogWarning($"{nameof(CoachComponentController)}: " +
                                     "No active AR session found, this will cause issues.");
                } else {
                    yield return enumerator;
                }
            }
#endif
            Runtime = CoachComponentRuntime.StartComponentRuntime(Components, this);

            yield return new WaitUntil(() => Runtime.RuntimeState == CoachComponentRuntime.State.Running);

            foreach( var subscription in EventSubscriptions) {
                var subscriptionId = Runtime.Subscribe(subscription.EventId, @event => {
                    subscription.OnEvent?.Invoke(@event);
                    OnAnyEvent.Invoke(@event);
                });
                if (subscriptionId < 0) {
                    Debug.LogWarning($"{nameof(CoachComponentController)}: Subscription to {subscription.EventId} failed");
                }
            }

            if (disableScreenSleep) {
                _screenSleepSet = true;
                _oldScreenSleep = ScreenUtils.AllowSleep(false);
            }
        }

        private void OnDisable() {
            Runtime?.Pause();

            if (disableScreenSleep && _screenSleepSet) {
                ScreenUtils.AllowSleep(_oldScreenSleep);
            }
        }

        private void OnEnable() {
            if (Runtime?.RuntimeState == CoachComponentRuntime.State.Paused) {
                Runtime.Resume();

                if (disableScreenSleep && _screenSleepSet) {
                    ScreenUtils.AllowSleep(false);
                }
            }
        }

        private void OnDestroy() {
            Runtime?.Stop();

            if (disableScreenSleep && _screenSleepSet) {
                ScreenUtils.AllowSleep(false);
            }
        }

        private void Update() {
            while (_mainQueue.TryDequeue(out var action)) {
                action.Invoke();
            }
        }

        public void PushToMain(Action action) {
            _mainQueue.Enqueue(action);
        }
    }
}
