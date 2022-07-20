// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace CoachAiEngine {
    public partial class CoachActivityRuntime : IActivityCallbacks {
        public event Action<ActivityDecisionRequest> OnDecisionRequest = delegate { };
        public event Action<ActivityError> OnError = delegate { };
        public event Action<FinishFlags> OnFinish = delegate { };
        public event Action OnInit = delegate { };
        public event Action<ActivityRequirement> OnRequire = delegate { };

        private class PublicEventSubscription {
            public Action<PublicEvent> Handler;
            public string EventType;
            public int SubId => Handler.GetHashCode();
        }

        public enum ActivityState {
            Stopped = 0,
            Stopping = 1,
            Initializing = 2,
            Initialized = 3,
            Running = 4
        }

        /**
         * <summary>Suspend polling for events.</summary>
         * <remarks>Suspending does not cancel subscriptions.</remarks>
         */
        public bool pollingSuspended;
        public Action<MetricsEvent> OnMetricsEvent;

        /**
         * <summary>Add a handler to invoke whenever the current activity issues an event for adding a world object.</summary>
         */
        public Action<WorldObjectEvent> OnWorldObjectAdded;

        /**
         * <summary>Add a handler to invoke whenever the current activity issues an event for removing a world object.</summary>
         */
        public Action<WorldObjectEvent> OnWorldObjectRemoved;

        /**
         * <summary>Add a handler to invoke whenever the current activity issues an event for updating a world object.</summary>
         */
        public Action<WorldObjectEvent> OnWorldObjectUpdated;

        private Action<PublicEvent> OnPublicEvent;

        private readonly List<PublicEventSubscription> subscriptions = new List<PublicEventSubscription>();
        private List<string> subscriptionsToRemove = new List<string>();
        private List<string> subscriptionsToAdd = new List<string>();

        private bool _worldObjectAddedEventsEnabled;
        private bool _worldObjectUpdatedEventsEnabled;
        private bool _worldObjectRemovedEventsEnabled;

        public ActivityState State { get; private set; } = ActivityState.Stopped;

        public void UpdatePublicEvents() {
            if (State != ActivityState.Running) return;
            MaybeUpdatePublicEventSubs(subscriptionsToAdd, NativeLayerAPI.SubscribePublicEvents);
            MaybeUpdatePublicEventSubs(subscriptionsToRemove, NativeLayerAPI.UnsubscribePublicEvents);

            UpdateWorldObjectEventSubscriptions(ref _worldObjectAddedEventsEnabled, OnWorldObjectAdded,
                WorldObjectEvent.EventType.ADDED);

            UpdateWorldObjectEventSubscriptions(ref _worldObjectUpdatedEventsEnabled, OnWorldObjectUpdated,
                WorldObjectEvent.EventType.UPDATED);

            UpdateWorldObjectEventSubscriptions(ref _worldObjectRemovedEventsEnabled, OnWorldObjectRemoved,
                WorldObjectEvent.EventType.REMOVED);
        }

        /// <summary>
        /// Will trigger a call to the native activity to check whether new events have been observed in the meantime.
        /// </summary>
        public string[] PollEvents() {
            if (State != ActivityState.Running || pollingSuspended || subscriptions.Count == 0) {
                return Array.Empty<string>();
            }

            var events = NativeLayerAPI.PollEvents();
            foreach (var @event in events) {
                var coachEvent = CoachEvent.FromJson(@event);
                InvokeEventHandlers(coachEvent);
            }
            return events;
        }

        private void InvokeEventHandlers(CoachEvent @event) {
            switch (@event) {
                case MetricsUpdateEvent metricsEvent:
                    foreach (var metric in metricsEvent.Metrics) {
                        OnMetricsEvent?.Invoke(metric);
                    }

                    break;
                case PublicEvent publicEvent:
                    OnPublicEvent?.Invoke(publicEvent);
                    break;
                case WorldObjectEvent {WorldObjectEventType: WorldObjectEvent.EventType.ADDED} worldObjectEvent:
                    if (_worldObjectAddedEventsEnabled) OnWorldObjectAdded(worldObjectEvent);
                    break;
                case WorldObjectEvent {WorldObjectEventType: WorldObjectEvent.EventType.UPDATED} worldObjectEvent:
                    if (_worldObjectUpdatedEventsEnabled) OnWorldObjectUpdated(worldObjectEvent);
                    break;
                case WorldObjectEvent {WorldObjectEventType: WorldObjectEvent.EventType.REMOVED} worldObjectEvent:
                    if (_worldObjectRemovedEventsEnabled) OnWorldObjectRemoved(worldObjectEvent);
                    break;
            }
        }

        internal void InjectEvent(string @event) {
            var coachEvent = CoachEvent.FromJson(@event);
            InvokeEventHandlers(coachEvent);
        }

        /**
         * <summary>Subscribe to the metrics with the given identifiers.</summary>
         * <param name="metricIds">The identifiers of the metrics to subscribe to.</param>
         * <remarks>The only way of cancelling a subscription to metrics is to restart the activity.</remarks>
         */
        public void SubscribeToMetric(params string[] metricIds) {
            NativeLayerAPI.SubscribeMetrics(metricIds);
        }

        /**
         * <summary>Subscribe to a public event published by an activity.</summary>
         * <param name="eventType">The identifiers of the event to subscribe to.</param>
         * <param name="handler">The action to invoke whenever an event is received.</param>
         * <returns>A subscription id. This id can be used to unsubscribe again later.</returns>
         */
        public int SubscribeToEvent(string eventType, Action<PublicEvent> handler) {
            var subscription = new PublicEventSubscription {
                EventType = eventType,
                Handler = publicEvent => {
                    if (eventType == publicEvent.Type) handler(publicEvent);
                }
            };
            OnPublicEvent += subscription.Handler;
            CheckForOtherSubscribers(subscription.EventType, subscriptionsToAdd);
            subscriptions.Add(subscription);
            return subscription.SubId;
        }

        /**
         * <summary>Cancels a subscription for an event.</summary>
         * <param name="subscriptionId">The id of the subscription to cancel.</param>
         */
        public void UnsubscribeFromEvent(int subscriptionId) {
            var subscription = subscriptions.Find(sub => sub.SubId == subscriptionId);
            if (subscription == null) return;
            subscriptions.Remove(subscription);
            OnPublicEvent -= subscription.Handler;
            CheckForOtherSubscribers(subscription.EventType, subscriptionsToRemove);
        }

        private void CheckForOtherSubscribers(string eventType, List<string> events) {
            if(subscriptions.Any(pes => pes.EventType == eventType)) return;
            events.Add(eventType);
        }

        private void MaybeUpdatePublicEventSubs(List<string> pendingSubscriptions, Action<List<string>> subscribe) {
            if (pendingSubscriptions.Count <= 0) return;
            subscribe(pendingSubscriptions.Distinct().ToList());
            pendingSubscriptions.Clear();
        }

        private void UpdateWorldObjectEventSubscriptions(
            ref bool subscribed,
            [CanBeNull] Action<WorldObjectEvent> handler,
            WorldObjectEvent.EventType eventType
        ) {
            var haveSubscribers = handler?.GetInvocationList().Length > 0;
            if (subscribed && !haveSubscribers) {
                NativeLayerAPI.UnsubscribeWorldObjectEvents(eventType);
                subscribed = false;
            } else if (!subscribed && haveSubscribers) {
                NativeLayerAPI.SubscribeWorldObjectEvents(eventType);
                subscribed = true;
            }
        }

        public void SendCommand(string commandName, object data = null) {
            NativeLayerAPI.SendCommand(commandName, data ?? new object());
        }

        internal static void TakeDecision(int requestId, [CanBeNull] object data) {
            Debug.Log($"Replying to decision request id: {requestId} with {data}");
            NativeLayerAPI.TakeDecision(requestId, data);
        }

        internal static void FulfillRequirement(int requirementId, [CanBeNull] object data) {
            Debug.Log($"Fulfilling requirement id: {requirementId} with {data}");
            NativeLayerAPI.FulfillRequirement(requirementId, data);
        }

        /**
         * Allows to start the activity explicitly. If the activity was already started,
         * this is a noop.
         */
        public void StartSdkActivity(CoachActivity activity) {
            if (State != ActivityState.Stopped) return;
            State = ActivityState.Initializing;
            var activityServiceConfig = new NativeLayerAPI.ActivityServiceConfig(
                activity.ActivityId,
                activity.Parameters,
                activity.Variant
            );
            NativeLayerAPI.StartActivity(activityServiceConfig, this);
            Debug.Log($"{nameof(CoachActivityRuntime)}: started coach ai activity");
        }

        /**
         * Stops the currently running activity and clears any subscriptions.
         */
        public void StopSdkActivity() {
            if (State == ActivityState.Stopping || State == ActivityState.Stopped) return;
            State = ActivityState.Stopping;
            Debug.Log($"{nameof(CoachActivityRuntime)}: stopping coach ai activity");
            NativeLayerAPI.StopActivity();
            // FIXME for now just clear any subs. we can't keep and restore them atm because the
            // next activity started might not be the same one
            subscriptions.Clear();
        }

        void IActivityCallbacks.OnDecisionRequest(int requestId, string json) {
            Debug.Log($"Received decision request id: {requestId} payload {json}");
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var request = new ActivityDecisionRequest {
                Id = requestId,
                Properties = properties,
                Type = properties["type"] as string
            };

            OnDecisionRequest(request);
        }

        void IActivityCallbacks.OnFinish(int flag) {
            State = ActivityState.Stopped;
            OnFinish((FinishFlags) flag);
        }

        void IActivityCallbacks.OnInit() {
            State = ActivityState.Running;
            OnInit();
        }

        void IActivityCallbacks.OnError(string json) {
            var error = new ActivityError(json);
            OnError(error);
        }

        void IActivityCallbacks.OnRequire(int requirementId, string type) {
            var requirement = new ActivityRequirement(requirementId, type);
            OnRequire(requirement);
        }
    }
}
