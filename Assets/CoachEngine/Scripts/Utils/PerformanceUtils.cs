/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CoachAiEngine {

    public enum PerformanceProfile {
        /**
         * 60 FPS
         * Full resolution
         */
        High,
        /**
         * 30 FPS
         * reduced resolution on android
         */
        Medium,
        /**
         * 30 FPS
         * resolution 0.5
         */
        Low
    }

    public static class PerformanceUtils {
        private static Resolution nativeResolution;

        private const int AndroidMinResolution = 1280 * 720; // 720p
        private const float AndroidMinResolutionFactor = 0.5f; //
        private const float AndroidMaxResolutionFactor = 1f; //

        public static Resolution NativeResolution {
            get {
                CaptureNativeResolution();
                return nativeResolution;
            }
            private set { nativeResolution = value; }
        }

        private static bool _startResolutionCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init() {
            _startResolutionCaptured = false;
        }

        public static void ConfigureFor(PerformanceProfile profile) {
            Debug.Log($"PerformanceUtils: Set performance profile {profile}");

            SetVsync(0);
            SetTargetFPS(profile);
            SetResolution(profile);
        }

        public static void SetNativeResolution() {
            Debug.Log($"PerformanceUtils: Use Native resolution {NativeResolution.width} x {NativeResolution.height}.");

            CaptureNativeResolution();
            Screen.SetResolution(NativeResolution.width, NativeResolution.height, true);
        }

        public static void SetResolutionByHeight(int height) {
            CaptureNativeResolution();

            var factor = Mathf.Clamp01(height / (float) NativeResolution.height);
            Debug.Log($"PerformanceUtils: Set resolution based on long height of {height} => factor {factor}.");

            SetResolution(factor);
        }

        private static void SetResolution(PerformanceProfile profile) {
            var factor = GetResolution(profile);
            SetResolution(factor);
        }

        public static void SetResolution(float factor) {
            CaptureNativeResolution();

            if (factor > 1) {
                SetNativeResolution();
            } else if (factor <= 0) {
                SetNativeResolution();
            } else {
                Debug.Log(
                    $"PerformanceUtils: Native resolution {NativeResolution.width} x {NativeResolution.height}. Decrease resolution to {factor}.");
                Screen.SetResolution(Mathf.RoundToInt(NativeResolution.width * factor),
                    Mathf.RoundToInt(NativeResolution.height * factor), true);
            }
        }

        public static float GetResolution(PerformanceProfile profile) {
            switch (profile) {
                case PerformanceProfile.High:
                    return 1f;
                case PerformanceProfile.Low:
                    return 0.5f;
                default:
#if !UNITY_EDITOR && UNITY_ANDROID
                    var curSize = NativeResolution.width * NativeResolution.height;
                    var factor =
 Mathf.Clamp(AndroidMinResolution / (float) curSize, AndroidMinResolutionFactor, AndroidMaxResolutionFactor);
                    return factor;
#else
                    return 1f;
#endif
            }
        }

        private static void CaptureNativeResolution() {
            if (!_startResolutionCaptured) {
                _startResolutionCaptured = true;
                NativeResolution = Screen.currentResolution;
            }
        }

        public static void SetVsync(int vsync) {
            QualitySettings.vSyncCount = vsync;
            Debug.Log($"PerformanceUtils: Set vsync {vsync}.");
        }

        public static int GetTargetFPS(PerformanceProfile profile) {
            switch (profile) {
                case PerformanceProfile.High:
                    return 60;
                default:
                    return 30;
            }
        }

        public static void SetTargetFPS(PerformanceProfile profile) {
            var target = GetTargetFPS(profile);
            SetTargetFPS(target);
        }

        public static void SetTargetFPS(int fps) {
            Application.targetFrameRate = fps;
            Debug.Log($"PerformanceUtils: Set target frame rate {fps}.");
        }
    }

}
