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

namespace CoachAiEngine.Analytics {

    [System.CodeDom.Compiler.GeneratedCode(tool: "Coach-Ai engine", version: "13.2.3")]
    [CreateAssetMenu(fileName = nameof(HandPalmDetectorAnalytics), menuName = "Coach-AI Engine/Components/HumanBody/HandPalmDetectorAnalytics (Experimental)")]
    public class HandPalmDetectorAnalytics : CoachComponent {

        public override string ComponentId => "com.coachai.engine.analytics.pose.components.HandPalmDetectorAnalytics";
        private static readonly string[] eventIds =  {
            "com.coachai.engine.analytics.pose.HandPalmDetectorResult"
        };

        private static readonly HandPalmModelType HandPalmModel = HandPalmModelType.HandPalmOBox1fGeneralLightV1;
        private static readonly float ConfidenceThreshold = 0.0f;
        private static readonly int TargetFPS = 30;
        private static readonly int MaximumInboundQueueSize = 2;

        [Tooltip("Choose a palm detection model. Please read model documentation to be aware of the use cases and limitations.")]
        public HandPalmModelType handPalmModel = HandPalmModel;
        [Tooltip("Use a higher threshold for higher precision or a lower threshold for higher recall. Allowed values are in range [0, 1[. If zero use default value of selected model.")]
        public float confidenceThreshold = ConfidenceThreshold;
        [Tooltip("Set maximum allowed processed frames per second")]
        public int targetFPS = TargetFPS;
        [Tooltip("Set maximum allowed inbound events to put into processing queue")]
        public int maximumInboundQueueSize = MaximumInboundQueueSize;

        public override Dictionary<string, object> Parameters {
            get {
                var parameters = new Dictionary<string, object>();

                AddUnlessDefault("com.coachai.engine.analytics.pose.components.HandPalmModel",
                    HandPalmModelTypeLookup[handPalmModel], HandPalmModelTypeLookup[HandPalmModel], parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.ConfidenceThreshold",
                    confidenceThreshold, ConfidenceThreshold, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.parameters.TargetFPS",
                    targetFPS, TargetFPS, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.parameters.MaximumInboundQueueSize",
                    maximumInboundQueueSize, MaximumInboundQueueSize, parameters
                );
                return parameters;
            }
        }

        private void AddUnlessDefault<T>(string key, T value, T @default, Dictionary<string, object> parameters) {
            if (value.Equals(@default)) return;
            parameters.Add(key, value);
        }

        public override List<string> PublishedEventIds => eventIds.ToList();

        public enum HandPalmModelType {
            HandPalmOBox1fGeneralLightV1,
            HandPalmOBox1fGeneralMediumV1
        }

        public Dictionary<HandPalmModelType, string> HandPalmModelTypeLookup = new Dictionary<HandPalmModelType, string> {
            { HandPalmModelType.HandPalmOBox1fGeneralLightV1, "hand-palm.o-box.1f.general.light v1" },
            { HandPalmModelType.HandPalmOBox1fGeneralMediumV1, "hand-palm.o-box.1f.general.medium v1" }
        };

    }
}