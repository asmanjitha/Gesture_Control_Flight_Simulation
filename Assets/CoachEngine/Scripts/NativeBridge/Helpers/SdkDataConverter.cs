/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;
using UnityEngine.Assertions;

namespace CoachAiEngine {
    public static class SdkDataConverter {
        /// <summary>
        ///
        /// </summary>
        /// <param name="sdkPositionString"></param>
        /// <returns></returns>
        public static Pose UnityPoseFromSdkPosition(string sdkPositionString) {
            var sdkPosition = ToVector3(sdkPositionString);
            var sdkPose = SdkPose.Factory.FromSdkPosition(sdkPosition);

            return sdkPose.ToUnityPose();
        }

        /// <summary>
        /// Translates a serialized float string e.g. "0.1,0.2,0.3" to a float array.
        /// </summary>
        /// <param name="floatsString">e.g. "0.1,0.2,0.3"</param>
        /// <returns></returns>
        public static Vector3 ToVector3(string floatsString) {
            var positionFloats = ToFloatArray(floatsString);
            Assert.IsTrue(positionFloats.Length == 3);

            return new Vector3(positionFloats[0], positionFloats[1], positionFloats[2]);
        }

        /// <summary>
        /// Convert list of float string e.g. "0.1,0.2,0.1" to float array
        /// </summary>
        /// <param name="floatsString">e.g. "0.1,0.2,0.1"</param>
        /// <returns></returns>
        public static float[] ToFloatArray(string floatsString) {
            var strings = floatsString.Split(',');
            var floatValues = new float[strings.Length];

            for (int i = 0; i < strings.Length; i++) {
                floatValues[i] = FloatUtils.ParseSane(strings[i]);
            }
            return floatValues;
        }
    }
}
