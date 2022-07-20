// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CoachAiEngine {

    /**
     * <summary>Base class for all event types published by CoachAi Engine activities and components.</summary>
     */
    public abstract class CoachEvent {
        /**
         * <summary>The concrete type identifier string of this event.
         * This will usually (but not always) be the qualified class name of the event on native side.
         * For an activity generating pose events this could be something like
         * <c>com.coachai.analytics.pose.Pose3dResult</c>.</summary>
         */
        public string Type { get; }

        /**
         * <summary>A timestamp ordering this event relative to other events.</summary>
         * <remarks>This cannot be correlated to the timestamps of events of other runs in a meaningful way.</remarks>
         */
        public double Timestamp { get; }

        /**
         * <summary>The data associated with this event.</summary>
         */
        public Dictionary<string, object> Properties { get; }

        protected CoachEvent(string type, double timestamp, Dictionary<string, object> properties) {
            Type = type;
            Timestamp = timestamp;
            Properties = properties;
        }

        internal static CoachEvent FromJson(string json) {
            var jObject = JObject.Parse(json);
            var timestamp = jObject["timestamp"]!.Value<double>();
            var type = jObject["type"]!.Value<string>();

            if (jObject.ContainsKey("eventType")) {
                if (!Enum.TryParse(jObject["eventType"]!.Value<string>(), true, out WorldObjectEvent.EventType eventType)) {
                    eventType = WorldObjectEvent.EventType.NONE;
                }
                var id = jObject["id"]!.Value<string>();
                var properties = jObject.ToObject<Dictionary<string, object>>();
                return new WorldObjectEvent(type, id, timestamp, eventType, properties);
            }


            var data = jObject["data"]?.Type switch {
                JTokenType.String =>
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(jObject["data"].Value<string>()),
                JTokenType.Object =>
                    jObject["data"].ToObject<Dictionary<string, object>>(),
                _ =>
                    new Dictionary<string, object>()
            };



            return type switch {
                "com.coachai.engine.activity.ActivityMetrics" =>
                    new MetricsUpdateEvent(type, timestamp, data),
                _ =>
                    new PublicEvent(type, timestamp, data)
            };
        }
    }

    /**
     * <summary>Represents a metrics event published by an activity.</summary>
     * <remarks>
     * The <c>Properties</c> of a metrics event only contain the latest value for any metric that has
     * been updated since the last event.
     * This means if a metric is updated more frequently than it is published these updates are lost.
     * Also if a metric hasn't been updated between the previous and the latest event it will not be
     * present inside its <c>Properties</c>.
     * </remarks>
     */
    public class MetricsEvent  {
        /**
         * <summary>The concrete type identifier string of this event.
         * This will usually (but not always) be the qualified class name of the event on native side.
         * For an activity generating pose events this could be something like
         * <c>com.coachai.analytics.pose.Pose3dResult</c>.</summary>
         */
        public string Type { get; }

        /**
         * <summary>A timestamp ordering this event relative to other events.</summary>
         * <remarks>This cannot be correlated to the timestamps of events of other runs in a meaningful way.</remarks>
         */
        public double Timestamp { get; }

        /**
         * <summary>Provides the actual value of the metric.</summary>
         */
        public object Value { get; }

        public bool IsInt => Value is int;
        public int IntValue => Value is int value ? value : 0;

        public bool IsLong => Value is long;
        public long LongValue => Value is long value ? value : 0L;


        public bool IsDouble => Value is Double;
        public double DoubleValue => Value is double value ? value : 0.0;

        public bool IsFloat => Value is float;
        public float FloatValue => Value is float value ? value : 0.0f;

        public bool IsString => Value is string;
        [CanBeNull] public string StringValue => Value is string value ? value : null;

        public MetricsEvent(string type, double timestamp, object value) {
            Type = type;
            Timestamp = timestamp;
            Value = value;
        }
    }

    public class MetricsUpdateEvent : CoachEvent {

        public List<MetricsEvent> Metrics { get; }

        public MetricsUpdateEvent(string type, double timestamp, Dictionary<string, object> properties)
            : base(type, timestamp, properties) {
            Metrics = new List<MetricsEvent>();
            foreach (var e in properties) {
                Metrics.Add(new MetricsEvent(e.Key, timestamp, e.Value));
            }
        }

    }

    /**
     * <summary>Represents an event published by an activity or a component.</summary>
     * <remarks>These are the most common events generated by Coach-Ai activities and components.</remarks>
     */
    public class PublicEvent : CoachEvent {
        public PublicEvent(string type, double timestamp, Dictionary<string, object> properties)
            : base(type, timestamp, properties) { }
    }

    /**
     * <summary>Represents an event published by an activity when a world object is added, updated or removed.</summary>
     * <remarks>
     * World object events don't necessarily refer to actual objects. An activity might for example issue a
     * collision world object event when two objects collide.
     * </remarks>
     */
    public class WorldObjectEvent : CoachEvent {

        /**
         * <summary>Indicates if the world object was added, updated or removed.</summary>
         * <remarks></remarks>
         */
        public EventType WorldObjectEventType { get; }

        /**
         * <summary>The identifier of the world object that this event references.</summary>
         * <remarks>This identifier is not guaranteed to be unique!</remarks>
         */
        public string Id { get; }

        public WorldObjectEvent(
            string type, string id, double timestamp, EventType eventType, Dictionary<string, object> properties
        ) : base (type, timestamp, properties) {
            Id = id;
            WorldObjectEventType = eventType;
        }

        /**
         * <summary>Types of events that can occur with world objects.</summary>
         */
        public enum EventType {
            NONE = 0,
            ADDED = 1,
            UPDATED = 2,
            REMOVED = 4,
            ALL = ADDED | UPDATED | REMOVED
        }
    }

    [Serializable]
    public class EventSubscription {
        [Tooltip("The id of the event that should be subscribed to.")]
        public string EventId;

        [Tooltip("A callback handler that is invoked whenever the event is observed.")]
        public UnityEvent<PublicEvent> OnEvent;
    }

    [Serializable]
    public class MetricSubscription {
        [Tooltip("The id of the event that should be subscribed to.")]
        public string EventId;

        [Tooltip("A callback handler that is invoked whenever the event is observed.")]
        public UnityEvent<MetricsEvent> OnEvent;
    }
}
