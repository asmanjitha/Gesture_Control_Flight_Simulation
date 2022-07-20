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
    [CreateAssetMenu(fileName = nameof(TrapBallDetection), menuName = "Coach-AI Engine/Components/BallSports/TrapBallDetection")]
    public class TrapBallDetection : CoachComponent {

        public override string ComponentId => "com.coachai.engine.analytics.ball.components.TrapBallDetection";
        private static readonly string[] eventIds =  {
            "com.coachai.engine.analytics.ball.pipeline.TrapBallEvent"
        };

        public override Dictionary<string, object> Parameters {
            get {
                var parameters = new Dictionary<string, object>();

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