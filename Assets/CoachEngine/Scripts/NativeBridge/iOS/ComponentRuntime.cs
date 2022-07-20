/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#if PLATFORM_IOS && !UNITY_EDITOR
using Newtonsoft.Json;
using UnityEngine;

namespace CoachAiEngine.Ios {
    public partial class ComponentRuntime : IComponentRuntime {
        private int _nativeRuntimeHandle;
        public bool Disposed => _nativeRuntimeHandle == -1;
        private readonly int _runtimeHandle;

        public ComponentRuntime(IComponentRuntimeCallbacks callbackHandler) {
            Debug.Log("Starting ComponentRuntime");
            _runtimeHandle = Registry.RegisterRuntime(callbackHandler);
            _nativeRuntimeHandle = coach_cr_create();
        }

        ~ComponentRuntime() => Stop();

        /**
         * <inheritdoc cref="IComponentRuntime.Load"/>
         */
        public void Load(CoachComponent component) {
            if (Disposed) return;
            var config = JsonConvert.SerializeObject(component.Parameters);
            coach_cr_load(_nativeRuntimeHandle, component.ComponentId, config, OnComponentLoaded, _runtimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Configure"/>
         */
        public void Configure(CoachComponent component) {
            if (Disposed) return;
            var runConfig = JsonConvert.SerializeObject(component.Parameters);
            coach_cr_configure(_nativeRuntimeHandle, component.ComponentId, runConfig, OnComponentConfigured, _runtimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Start"/>
         */
        public void Start() {
            if (Disposed) return;
            coach_cr_resume(_nativeRuntimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Pause"/>
         */
        public void Pause() {
            if (Disposed) return;
            coach_cr_pause(_nativeRuntimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Stop"/>
         */
        public void Stop() {
            if (Disposed) return;
            var handle = _nativeRuntimeHandle;
            _nativeRuntimeHandle = -1;
            coach_cr_dispose(handle);
            Registry.DeregisterRuntime(_runtimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Subscribe"/>
         */
        public long Subscribe(string eventId) {
            if (Disposed) {
                Debug.LogError($"{nameof(ComponentRuntime)}: runtime is disposed.");
                return -1;
            }
            return coach_cr_subscribe(_nativeRuntimeHandle, eventId, OnEvent, _runtimeHandle);
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Unsubscribe"/>
         */
        public void Unsubscribe(long subscriptionId) {
            if (Disposed) return;
            coach_cr_unsubscribe(_nativeRuntimeHandle, subscriptionId);
        }
    }
}
#endif
