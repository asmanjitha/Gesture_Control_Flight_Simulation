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
    [CreateAssetMenu(fileName = nameof(PoseProximity2dAnalytics), menuName = "Coach-AI Engine/Components/HumanBody/PoseProximity2dAnalytics")]
    public class PoseProximity2dAnalytics : CoachComponent {

        public override string ComponentId => "com.coachai.engine.analytics.pose.components.PoseProximity2dAnalytics";
        private static readonly string[] eventIds =  {
            "com.coachai.engine.analytics.pose.components.PoseProximityEvent"
        };

        private static readonly float PoseProximityWidthToImageHeightRatio = 0.4f;
        private static readonly float PoseProximityHeightToImageHeightRatio = 0.8f;
        private static readonly float PoseProximityImageWidthRatioToCenter = 0.5f;
        private static readonly float PoseProximityImageHeightRatioToCenter = 0.5f;

        [Tooltip("Pose box width in camera image height, capped by screen width")]
        public float poseProximityWidthToImageHeightRatio = PoseProximityWidthToImageHeightRatio;
        [Tooltip("Pose box height in camera image height, capped by screen height")]
        public float poseProximityHeightToImageHeightRatio = PoseProximityHeightToImageHeightRatio;
        [Tooltip("Pose box center x coordinate as a proportion camera image size, capped by screen size")]
        public float poseProximityImageWidthRatioToCenter = PoseProximityImageWidthRatioToCenter;
        [Tooltip("Pose box center y coordinate as a proportion camera image size, capped by screen size")]
        public float poseProximityImageHeightRatioToCenter = PoseProximityImageHeightRatioToCenter;

        public override Dictionary<string, object> Parameters {
            get {
                var parameters = new Dictionary<string, object>();

                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseProximityWidthToImageHeightRatio",
                    poseProximityWidthToImageHeightRatio, PoseProximityWidthToImageHeightRatio, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseProximityHeightToImageHeightRatio",
                    poseProximityHeightToImageHeightRatio, PoseProximityHeightToImageHeightRatio, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseProximityImageWidthRatioToCenter",
                    poseProximityImageWidthRatioToCenter, PoseProximityImageWidthRatioToCenter, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseProximityImageHeightRatioToCenter",
                    poseProximityImageHeightRatioToCenter, PoseProximityImageHeightRatioToCenter, parameters
                );
                return parameters;
            }
        }

        private void AddUnlessDefault<T>(string key, T value, T @default, Dictionary<string, object> parameters) {
            if (value.Equals(@default)) return;
            parameters.Add(key, value);
        }

        public override List<string> PublishedEventIds => eventIds.ToList();

    }
}