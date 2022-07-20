/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#if PLATFORM_IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using UnityEngine;

namespace CoachAiEngine.Ios {
    public partial class ComponentRuntime {
        private static class Registry {
            private static readonly Dictionary<int, IComponentRuntimeCallbacks> Register = new Dictionary<int, IComponentRuntimeCallbacks>();
            private static int lastHandle = 0;

            private static int NextHandle() => Interlocked.Increment(ref lastHandle);

            public static int RegisterRuntime(IComponentRuntimeCallbacks runtime) {
                var handle = NextHandle();
                Register.Add(handle, runtime);
                return handle;
            }

            public static void DeregisterRuntime(int handle) => Register.Remove(handle);

            public static IComponentRuntimeCallbacks Get(int handle) => Register[handle];

            public static bool TryGet(int handle, out IComponentRuntimeCallbacks runtime) =>
                Register.TryGetValue(handle, out runtime);
        }

        private delegate void OnComponentLoadedCallback(int handle, IntPtr componentIdPtr, bool success);

        [MonoPInvokeCallback(typeof(OnComponentLoadedCallback))]
        private static void OnComponentLoaded(int handle, IntPtr componentIdPtr, bool success) {
            if (!Registry.TryGet(handle, out var runtime)) {
                Debug.LogError($"Trying to access invalid component runtime {handle}");
                return;
            }

            var componentId = Marshal.PtrToStringAuto(componentIdPtr);
            runtime.OnLoaded(componentId, success);
        }

        private delegate void OnComponentConfiguredCallback(int handle, IntPtr componentIdPtr, bool success);

        [MonoPInvokeCallback(typeof(OnComponentConfiguredCallback))]
        private static void OnComponentConfigured(int handle, IntPtr componentIdPtr, bool success) {
            if (!Registry.TryGet(handle, out var runtime)) {
                Debug.LogError($"Trying to access invalid component runtime {handle}");
                return;
            }
            var componentId = Marshal.PtrToStringAuto(componentIdPtr);
            runtime.OnConfigured(componentId, success);
        }

        private delegate void OnEventCallback(int handle, IntPtr @event);

        [MonoPInvokeCallback(typeof(OnEventCallback))]
        private static void OnEvent(int handle, IntPtr json) {
            if (!Registry.TryGet(handle, out var runtime)) {
                Debug.LogError($"Trying to access invalid component runtime {handle}");
                return;
            }
            var @event = Marshal.PtrToStringAuto(json);
            runtime.OnEvent(@event);
        }

        [DllImport("__Internal")]
        private static extern void coach_cr_load(
            int handle,
            string component,
            string config,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnComponentLoadedCallback callback,
            int runtimeHandle
        );

        [DllImport("__Internal")]
        private static extern void coach_cr_configure(
            int handle,
            string component,
            string config,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnComponentConfiguredCallback callback,
            int runtimeHandle
        );

        [DllImport("__Internal")]
        private static extern void coach_cr_resume(int handle);

        [DllImport("__Internal")]
        private static extern void coach_cr_pause(int handle);

        [DllImport("__Internal")]
        private static extern void coach_cr_dispose(int handle);

        [DllImport("__Internal")]
        private static extern int coach_cr_subscribe(
            int handle,
            string eventIdentifier,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnEventCallback callback,
            int runtimeHandle
        );

        [DllImport("__Internal")]
        private static extern void coach_cr_unsubscribe(int handle, long subscriptionId);

        [DllImport("__Internal")]
        private static extern int coach_cr_create();
    }
}
#endif
