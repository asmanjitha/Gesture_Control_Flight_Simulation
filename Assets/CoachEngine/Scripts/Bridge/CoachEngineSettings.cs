/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

namespace CoachAiEngine {

    /**
     * Global settings for Coach-Ai Engine
     */
    [CreateAssetMenu(fileName = nameof(CoachEngineSettings), menuName = "Coach-AI Engine/Settings")]
    public class CoachEngineSettings : ScriptableObject {
        [Tooltip("Log level used by the CoachEngine SDK. This does NOT effect Unity log levels.")]
        public LogLevel logLevel = LogLevel.Info;
    }
}
