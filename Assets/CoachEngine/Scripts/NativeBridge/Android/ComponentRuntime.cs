/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#if PLATFORM_ANDROID

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace CoachAiEngine.Android {

    internal class ComponentRuntime : IComponentRuntime {
        private const string ComponentRuntimeClassName = "com.coachai.engine.unity.ComponentRuntime";

        private static IntPtr runtimeClassRef = IntPtr.Zero;
        private static IntPtr loadMethod = IntPtr.Zero;
        private static IntPtr configureMethod = IntPtr.Zero;
        private static IntPtr startMethod = IntPtr.Zero;
        private static IntPtr pauseMethod = IntPtr.Zero;
        private static IntPtr disposeMethod = IntPtr.Zero;
        private static IntPtr subscribeMethod = IntPtr.Zero;
        private static IntPtr unsubscribeMethod = IntPtr.Zero;

        private IntPtr _runtime = IntPtr.Zero;

        public ComponentRuntime(IComponentRuntimeCallbacks callbackHandler) {
            Debug.Log("Starting ComponentRuntime");
            InitRefs();
            NewRuntime(callbackHandler);
        }

        ~ComponentRuntime() {
            Stop();
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Load"/>
         */
        public void Load(CoachComponent component) {
            var json = JsonConvert.SerializeObject(component.Parameters);
            var args = new[] {
                new jvalue {l = AndroidJNI.NewString(component.ComponentId)},
                new jvalue {l = AndroidJNI.NewString(json)}
            };
            AndroidJNI.CallVoidMethod(_runtime, loadMethod, args);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Configure"/>
         */
        public void Configure(CoachComponent component) {
            var json = JsonConvert.SerializeObject(component.Parameters);
            var args = new[] {
                new jvalue {l = AndroidJNI.NewString(component.ComponentId)},
                new jvalue {l = AndroidJNI.NewString(json)}
            };
            AndroidJNI.CallVoidMethod(_runtime, configureMethod, args);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Start"/>
         */
        public void Start() {
            AndroidJNI.CallVoidMethod(_runtime, startMethod, Array.Empty<jvalue>());
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Pause"/>
         */
        public void Pause() {
            AndroidJNI.CallVoidMethod(_runtime, pauseMethod, Array.Empty<jvalue>());
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Stop"/>
         */
        public void Stop() {
            if (_runtime == IntPtr.Zero) return;
            AndroidJNI.CallVoidMethod(_runtime, disposeMethod, Array.Empty<jvalue>());
            AndroidJNI.DeleteGlobalRef(_runtime);
            _runtime = IntPtr.Zero;
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Subscribe"/>
         */
        public long Subscribe(string eventId) {
            if (_runtime == IntPtr.Zero) {
                Debug.LogError($"{nameof(ComponentRuntime)}: runtime is disposed.");
                return -1;
            }

            var args = new[] {
                new jvalue {l = AndroidJNI.NewString(eventId)}
            };
            return AndroidJNI.CallLongMethod(_runtime, subscribeMethod, args);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Unsubscribe"/>
         */
        public void Unsubscribe(long subscriptionId) {
            var args = new[] {
                new jvalue {j = subscriptionId}
            };
            AndroidJNI.CallVoidMethod(_runtime, unsubscribeMethod, args);
        }

        private static void InitRefs() {
            if (runtimeClassRef != IntPtr.Zero) return;

            var className = ComponentRuntimeClassName.Replace('.', '/');
            runtimeClassRef = AndroidJNI.NewGlobalRef(AndroidJNI.FindClass(className));

            loadMethod = AndroidJNI.GetMethodID(
                runtimeClassRef, "load", "(Ljava/lang/String;Ljava/lang/String;)V"
            );
            configureMethod = AndroidJNI.GetMethodID(
                runtimeClassRef, "reconfigure", "(Ljava/lang/String;Ljava/lang/String;)V"
            );
            startMethod = AndroidJNI.GetMethodID(runtimeClassRef, "start", "()V");
            pauseMethod = AndroidJNI.GetMethodID(runtimeClassRef, "pause", "()V");
            disposeMethod = AndroidJNI.GetMethodID(runtimeClassRef, "dispose", "()V");
            subscribeMethod = AndroidJNI.GetMethodID(runtimeClassRef, "subscribe", "(Ljava/lang/String;)J");
            unsubscribeMethod = AndroidJNI.GetMethodID(runtimeClassRef, "unsubscribe", "(J)V");
        }

        private void NewRuntime(IComponentRuntimeCallbacks callbackHandler) {
            var args = new object[] {new ComponentRuntimeCallbackProxy(callbackHandler)};
            var instance = new AndroidJavaObject(ComponentRuntimeClassName, args);
            _runtime = AndroidJNI.NewGlobalRef(instance.GetRawObject());
        }

        private class ComponentRuntimeCallbackProxy : AndroidJavaProxy, IComponentRuntimeCallbacks {
            private readonly IComponentRuntimeCallbacks _callbackHandler;

            public ComponentRuntimeCallbackProxy(IComponentRuntimeCallbacks callbackHandler)
                : base($"{ComponentRuntimeClassName}${nameof(IComponentRuntimeCallbacks)}") {
                _callbackHandler = callbackHandler;
            }

            public void OnLoaded(string componentId, bool success)
                => _callbackHandler.OnLoaded(componentId, success);

            public void OnConfigured(string componentId, bool success)
                => _callbackHandler.OnConfigured(componentId, success);

            public void OnEvent(string @event) => _callbackHandler.OnEvent(@event);
        }
    }
}
#endif
