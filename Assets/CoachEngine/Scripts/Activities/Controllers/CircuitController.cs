/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by a tool.
// CoachAI Core Version: 13.2.3
// Generated On: 2022-06-13T13:29:09Z
//
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CoachAiEngine;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CoachAiEngine.Activities {

    [System.CodeDom.Compiler.GeneratedCode(tool: "Coach-Ai engine", version: "13.2.3")]
    [AddComponentMenu("Coach-AI Engine/Activities/Ball Sports/CircuitController")]
    public class CircuitController : CoachActivityController {

#if UNITY_EDITOR
        public void OnValidate() {
            if (CoachActivityConfiguration != null && !(CoachActivityConfiguration is CircuitSpec)) {
                Debug.LogWarning($"Configuration must be of type {nameof(CircuitSpec)}");
                CoachActivityConfiguration = null;
            }
        }
#endif

        [Serializable]
        public class EventSubscription {
            [Tooltip("The id of the event that should be subscribed to.")]
            public AvailableEvents Event;

            [Tooltip("A callback handler that is invoked whenever the event is observed.")]
            public UnityEvent<PublicEvent> OnEvent;
        }

        [Serializable]
        public class FeedbackSubscription {
            [Tooltip("The id of the feedback event that should be subscribed to.")]
            public AvailableFeedback Feedback;

            [Tooltip("A callback handler that is invoked whenever the feedback event is observed.")]
            public UnityEvent<PublicEvent> OnFeedback;
        }

        [Serializable]
        public class MetricsSubscription {
            [Tooltip("The id of the event that should be subscribed to.")]
            public AvailableMetrics Metric;

            [Tooltip("A callback handler that is invoked whenever the event is observed.")]
            public UnityEvent<MetricsEvent> OnMetricUpdate;
        }

        [Space(15)] [Header("Events")] [Tooltip("List of [PublicEvent]s to subscribe to")]
        public List<EventSubscription> EventSubscriptions;

        [Space(15)] [Tooltip("List of Feedbacks to subscribe to")]
        public List<FeedbackSubscription> FeedbackSubscriptions;

        [Space(15)] [Tooltip("List of Metrics to subscribe to")]
        public List<MetricsSubscription> MetricSubscriptions;

        [Space(15)]
        [Tooltip("Callback that is called on any observed event")]
        public UnityEvent<PublicEvent> OnAnyEvent;

        [Space(15)] [Header("World Objects")]
        public UnityEvent<WorldObjectEvent> OnWorldObjectAdded;
        public UnityEvent<WorldObjectEvent> OnWorldObjectRemoved;
        public UnityEvent<WorldObjectEvent> OnWorldObjectUpdated;

        [Space(15)] [Header("Decision Requests")]
        public UnityEvent<ActivityDecisionRequest> OnDecisionRequest;

        protected override void InitializeSubscriptions() {
            // Event configuration
            foreach (var subscription in EventSubscriptions) {
                var subscriptionId = Runtime.SubscribeToEvent(AvailableEventsLookup[subscription.Event], @event => {
                    subscription.OnEvent?.Invoke(@event);
                    OnAnyEvent.Invoke(@event);
                });
                if (subscriptionId < 0) {
                    Debug.LogError($"{nameof(CircuitController)}: Could not subscribe to {subscription.Event}");
                }
            }

            // Feedback configuration
            foreach (var subscription in FeedbackSubscriptions) {
                var subscriptionId = Runtime.SubscribeToEvent(AvailableFeedbackLookup[subscription.Feedback], @event => {
                    subscription.OnFeedback?.Invoke(@event);
                    OnAnyEvent.Invoke(@event);
                });
                if (subscriptionId < 0) {
                    Debug.LogError($"{nameof(CircuitController)}: Could not subscribe to {subscription.Feedback}");
                }
            }

            // Metrics
            if (MetricSubscriptions.Count > 0) {
                Runtime.SubscribeToMetric(MetricSubscriptions.Select(m => AvailableMetricsLookup[m.Metric]).ToArray());
                Runtime.OnMetricsEvent += @event => {
                    foreach (var subscription in MetricSubscriptions) {
                        if (@event.Type == AvailableMetricsLookup[subscription.Metric]) {
                            subscription.OnMetricUpdate?.Invoke(@event);
                            return;
                        }
                    }
                };
            }

            // wire up
            Runtime.OnWorldObjectAdded += @event => OnWorldObjectAdded?.Invoke(@event);
            Runtime.OnWorldObjectRemoved += @event => OnWorldObjectRemoved?.Invoke(@event);
            Runtime.OnWorldObjectUpdated += @event => OnWorldObjectUpdated?.Invoke(@event);
        }

        protected override void RegisterLifecycleEventHandlers() {
            base.RegisterLifecycleEventHandlers();
            Runtime.OnDecisionRequest += OnDecisionRequest.Invoke;
            Runtime.OnRequire += requirement => {
                Debug.Log($"Fulfilling requirement {requirement.Type}(id={requirement.Id})");
                if (requirement.RequiresArSource || requirement.RequiresCameraSource) {
                    requirement.Fulfill(new { type = "com.coachai.engine.unity.UnitySession" });
                } else {
                    throw new ArgumentException($"Unsupported requirement {requirement.Type}");
                }
            };
        }

        public enum AvailableEvents {
            CameraPlacementLocations,
            MissingArPlanesIssue,
            PoseOutsidePlanesIssue,
            BallPresence
        }

        public Dictionary<AvailableEvents, string> AvailableEventsLookup = new Dictionary<AvailableEvents, string> {
            { AvailableEvents.CameraPlacementLocations, "com.coachai.engine.cameraplacement.CameraPlacementLocations" },
            { AvailableEvents.MissingArPlanesIssue, "com.coachai.engine.analytics.pose.MissingArPlanesIssue" },
            { AvailableEvents.PoseOutsidePlanesIssue, "com.coachai.engine.analytics.pose.PoseOutsidePlanesIssue" },
            { AvailableEvents.BallPresence, "com.coachai.engine.analytics.ball.BallPresence" }
        };

        public enum AvailableFeedback {
            PlaceTheBallOnTheCenterToStart,
            SuccessfulPlacement,
            FailedPlacement,
            Failure,
            InstructionSequenceSuccess,
            InstructionExplanation,
            ListInstructionExplanation,
            CheckYourReport,
            BallPresenceFeedback
        }

        public Dictionary<AvailableFeedback, string> AvailableFeedbackLookup = new Dictionary<AvailableFeedback, string> {
            { AvailableFeedback.PlaceTheBallOnTheCenterToStart, "com.coachai.activities.ballsports.circuit.PlaceTheBallOnTheCenterToStart" },
            { AvailableFeedback.SuccessfulPlacement, "com.coachai.engine.activity.feedback.SuccessfulPlacement" },
            { AvailableFeedback.FailedPlacement, "com.coachai.engine.activity.feedback.FailedPlacement" },
            { AvailableFeedback.Failure, "com.coachai.engine.activity.feedback.Failure" },
            { AvailableFeedback.InstructionSequenceSuccess, "com.coachai.activities.ballsports.circuit.InstructionSequenceSuccess" },
            { AvailableFeedback.InstructionExplanation, "com.coachai.activities.ballsports.circuit.InstructionExplanation" },
            { AvailableFeedback.ListInstructionExplanation, "com.coachai.activities.ballsports.circuit.ListInstructionExplanation" },
            { AvailableFeedback.CheckYourReport, "com.coachai.engine.activity.feedback.CheckYourReport" },
            { AvailableFeedback.BallPresenceFeedback, "com.coachai.engine.analytics.ball.BallPresenceFeedback" }
        };

        public enum AvailableMetrics {
            Score,
            TotalTime,
            InstructionSequenceList
        }

        public Dictionary<AvailableMetrics, string> AvailableMetricsLookup = new Dictionary<AvailableMetrics, string> {
            { AvailableMetrics.Score, "com.coachai.engine.activity.metric.Score" },
            { AvailableMetrics.TotalTime, "com.coachai.engine.activity.metric.TotalTime" },
            { AvailableMetrics.InstructionSequenceList, "com.coachai.activities.ballsports.circuit.InstructionSequenceList" }
        };

    }
}   