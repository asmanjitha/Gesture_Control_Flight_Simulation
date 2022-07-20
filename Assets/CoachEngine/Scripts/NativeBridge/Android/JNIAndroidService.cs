/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#if PLATFORM_ANDROID
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CoachAiEngine.Android {
    /// <summary>
    /// Proxy to the Kotlin CoachActivityController object
    /// </summary>
    public static class JNIAndroidService {
        // CoachActivityController object and method ids
        private const string ActivityControllerClassName = "com.coachai.engine.unity.CoachActivityController";
        private static readonly AndroidJavaObject ActivityController;

        private static readonly IntPtr IsActivityInitializedMethod;
        private static readonly IntPtr PollEventsMethod;
        private static readonly IntPtr SendCommandMethod;
        private static readonly IntPtr SubscribeMetricsMethod;
        private static readonly IntPtr SubscribePublicEventsMethod;
        private static readonly IntPtr SubscribeWorldObjectEventsMethod;
        private static readonly IntPtr TakeDecisionMethod;
        private static readonly IntPtr FulfillRequirementMethod;
        private static readonly IntPtr UnsubscribePublicEventsMethod;
        private static readonly IntPtr UnsubscribeWorldObjectEventsMethod;

        // CoachSdk object
        private const string SdkClassName = "com.coachai.engine.unity.CoachSdk";

        public static bool SdkSetupDone { get; private set; }

        private static readonly IntPtr InstancePtr;

        static JNIAndroidService() {
            // look-up ActivityController object and method ids
            ActivityController = Kotlin.GetObject(ActivityControllerClassName);
            InstancePtr = ActivityController.GetRawObject();
            IsActivityInitializedMethod = GetMethodId(ActivityController, "isActivityInitialized", "()Z");
            PollEventsMethod = GetMethodId(ActivityController, "pollEvents", "()[Ljava/lang/String;");
            SendCommandMethod = GetMethodId(
                ActivityController, "sendCommand", "(Ljava/lang/String;Ljava/lang/String;)V");
            SubscribeMetricsMethod = GetMethodId(
                ActivityController, "subscribeActivityMetric", "([Ljava/lang/String;)V");
            SubscribePublicEventsMethod = GetMethodId(
                ActivityController, "subscribePublicEvents", "([Ljava/lang/String;)V");
            SubscribeWorldObjectEventsMethod = GetMethodId(
                ActivityController, "subscribeWorldObjectEvents", "(I)V");
            TakeDecisionMethod = GetMethodId(ActivityController, "takeDecision", "(ILjava/lang/String;)V");
            FulfillRequirementMethod = GetMethodId(ActivityController, "fulfillRequirement", "(ILjava/lang/String;)V");
            UnsubscribePublicEventsMethod = GetMethodId(
                ActivityController, "unsubscribePublicEvents", "([Ljava/lang/String;)V");
            UnsubscribeWorldObjectEventsMethod = GetMethodId(
                ActivityController, "unsubscribeWorldObjectEvents", "(I)V");
        }

        private static IntPtr GetMethodId(AndroidJavaObject obj, string name, string sig) {
            return AndroidJNI.GetMethodID(obj.GetRawClass(), name, sig);
        }

        public static void StartSdk(LogLevel logLevel = LogLevel.Error) {
            if (SdkSetupDone) return;
            var sdk = Kotlin.GetObject(SdkClassName);
            var level = logLevel.ToString().ToUpperInvariant();
            const string mode = "PRODUCTION";
            sdk.Call("startSdk", level, mode);
            SdkSetupDone = true;
        }

        public static void StartActivity(
            NativeLayerAPI.ActivityServiceConfig serviceConfig, IActivityCallbacks callbackHandler
        ) {
            if (!SdkSetupDone) {
                Debug.LogWarning("Trying to start an activity before starting the sdk!");
                StartSdk();
            }
            Debug.Log("StartActivity - SDK Activity called");

            var json = JsonConvert.SerializeObject(serviceConfig);
            var proxy = new ActivityCallbackProxy(callbackHandler);
            ActivityController.Call("startActivity", json, proxy);
        }

        public static void StopActivity() {
            ActivityController.Call("stopActivity");
        }

        public static void SubscribePublicEvents(List<string> events) {
            var args = AndroidJNIHelper.CreateJNIArgArray(new object[] { events.ToArray() });
            AndroidJNI.CallVoidMethod(InstancePtr, SubscribePublicEventsMethod, args);
        }

        public static void UnsubscribePublicEvents(List<string> events) {
            var args = AndroidJNIHelper.CreateJNIArgArray(new object[] { events.ToArray() });
            AndroidJNI.CallVoidMethod(InstancePtr, UnsubscribePublicEventsMethod, args);
        }

        public static string[] PollEvents() {
            var intPtr = AndroidJNI.CallObjectMethod(InstancePtr, PollEventsMethod, Array.Empty<jvalue>());
            return AndroidJNIHelper.ConvertFromJNIArray<string[]>(intPtr);
        }

        public static void SubscribeWorldObjectEvents(WorldObjectEvent.EventType eventType) {
            var args = AndroidJNIHelper.CreateJNIArgArray(new object[] { (int) eventType });
            AndroidJNI.CallVoidMethod(InstancePtr, SubscribeWorldObjectEventsMethod, args);
        }

        public static void UnsubscribeWorldObjectEvents(WorldObjectEvent.EventType eventType) {
            var args = AndroidJNIHelper.CreateJNIArgArray(new object[] { (int) eventType });
            AndroidJNI.CallVoidMethod(InstancePtr, UnsubscribeWorldObjectEventsMethod, args);
        }

        public static bool IsSdkActivityInitialized() {
            return AndroidJNI.CallBooleanMethod(InstancePtr, IsActivityInitializedMethod, Array.Empty<jvalue>());
        }

        public static void TakeDecision(int requestId, object data) {
            var json = JsonConvert.SerializeObject(data);
            var args = new[] {
                new jvalue {i = requestId},
                new jvalue {l = AndroidJNI.NewString(json)}
            };
            Debug.Log($"JNI: {nameof(TakeDecision)}n");
            AndroidJNI.CallVoidMethod(InstancePtr, TakeDecisionMethod, args);
        }

        public static void FulfillRequirement(int requestId, object data) {
            var json = JsonConvert.SerializeObject(data);
            var args = new[] {
                new jvalue {i = requestId},
                new jvalue {l = AndroidJNI.NewString(json)}
            };
            Debug.Log($"JNI: {nameof(FulfillRequirement)}n");
            AndroidJNI.CallVoidMethod(InstancePtr, FulfillRequirementMethod, args);
        }

        public static void SendCommand(string command, object data) {
            var json = JsonConvert.SerializeObject(data);
            var args = new[] {
                new jvalue {l = AndroidJNI.NewString(command)},
                new jvalue {l = AndroidJNI.NewString(json)}
            };
            Debug.Log($"JNI: {nameof(SendCommand)} command={command}");
            AndroidJNI.CallVoidMethod(InstancePtr, SendCommandMethod, args);
        }

        public static void SubscribeMetrics(string[] metricIds) {
            var args = new[] {
                new jvalue {l = AndroidJNIHelper.ConvertToJNIArray(metricIds)}
            };
            Debug.Log($"JNI: {nameof(SubscribeMetricsMethod)} metric count={metricIds.Length}");
            AndroidJNI.CallVoidMethod(InstancePtr, SubscribeMetricsMethod, args);
        }
    }

    internal class ActivityCallbackProxy : AndroidJavaProxy, IActivityCallbacks {
        private const string AndroidClassName = "com.coachai.engine.unity.IActivityCallbacks";
        private readonly IActivityCallbacks _callbackHandler;

        public ActivityCallbackProxy(IActivityCallbacks callbackHandler) : base(AndroidClassName) {
            _callbackHandler = callbackHandler;
        }

        public void OnDecisionRequest(int requestId, string json) =>
            _callbackHandler.OnDecisionRequest(requestId, json);

        public void OnFinish(int finishFlag) => _callbackHandler.OnFinish(finishFlag);

        public void OnInit() => _callbackHandler.OnInit();

        public void OnError(string json) => _callbackHandler.OnError(json);

        public void OnRequire(int requirementId, string json) =>
            _callbackHandler.OnRequire(requirementId, json);
    }
}
#endif
