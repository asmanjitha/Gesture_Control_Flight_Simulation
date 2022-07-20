/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

namespace CoachAiEngine {

    /// <summary>
    /// Stores a pose in a right handed coordinate system (as in ARCore, ARKit and CoachEngine).
    /// </summary>
    public struct SdkPose {
        public float qx, qy, qz, qw;
        public float x, y, z;
        public Vector3 Position => new Vector3(x, y, z);
        public Quaternion Rotation => new Quaternion(qx, qy, qz, qw);

        /// <summary>
        /// Factory methods to create SdkPose structs
        /// </summary>
        public static class Factory {
            /// <summary>
            /// See ConversionHelper except that the camera is rotated with the phone with
            /// phone held in landscape top right being 0 degrees. Hence portrait (top up)
            /// is 90 degrees rotated to the left in which case we have
            ///
            /// Unity       Coordinate System: right: X, top: Y, forward: Z
            /// CoachEngine Coordinate System: right: X, top: Y, forward: -Z
            ///
            /// </summary>
            /// <param name="unityCamera"></param>
            /// <returns></returns>
            public static SdkPose FromUnityCamera(Camera unityCamera) {
                var rotation = unityCamera.transform.rotation
                               * Quaternion.Euler(Vector3.forward * -ConversionHelper.SensorOrientationDegrees());
                return ConversionHelper.UnityPoseToSdkPose(unityCamera.transform.position, rotation);
            }

            public static SdkPose FromSdkPosition(Vector3 position) {
                var rotation = new Quaternion(0, 0, 0, 1);
                return FromSdkPR(position, rotation);
            }

            // ReSharper disable once InconsistentNaming
            public static SdkPose FromSdkPR(Vector3 position, Quaternion rotation) =>
                new SdkPose {
                    x = position.x,
                    y = position.y,
                    z = position.z,
                    qx = rotation.x,
                    qy = rotation.y,
                    qz = rotation.z,
                    qw = rotation.w
                };
        }

        public Pose ToUnityPose() {
            return ConversionHelper.SdkPoseToUnityPose(this);
        }

        public override string ToString() {
            return $"x: {x}, y: {y}, z: {z}, qx: {qx}, qy: {qy}, qz: {qz}, qw: {qw}";
        }

        public static class ConversionHelper {
            public static int SensorOrientationDegrees() {
                return Screen.orientation switch {
                    ScreenOrientation.LandscapeLeft => 0,
                    ScreenOrientation.Portrait => 90,
                    ScreenOrientation.LandscapeRight => 180,
                    ScreenOrientation.PortraitUpsideDown => 270,
                    _ => 0
                };
            }

            /// <summary>
            /// Unity       Coordinate System: right: X, top: Y, forward: Z
            /// CoachEngine Coordinate System: right: X, top: Y, forward: -Z
            ///
            /// Here forward means: as the camera points.
            ///
            /// Translation between the two system for points is simply flipping the Z axis
            /// For quaternions note that a quaternion describes a rotation of an angle a around an axis
            /// specified by xyz. The quaternion is given as
            /// - Quaternion = [sin(a/2) * xyz , cos(a/2) w]
            /// /// Further note
            /// sin(-a) = -sin(a)
            /// cos(-a) = cos(a)
            /// In order to translate the quaternion we need to map the axis and as we map
            /// from a left-handed (unity) to a right-handed (sdk) system we need to change the sign of the angle.
            ///
            /// </summary>
            /// <param name="unityPose"></param>
            /// <returns></returns>
            public static SdkPose UnityPoseToSdkPose(Pose unityPose) {
                return UnityPoseToSdkPose(unityPose.position, unityPose.rotation);
            }

            /// <summary>
            /// Unity       Coordinate System: right: X, top: Y, forward: Z
            /// CoachEngine Coordinate System: right: X, top: Y, forward: -Z
            ///
            /// Here forward means: as the camera points.
            ///
            /// Translation between the two system for points is simply flipping the Z axis
            /// For quaternions note that a quaternion describes a rotation of an angle a around an axis
            /// specified by xyz. The quaternion is given as
            /// - Quaternion = [sin(a/2) * xyz , cos(a/2) w]
            /// /// Further note
            /// sin(-a) = -sin(a)
            /// cos(-a) = cos(a)
            /// In order to translate the quaternion we need to map the axis and as we map
            /// from a left-handed (unity) to a right-handed (sdk) system we need to change the sign of the angle.
            ///
            /// </summary>
            /// <param name="unityPosition"></param>
            /// <param name="unityQuaternion"></param>
            /// <returns></returns>
            public static SdkPose UnityPoseToSdkPose(Vector3 unityPosition, Quaternion unityQuaternion) {
                return new SdkPose {
                    x = unityPosition.x,
                    y = unityPosition.y,
                    z = -unityPosition.z,
                    qx = unityQuaternion.x,
                    qy = unityQuaternion.y,
                    qz = -unityQuaternion.z,
                    qw = -unityQuaternion.w
                };
            }

            /// <summary>
            /// Translates back from CoachEngine coordinate system to Unity coordinate system
            /// </summary>
            /// <param name="sdkPose"></param>
            /// <returns></returns>
            public static Pose SdkPoseToUnityPose(SdkPose sdkPose) {
                var unityPosition = new Vector3(sdkPose.x, sdkPose.y, -sdkPose.z);
                var unityRotation = new Quaternion(sdkPose.qx, sdkPose.qy, -sdkPose.qz, -sdkPose.qw);
                return new Pose(unityPosition, unityRotation);
            }
        }
    }
}
