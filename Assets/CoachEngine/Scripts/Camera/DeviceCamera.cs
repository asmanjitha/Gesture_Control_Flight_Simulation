/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoachAiEngine {

    public enum Rotation {
        Rotate0Degree = 0,
        Rotate90Degree = 90,
        Rotate180Degree = 180,
        Rotate270Degree = 270
    }

    public static class RotationHelpers {
        public static float ToRadians(this Rotation rotation) {
            switch (rotation) {
                case Rotation.Rotate0Degree:
                    return 0;
                case Rotation.Rotate90Degree:
                    return Mathf.PI / 2;
                case Rotation.Rotate180Degree:
                    return Mathf.PI;
                default:
                    return Mathf.PI * 3 / 2;
            }
        }

        public static int ToDegree(this Rotation rotation) => (int) rotation;

        public static Rotation Plus(this Rotation rotation, ScreenOrientation orientation) {
            return rotation.Plus(orientation.ToRotation());
        }

        public static Rotation Plus(this Rotation rot1, Rotation rot2) {
            var degrees = ((int) rot1 + (int) rot2) % 360;
            return RotationFrom(degrees);
        }

        public static Rotation Minus(this Rotation rotation, ScreenOrientation orientation) {
            return rotation.Minus(orientation.ToRotation());
        }

        public static Rotation Minus(this Rotation rot1, Rotation rot2) {
            // C# does not deal well with negative mods
            var degrees = ((((int) rot1 - (int) rot2) % 360) + 360) % 360;
            return RotationFrom(degrees);
        }

        private static Rotation RotationFrom(int degree) {
            switch (degree) {
                case 0:
                    return Rotation.Rotate0Degree;
                case 90:
                    return Rotation.Rotate90Degree;
                case 180:
                    return Rotation.Rotate180Degree;
                case 270:
                    return Rotation.Rotate270Degree;
                default:
                    Debug.LogWarning($"Rotation: unknown degree {degree}.");
                    return Rotation.Rotate0Degree;
            }
        }


        private static Rotation ToRotation(this ScreenOrientation orientation) {
            switch (orientation) {
                case ScreenOrientation.Portrait:
                    return Rotation.Rotate0Degree;
                case ScreenOrientation.LandscapeLeft:
                    return Rotation.Rotate270Degree;
                case ScreenOrientation.LandscapeRight:
                    return Rotation.Rotate90Degree;
                case ScreenOrientation.PortraitUpsideDown:
                    return Rotation.Rotate180Degree;
                default:
                    return Rotation.Rotate0Degree;
            }
        }
    }

    public class DeviceCamera {
        public enum Lens {
            Front,
            Back
        }

        private const string JsonKeyIntrinsics = "intrinsicsMatrix";
        private const string JsonKeyPose = "cameraPose";
        private const string JsonKeyViewMatrix = "viewMatrix";
        private const string JsonKeySensorRotation = "sensorRotation";
        private const string JsonKeyLensFacing = "lensFacing";
        private const string JsonKeyImageDimension = "imageDimension";

        /**
         * Usually an intrinsics matrix is a 3x3 matrix. But Unity treats 2d transformations
         * simply by ignoring z ..
         */
        public Matrix4x4 IntrinsicsMatrix;

        public Matrix4x4 Pose;
        public Matrix4x4 ViewMatrix;
        public Lens LensFacing;
        public int ImageWidth;
        public int ImageHeight;
        public Rotation SensorRotation;

        public bool IsFrontCamera => LensFacing == Lens.Front;
        public bool IsBackCamera => LensFacing == Lens.Back;

        private Matrix4x4 _screenTransform = Matrix4x4.zero;

        public Vector2 TransformToScreenCoordinates(Vector2 location) {
            var transform = GetTransformToViewCoordinates();
            var loc4 = transform * new Vector4(location.x, location.y, 1f, 1f);
            return new Vector2(loc4.x, loc4.y);
        }

        /**
         * Return a transformation for transforming values in sensor space to Unity screen space
         */
        public Matrix4x4 GetTransformToViewCoordinates() {
            if (_screenTransform == Matrix4x4.zero) {
                _screenTransform = GetTransformToViewCoordinates(Screen.width, Screen.height, Screen.orientation);
            }

            return _screenTransform;
        }

        public Matrix4x4 GetTransformToViewCoordinates(int width, int height, ScreenOrientation orientation) {
            return GetTransformToViewCoordinates(width, height, orientation, IsFrontCamera);
        }

        public Matrix4x4 GetTransformToViewCoordinates(
            int width,
            int height,
            ScreenOrientation screenOrientation,
            bool flipHorizontally
        ) {
            var rotation = ImageToScreenRotation(screenOrientation);
            return GetTransformToViewCoordinates(width, height, rotation, flipHorizontally);
        }

        public Matrix4x4 GetTransformToViewCoordinates(
            int width,
            int height,
            Rotation cameraImageToTargetRotation,
            bool flipHorizontally
        ) {
            return TransformUtils2d.CreateTransform2d(
                ImageWidth,
                ImageHeight,
                width,
                height,
                cameraImageToTargetRotation,
                flipHorizontally
            );
        }

        /**
         * Returns the rotation that is needed to transform the camera image to the screen.
         */
        public Rotation ImageToScreenRotation() {
            return ImageToScreenRotation(Screen.orientation);
        }

        public Rotation ImageToScreenRotation(ScreenOrientation orientation) {
            return IsFrontCamera ? SensorRotation.Minus(orientation) : SensorRotation.Plus(orientation);
        }

        /**
         * A camera object as coming from the Coach SDK is of the form
         *
         *     { "intrinsicsMatrix":{"m":[991.05817,0.0,0.0,0.0,1077.1757,0.0,640.0,360.0,1.0]},
         *       "cameraPose": {"m":[1.0,0.0,0.0,0.0, 0.0,1.0,0.0,0.0, 0.0,0.0,1.0,0.0, 0.0,0.0,0.0,1.0]},
         *      "viewMatrix":{"m":[1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0]}
         *
         * Matrices are in column-major
         */
        [CanBeNull]
        public static DeviceCamera CreateFrom(JObject cameraJson) {
            var rawIntrinsics = cameraJson[JsonKeyIntrinsics];
            var rawPose = cameraJson[JsonKeyPose];
            var rawViewMatrix = cameraJson[JsonKeyViewMatrix];
            var rawLensFacing = cameraJson[JsonKeyLensFacing];
            var rawImageDimension = cameraJson[JsonKeyImageDimension];
            var rawSensorRotation = cameraJson[JsonKeySensorRotation];

            Matrix4x4 intrinsics = Matrix4x4.identity;
            if (rawIntrinsics["m"] is JArray mIntrinsic) {
                intrinsics.m00 = mIntrinsic[0].Value<float>();
                intrinsics.m01 = mIntrinsic[1].Value<float>();
                intrinsics.m10 = mIntrinsic[3].Value<float>();
                intrinsics.m11 = mIntrinsic[4].Value<float>();
                intrinsics.m20 = mIntrinsic[6].Value<float>();
                intrinsics.m21 = mIntrinsic[7].Value<float>();
            } else {
                return null;
            }

            Matrix4x4 pose;
            if (rawPose["m"] is JArray mPose) {
                pose = new Matrix4x4(
                    new Vector4(mPose[0].Value<float>(),
                        mPose[1].Value<float>(),
                        mPose[2].Value<float>(),
                        mPose[3].Value<float>()),
                    new Vector4(mPose[4].Value<float>(),
                        mPose[5].Value<float>(),
                        mPose[6].Value<float>(),
                        mPose[7].Value<float>()),
                    new Vector4(mPose[8].Value<float>(),
                        mPose[9].Value<float>(),
                        mPose[10].Value<float>(),
                        mPose[11].Value<float>()),
                    new Vector4(mPose[12].Value<float>(),
                        mPose[13].Value<float>(),
                        mPose[14].Value<float>(),
                        mPose[15].Value<float>())
                );
            } else {
                return null;
            }

            Matrix4x4 viewMatrix;
            if (rawViewMatrix["m"] is JArray mViewMatrix) {
                viewMatrix = new Matrix4x4(
                    new Vector4(mViewMatrix[0].Value<float>(),
                        mViewMatrix[1].Value<float>(),
                        mViewMatrix[2].Value<float>(),
                        mViewMatrix[3].Value<float>()),
                    new Vector4(mViewMatrix[4].Value<float>(),
                        mViewMatrix[5].Value<float>(),
                        mViewMatrix[6].Value<float>(),
                        mViewMatrix[7].Value<float>()),
                    new Vector4(mViewMatrix[8].Value<float>(),
                        mViewMatrix[9].Value<float>(),
                        mViewMatrix[10].Value<float>(),
                        mViewMatrix[11].Value<float>()),
                    new Vector4(mViewMatrix[12].Value<float>(),
                        mViewMatrix[13].Value<float>(),
                        mViewMatrix[14].Value<float>(),
                        mViewMatrix[15].Value<float>())
                );
            } else {
                return null;
            }

            int imageHeight;
            int imageWidth;
            if (rawImageDimension is JObject oImageDimension) {
                imageWidth = oImageDimension["first"].Value<int>();
                imageHeight = oImageDimension["second"].Value<int>();
            } else {
                return null;
            }

            Lens lens;
            if (rawLensFacing is JToken oLens) {
                lens = oLens.Value<string>() == "BACK" ? Lens.Back : Lens.Front;
            } else {
                return null;
            }

            Rotation sensorOrientation;
            if (rawSensorRotation is JToken oSensorOrientation) {
                var strOrientation = oSensorOrientation.Value<string>();
                switch (strOrientation) {
                    case "Rotate0Degree":
                        sensorOrientation = Rotation.Rotate0Degree;
                        break;
                    case "Rotate90Degree":
                        sensorOrientation = Rotation.Rotate90Degree;
                        break;
                    case "Rotate180Degree":
                        sensorOrientation = Rotation.Rotate180Degree;
                        break;
                    case "Rotate270Degree":
                        sensorOrientation = Rotation.Rotate270Degree;
                        break;
                    default:
                        sensorOrientation = Rotation.Rotate0Degree;
                        Debug.LogWarning($"DeviceCamera: Unsupported sensor orientation: {strOrientation}");
                        break;
                }
            } else {
                return null;
            }

            return new DeviceCamera {
                IntrinsicsMatrix = intrinsics,
                Pose = pose,
                ViewMatrix = viewMatrix,
                ImageHeight = imageHeight,
                ImageWidth = imageWidth,
                LensFacing = lens,
                SensorRotation = sensorOrientation
            };
        }
    }
}
