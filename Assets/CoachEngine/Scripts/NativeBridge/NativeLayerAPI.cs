/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System;
using System.Collections.Generic;
using CoachAiEngine;
using UnityEngine;

#if PLATFORM_ANDROID
using CoachAiEngine.Android;
#elif PLATFORM_IOS
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
#endif

namespace CoachAiEngine {

    public static class NativeLayerAPI {

        public class ActivityServiceConfig {
            public string Identifier;
            public Dictionary<string, object> Parameters;
            public string Variant;

            public ActivityServiceConfig(string identifier, Dictionary<string, object> parameters, string variant) {
                Identifier = identifier;
                Parameters = parameters;
                Variant = variant;
            }
        }

        public class IOSNativeAPI {
#if PLATFORM_IOS
            [DllImport("__Internal")]
            public static extern void setupIOSUnityBridge(string logLevel);

            [DllImport("__Internal")]
            public static extern void startActivity(string activityConfigJson);

            [DllImport("__Internal")]
            public static extern void stopActivity();

            [DllImport("__Internal")]
            public static extern void subscribePublicEvent(string events);

            [DllImport("__Internal")]
            public static extern void unsubscribePublicEvent(string events);

            [DllImport("__Internal")]
            public static extern void pollEvents(out IntPtr events, out int count);

            [DllImport("__Internal")]
            public static extern void subscribeWorldObjectEvents(int eventType);

            [DllImport("__Internal")]
            public static extern void unsubscribeWorldObjectEvents(int eventType);

            [DllImport("__Internal")]
            public static extern int getThermalState();

            [DllImport("__Internal")]
            public static extern string getBuildNumber();

            [DllImport("__Internal")]
            public static extern string getThermalStateName();

            [DllImport("__Internal")]
            public static extern void quit();

            [DllImport("__Internal")]
            public static extern bool isSdkInitialized();

            [DllImport("__Internal")]
            public static extern bool isSdkActivityInitialized();

            [DllImport("__Internal")]
            public static extern void coach_activity_take_decision(int requestId, string json);

            [DllImport("__Internal")]
            public static extern void coach_activity_send_command(string command, string json);

            [DllImport("__Internal")]
            public static extern void coach_activity_subscribe_metric(string metricId);

            [DllImport("__Internal")]
            public static extern void coach_activity_fulfill_requirement(int requirementId, string json);
#endif
        }

        /**
         * <returns>Returns true if the native part of Coach-Ai Engine has been initialized.</returns>
         * <remarks>In Unity Editor this will always return <c>true</c>.</remarks>
         */
        public static bool IsSdkInitialized() {
#if   UNITY_EDITOR
            return true;
#elif PLATFORM_IOS
            return IOSNativeAPI.isSdkInitialized();
#elif PLATFORM_ANDROID
            return JNIAndroidService.SdkSetupDone;
#endif
        }

        /**
         * <summary>
         * Checks if a Coach-Ai activity has been initialized.
         * This doesn't necessarily mean the activity is also running.
         * </summary>
         * <returns>Returns true is a Coach-Ai activity has been initialized.</returns>
         * <remarks>In Unity Editor this will always return <c>true</c>.</remarks>
         */
        public static bool IsSdkActivityInitialized() {
#if   UNITY_EDITOR
            return true;
#elif PLATFORM_ANDROID
            return JNIAndroidService.IsSdkActivityInitialized();
#elif PLATFORM_IOS
            // this only checks if a handler is present!
            return IOSNativeAPI.isSdkActivityInitialized();
#endif
        }

        /**
         * <summary>Starts the native part of Coach-Ai Engine.</summary>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void SetupNativeBridge(LogLevel logLevel) {
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            Debug.Log("Initializing Coach-Ai Engine for Android");
            JNIAndroidService.StartSdk(logLevel);
#elif PLATFORM_IOS
            Debug.Log("Initializing Coach-AI Engine for iOS");
            IOSNativeAPI.setupIOSUnityBridge(logLevel.ToString());
#endif
        }

        /**
         * <summary>Starts a Coach-Ai activity. Only one activity can be active at any time.</summary>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void StartActivity(
            ActivityServiceConfig activityServiceConfig, IActivityCallbacks callbackHandler
        ) {
            Debug.Log($"Starting Coach-AI activity {activityServiceConfig.Identifier}");
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            JNIAndroidService.StartActivity(activityServiceConfig, callbackHandler);
#elif PLATFORM_IOS
            CoachActivityRuntime.callbackHandler = callbackHandler;
            var json = JsonConvert.SerializeObject(activityServiceConfig);
            IOSNativeAPI.startActivity(json);
#endif
        }

        /**
         * <summary>Stops a Coach-Ai activity.</summary>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void StopActivity() {
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            JNIAndroidService.StopActivity();
#elif PLATFORM_IOS
            IOSNativeAPI.stopActivity();
#endif
        }

        /**
         * <summary>Subscribe to events published by the current Coach-Ai activity.</summary>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void SubscribePublicEvents(List<string> events) {
            Debug.Log($"NativeAPI: Subscribing to {events.Count} public events");
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            JNIAndroidService.SubscribePublicEvents(events);
#elif PLATFORM_IOS
            foreach (var @event in events) {
                IOSNativeAPI.subscribePublicEvent(@event);
            }
#endif
        }

        /**
         * <summary>Unsubscribe from events published by the current Coach-Ai activity.</summary>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void UnsubscribePublicEvents(List<string> events) {
            Debug.Log($"NativeAPI: Unsubscribing from {events.Count} public events");
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            JNIAndroidService.UnsubscribePublicEvents(events);
#elif PLATFORM_IOS
            foreach (var @event in events) {
                IOSNativeAPI.unsubscribePublicEvent(@event);
            }
#endif
        }

        /**
         * <summary>Poll events from the currently running Coach-Ai activity.</summary>
         * <remarks>In Unity Editor this always returns an empty array.</remarks>
         */
        public static string[] PollEvents() {
#if UNITY_EDITOR
            return Array.Empty<string>();
#elif PLATFORM_ANDROID
            return JNIAndroidService.PollEvents();
#elif PLATFORM_IOS
            IOSNativeAPI.pollEvents(out var pEvents, out var count);
            var eventPointers = new IntPtr[count];
            var events = new string[count];
            Marshal.Copy(pEvents, eventPointers, 0, count);

            for (var i = 0; i < count; i++) {
                events[i] = Marshal.PtrToStringAnsi(eventPointers[i]);
                Marshal.FreeCoTaskMem(eventPointers[i]);
            }
            Marshal.FreeCoTaskMem(pEvents);
            return events;
#endif
        }

        /**
         * <summary>Obtain the current thermal state of the device.</summary>
         * <remarks>In Android and Unity Editor this always returns <c>0</c>.</remarks>
         */
        public static int GetThermalState() {
            Debug.Log("NativeAPI: Checking thermal state on device");
#if   UNITY_EDITOR
            return 0;
#elif PLATFORM_ANDROID
            return 0;
#elif PLATFORM_IOS
            return IOSNativeAPI.getThermalState();
#endif
        }

        /**
         * <summary>Obtain the human-readable current thermal state of the device.</summary>
         * <remarks>In Android and Unity Editor this always returns <c>normal</c>.</remarks>
         */
        public static string GetThermalStateName() {
            Debug.Log("NativeAPI: Checking thermal state on device");
#if UNITY_EDITOR
            return "normal";
#elif PLATFORM_ANDROID
            return "normal";
#elif PLATFORM_IOS
            return IOSNativeAPI.getThermalStateName();
#endif
        }

        public static string GetBuildNumber() {
            Debug.Log("NativeAPI: Checking build number from device");
#if UNITY_EDITOR
            return "";
#elif PLATFORM_ANDROID
            return "";
#elif PLATFORM_IOS
            return IOSNativeAPI.getBuildNumber();
#endif
        }

        public static void Quit() {
#if UNITY_EDITOR
            Application.Quit();
#elif PLATFORM_ANDROID
            Application.Quit();
#elif PLATFORM_IOS
            IOSNativeAPI.quit();
#endif
        }

        /**
         * <summary>Subscribe to world object events of the given type(s).</summary>
         * <param name="eventType">A flag containing the type(s) of events to subscribe to.</param>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void SubscribeWorldObjectEvents(WorldObjectEvent.EventType eventType) {
#if UNITY_EDITOR
            // nothing
#elif PLATFORM_ANDROID
            JNIAndroidService.SubscribeWorldObjectEvents(eventType);
#elif PLATFORM_IOS
            IOSNativeAPI.subscribeWorldObjectEvents((int) eventType);
#endif
        }

        /**
         * <summary>Unsubscribe from world object events of the given type(s).</summary>
         * <param name="eventType">A flag containing the type(s) of events to unsubscribe from.</param>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void UnsubscribeWorldObjectEvents(WorldObjectEvent.EventType eventType) {
#if   UNITY_EDITOR
            // nothing
#elif PLATFORM_ANDROID
            JNIAndroidService.UnsubscribeWorldObjectEvents(eventType);
#elif PLATFORM_IOS
            IOSNativeAPI.unsubscribeWorldObjectEvents((int) eventType);
#endif
        }

        /**
         * <summary>Responds to a decision request received from a Coach-Ai activity.</summary>
         * <param name="requestId">The id of the request this decision is meant for.</param>
         * <param name="data">The data to attach to the reply. Must be serializable.</param>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void TakeDecision(int requestId, object data) {
#if   UNITY_EDITOR
            // nothing
#elif PLATFORM_ANDROID
            JNIAndroidService.TakeDecision(requestId, data);
#elif PLATFORM_IOS
            var json = JsonConvert.SerializeObject(data);
            IOSNativeAPI.coach_activity_take_decision(requestId, json);
#endif
        }

        public static void FulfillRequirement(int requirementId, object data) {
#if   UNITY_EDITOR
// not required
#elif PLATFORM_ANDROID
            JNIAndroidService.FulfillRequirement(requirementId, data);
#elif PLATFORM_IOS
            var json = JsonConvert.SerializeObject(data);
            IOSNativeAPI.coach_activity_fulfill_requirement(requirementId, json);
#endif
        }

        /**
         * <summary>Sends a command to the currently active Coach-Ai activity.</summary>
         * <param name="command">The id of the command to send.</param>
         * <param name="data">The the serialized command data to send.</param>
         * <remarks>In Unity Editor this is a noop.</remarks>
         */
        public static void SendCommand(string command, object data) {
#if   UNITY_EDITOR
            // not required
#elif PLATFORM_ANDROID
            JNIAndroidService.SendCommand(command, data);
#elif PLATFORM_IOS
            var json = JsonConvert.SerializeObject(data);
            IOSNativeAPI.coach_activity_send_command(command, json);
#endif
        }

        /**
         * <summary>Subscribes to metrics generated by the currently active Coach-Ai activity.</summary>
         * <param name="metricIds">The ids of the metrics to subscribe to.</param>
         * <remarks>
         * Unsubscribing is currently impossible and requires a complete restart of the activity.
         * <para>In Unity Editor this is a noop.</para>
         * </remarks>
         */
        public static void SubscribeMetrics(string[] metricIds) {
#if UNITY_EDITOR
            // nothing
#elif PLATFORM_ANDROID
            JNIAndroidService.SubscribeMetrics(metricIds);
#elif PLATFORM_IOS
            foreach (var metricId in metricIds) {
                IOSNativeAPI.coach_activity_subscribe_metric(metricId);
            }
#endif
        }
    }
}
