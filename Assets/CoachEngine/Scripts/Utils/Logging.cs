/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System;
using System.Diagnostics;

namespace CoachAiEngine {

    public static class Log {

        [Conditional("COACH_AI_LOG_VERBOSE"),
         Conditional("COACH_AI_LOG_DEBUG"),
         Conditional("COACH_AI_LOG_INFO"),
         Conditional("COACH_AI_LOG_WARN"),
         Conditional("COACH_AI_LOG_ERROR")]
        public static void Verbose(object message) {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("COACH_AI_LOG_DEBUG"),
         Conditional("COACH_AI_LOG_INFO"),
         Conditional("COACH_AI_LOG_WARN"),
         Conditional("COACH_AI_LOG_ERROR")]
        public static void Debug(object message) {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("COACH_AI_LOG_INFO"),
         Conditional("COACH_AI_LOG_WARN"),
         Conditional("COACH_AI_LOG_ERROR")]
        public static void Info(object message) {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("COACH_AI_LOG_WARN"), Conditional("COACH_AI_LOG_ERROR")]
        public static void Warn(object message) {
            UnityEngine.Debug.LogWarning(message);
        }

        [Conditional("COACH_AI_LOG_ERROR")]
        public static void Error(Exception ex) {
            UnityEngine.Debug.LogException(ex);
        }

        [Conditional("COACH_AI_LOG_ERROR")]
        public static void Error(object message) {
            UnityEngine.Debug.LogError(message);
        }
    }
}
