// /*
//  * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoachAiEngine {

    internal interface IComponentRuntime {
        /**
         * <summary>Load and start the given component.</summary>
         * <param name="component">The component to load and its configuration.</param>
         */
        void Load(CoachComponent component);

        /**
         * <summary>Configure a loaded component.</summary>
         * <param name="component">The component to configure and its configuration.</param>
         * <remarks>The component must already be loaded into runtime.</remarks>
         */
        void Configure(CoachComponent component);

        /**
         * <summary>Start or resume all configured components.</summary>
         */
        void Start();

        /**
         * <summary>Pause all configured components of this runtime.</summary>
         */
        void Pause();

        /**
         * <summary>Stop this runtime and all components loaded by this runtime.</summary>
         * <remarks>Using the runtime after stopping it is an error.</remarks>
         */
        void Stop();

        /**
         * <summary>Subscribe to events published by the components inside this runtime.</summary>
         * <param name="eventId">The identifier of the event to subscribe to.</param>
         * <returns>A subscriptionId != -1 in case of successful subscription.</returns>
         */
        long Subscribe(string eventId);

        /**
         * <summary>Unsubscribe from an event published by components inside this runtime.</summary>
         * <param name="subscriptionId">Cancel the subscription with this id.</param>
         */
        void Unsubscribe(long subscriptionId);
    }

    public interface IComponentRuntimeCallbacks {
        void OnLoaded(string componentId, bool success);
        void OnConfigured(string componentId, bool success);
        void OnEvent(string @event);
    }

    public class CoachComponentRuntime : IComponentRuntimeCallbacks {
        private IComponentRuntime _runtime;
        private readonly Dictionary<string, string> _pendingComponentCallbacks = new Dictionary<string, string>();
        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private CoachComponentController _controller;

        private bool Initialized { get; set; }

        public State RuntimeState { get; private set; }

        /**
         * <summary>
         * Creates a new empty <see cref="CoachComponentRuntime"/> for the current platform. After creation the runtime
         * will be in <see cref="State"/> Paused.
         * </summary>
         *
         * <remarks>In Unity Editor this will create a stub runtime!</remarks>
         */
        public CoachComponentRuntime(CoachComponentController controller) {
#if PLATFORM_ANDROID
            _runtime = new Android.ComponentRuntime(this);
#elif PLATFORM_IOS && !UNITY_EDITOR
            _runtime = new Ios.ComponentRuntime(this);
#elif UNITY_EDITOR
            // start empty implementation for now. in future we could
            // start a fake component runtime replaying events here
            _runtime = new EditorComponentRuntime(this);
#endif
            _controller = controller;
            RuntimeState = State.Paused;
        }

        /**
         * <summary><para>
         * Creates a new <see cref="CoachComponentRuntime"/> for the current platform. After creation the
         * <see cref="RuntimeState"/> will be <c>Paused</c>.
         * </para><para>
         * Once all components have been successfully loaded the runtime will automatically resume execution
         * and its <see cref="RuntimeState"/> will change to <c>Running</c>.
         * </para></summary>
         *
         * <param name="components">A list of components to load into this runtime.</param>
         * <param name="controller">The owner of this runtime instance.</param>
         * <remarks>In Unity Editor this will create a stub runtime!</remarks>
         */
        public static CoachComponentRuntime StartComponentRuntime(List<CoachComponent> components, CoachComponentController controller) {
            var runtime = new CoachComponentRuntime(controller);
            runtime.Load(components);
            runtime.Initialized = true;
            return runtime;
        }

        void IComponentRuntimeCallbacks.OnLoaded(string componentId, bool success)
            => ComponentLifecycleCallback(componentId, nameof(Load), success);

        void IComponentRuntimeCallbacks.OnConfigured(string componentId, bool success)
            => ComponentLifecycleCallback(componentId, nameof(Configure), success);

        void IComponentRuntimeCallbacks.OnEvent(string @event) => OnComponentEvent(@event);

        private void ComponentLifecycleCallback(string componentId, string operation, bool success) {
            if (!success) {
                Debug.LogWarning($"{operation} failed for {componentId}");
            }

            if (_pendingComponentCallbacks[componentId] == operation) {
                _pendingComponentCallbacks.Remove(componentId);
            }

            if (_pendingComponentCallbacks.Count == 0) {
                Debug.Log("Received all outstanding callbacks. Resuming component runtime.");
                Resume();
            }
        }

        private void OnComponentEvent(string @event) {
            _controller.PushToMain(() => {
                var publicEvent = (PublicEvent) CoachEvent.FromJson(@event);
                foreach (var subscription in _subscriptions.FindAll(sub => sub.eventType == publicEvent.Type)) {
                    subscription.handler.Invoke(publicEvent);
                }
            });
        }

        private bool CheckIsPaused() {
            if (RuntimeState == State.Paused) return true;
            Debug.LogWarning("Runtime must be paused before loading or configuring components!");
            return false;
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Load"/>
         */
        public void Load(CoachComponent component) {
            if (!CheckIsPaused()) return;
            Debug.Log($"Loading component {component.ComponentId} into runtime.");
            _pendingComponentCallbacks[component.ComponentId] = nameof(Load);
            _runtime.Load(component);
        }

        private void Load(List<CoachComponent> components) {
            if (!CheckIsPaused()) return;
            foreach (var component in components) {
                _pendingComponentCallbacks[component.ComponentId] = nameof(Load);
            }
            foreach (var component in components) {
                _runtime.Load(component);
            }
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Configure"/>
         */
        public void Configure(CoachComponent component) {
            if (!CheckIsPaused()) return;
            Debug.Log($"Configuring component {component.ComponentId}.");
            _pendingComponentCallbacks[component.ComponentId] = nameof(Configure);
            _runtime.Configure(component);
        }

        public void Resume() {
            _runtime.Start();
            Debug.Log("Resuming component runtime");
            RuntimeState = State.Running;
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Pause"/>
         */
        public void Pause() {
            RuntimeState = State.Paused;
            Debug.Log("Pausing component runtime");
            _runtime.Pause();
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Stop"/>
         */
        public void Stop() {
            if (!Initialized) return;
            RuntimeState = State.Stopped;
            Debug.Log("Stopping component runtime");
            _runtime.Stop();
            _runtime = null;
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Subscribe"/>
         */
        public long Subscribe(string eventId, Action<PublicEvent> action) {
            if (!Initialized) {
                Debug.LogError($"{nameof(CoachComponentRuntime)}: runtime not initialized");
                return -1;
            }
            Debug.Log($"Subscribing to {eventId}");
            var subscriptionId = _runtime.Subscribe(eventId);
            if (subscriptionId == -1) {
                Debug.LogError($"{nameof(CoachComponentRuntime)}: subscription to {eventId} failed.");
                return subscriptionId;
            }
            Debug.Log($"Subscribed to {eventId}. Subscription id: {subscriptionId}");
            var subscription = new Subscription {
                eventType = eventId,
                handler = action,
                id = subscriptionId
            };
            _subscriptions.Add(subscription);

            return subscriptionId;
        }

        /**
         * <inheritdoc cref="IComponentRuntime.Unsubscribe"/>
         */
        public void Unsubscribe(long subscriptionId) {
            if (!Initialized) return;
            Debug.Log($"Cancelling subscription {subscriptionId}");
            _runtime.Unsubscribe(subscriptionId);
            _subscriptions.RemoveAll(sub => sub.id == subscriptionId);
        }

        internal void InjectEvent(string @event) {
            OnComponentEvent(@event);
        }

        public enum State {
            Paused,
            Running,
            Stopped
        }

        private class Subscription {
            public string eventType;
            public Action<PublicEvent> handler;
            public long id;
        }

#if UNITY_EDITOR
        private class EditorComponentRuntime : IComponentRuntime {
            private readonly IComponentRuntimeCallbacks _callbackHandler;

            public EditorComponentRuntime(IComponentRuntimeCallbacks callbackHandler) {
                _callbackHandler = callbackHandler;
            }

            public void Configure(CoachComponent component) {
                Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Configure)} {component.ComponentId}");
                _callbackHandler.OnConfigured(component.ComponentId, true);
            }

            public void Load(CoachComponent component) {
                Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Load)}");
                _callbackHandler.OnLoaded(component.ComponentId, true);
            }

            public void Start() => Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Start)}");

            public void Pause() => Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Pause)}");

            public void Stop() => Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Stop)}");

            public long Subscribe(string eventId) {
                Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Subscribe)} {eventId}");
                return eventId.GetHashCode();
            }

            public void Unsubscribe(long subscriptionId) =>
                Debug.Log($"{nameof(EditorComponentRuntime)} {nameof(Unsubscribe)} {subscriptionId}");
        }
#endif
    }
}
