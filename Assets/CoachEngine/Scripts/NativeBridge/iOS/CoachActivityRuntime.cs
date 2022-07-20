// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace CoachAiEngine {

#if UNITY_IOS
    public partial class CoachActivityRuntime {
        private delegate void OnDecisionRequestT(int requestId, IntPtr json);
        private delegate void OnFinishT(int flag);
        private delegate void OnInitT();
        private delegate void OnErrorT(IntPtr json);
        private delegate void OnRequireT(int requirementId, IntPtr type);

        internal static IActivityCallbacks callbackHandler;

        [DllImport("__Internal")]
        private static extern void coach_activity_initialize_callbacks(
            [MarshalAs(UnmanagedType.FunctionPtr)] OnDecisionRequestT onDecisionRequest,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnFinishT onFinish,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnInitT onInit,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnErrorT onError,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnRequireT onRequire
        );

        [MonoPInvokeCallback(typeof(OnDecisionRequestT))]
        private static void OnDecisionRequestCallback(int requestId, IntPtr jsonPtr) {
            var json = Marshal.PtrToStringAuto(jsonPtr);
            callbackHandler?.OnDecisionRequest(requestId, json);
        }

        [MonoPInvokeCallback(typeof(OnFinishT))]
        private static void OnFinishCallback(int flag) {
            callbackHandler?.OnFinish(flag);
        }

        [MonoPInvokeCallback(typeof(OnInitT))]
        private static void OnInitCallback() {
            callbackHandler?.OnInit();
        }

        [MonoPInvokeCallback(typeof(OnErrorT))]
        private static void OnErrorCallback(IntPtr jsonPtr) {
            var json = Marshal.PtrToStringAuto(jsonPtr);
            callbackHandler?.OnError(json);
        }

        [MonoPInvokeCallback(typeof(OnRequireT))]
        private static void OnRequireCallback(int requirementId, IntPtr jsonPtr) {
            var json = Marshal.PtrToStringAuto(jsonPtr);
            callbackHandler?.OnDecisionRequest(requirementId, json);
        }

        static CoachActivityRuntime() {
#if !UNITY_EDITOR
            coach_activity_initialize_callbacks(
                OnDecisionRequestCallback,
                OnFinishCallback,
                OnInitCallback,
                OnErrorCallback,
                OnRequireCallback
            );
#endif
        }

        ~CoachActivityRuntime() {
            callbackHandler = null;
        }
    }
#endif
}
