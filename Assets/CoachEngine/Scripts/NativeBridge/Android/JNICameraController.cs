// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using UnityEngine;

namespace CoachAiEngine.Android {

    public class JNICameraController {
        // DirectByteBuffer
        private const string DirectByteBufferClassName = "java/nio/DirectByteBuffer";
        private static readonly IntPtr DirectByteBufferClass;
        private static readonly IntPtr DirectByteBufferInit;

        // UnityCameraConfig
        private const string CameraConfigClassName = "com/coachai/engine/unity/UnityCameraConfig";

        private static readonly IntPtr CameraConfigClass;
        private static readonly IntPtr CameraConfigInit;

        // UnityArPlaneData class and constructor
        private const string ArPlaneClassName = "com/coachai/engine/unity/UnityArPlaneData";

        private static readonly IntPtr ArPlaneClass;
        private static readonly IntPtr ArPlaneInit;

        // UnityCameraController object instance and method ids
        private static readonly IntPtr InstancePtr;
        private const string CameraControllerClassName = "com.coachai.engine.unity.UnityCameraController";

        private static readonly IntPtr CameraTextureHeightMethod;
        private static readonly IntPtr CameraTextureWidthMethod;
        private static readonly IntPtr DestroyCameraSessionMethod;
        private static readonly IntPtr GetCameraOrientationMethod;
        private static readonly IntPtr GetOutputTextureMethod;
        private static readonly IntPtr PauseCameraSessionMethod;
        private static readonly IntPtr ResumeCameraSessionMethod;
        private static readonly IntPtr SetupCameraSessionMethod;
        private static readonly IntPtr UpdateConfigMethod;
        private static readonly IntPtr UpdateFrameMethod;

        private static bool imageConfigUpdateSent = false;

        static JNICameraController() {
            // look-up CameraController object and method ids
            var cameraController = Kotlin.GetObject(CameraControllerClassName);
            InstancePtr = AndroidJNI.NewGlobalRef(cameraController.GetRawObject());
            CameraTextureHeightMethod = GetMethodId(cameraController, "getCameraHeight", "()I");
            CameraTextureWidthMethod = GetMethodId(cameraController, "getCameraWidth", "()I");
            DestroyCameraSessionMethod = GetMethodId(cameraController, "destroyCameraSession", "()V");
            GetOutputTextureMethod = GetMethodId(cameraController, "getOutputTexture", "()I");
            GetCameraOrientationMethod = GetMethodId(cameraController, "getCameraOrientation", "()I");
            PauseCameraSessionMethod = GetMethodId(cameraController, "pauseCameraSession", "()V");
            ResumeCameraSessionMethod = GetMethodId(cameraController, "resumeCameraSession", "()V");
            SetupCameraSessionMethod = GetMethodId(cameraController, "setupCameraSession", "(ZII)V");
            UpdateConfigMethod = GetMethodId(
                cameraController, "updateConfig", $"(L{CameraConfigClassName};IF)V");
            const string byteBufferClassName = "java/nio/ByteBuffer";
            UpdateFrameMethod = GetMethodId(
                cameraController, "updateFrame",
                $"(L{byteBufferClassName};L{byteBufferClassName};L{byteBufferClassName};D[F[F[L{ArPlaneClassName};)V"
            );

            // look-up UnityArPlaneData class and constructor
            ArPlaneClass = AndroidJNI.NewGlobalRef(AndroidJNI.FindClass(ArPlaneClassName));
            ArPlaneInit = AndroidJNI.GetMethodID(ArPlaneClass, "<init>", "([F[F[F[F)V");

            // look-up UnityCameraConfig class and constructor
            CameraConfigClass = AndroidJNI.NewGlobalRef(AndroidJNI.FindClass(CameraConfigClassName));
            CameraConfigInit = AndroidJNI.GetMethodID(CameraConfigClass, "<init>", "([F[F[I)V");

            // look-up DirectByteBuffer class and constructor
            DirectByteBufferClass = AndroidJNI.NewGlobalRef(AndroidJNI.FindClass(DirectByteBufferClassName));
            DirectByteBufferInit = AndroidJNI.GetMethodID(DirectByteBufferClass, "<init>", "(JI)V");
        }

        private static IntPtr GetMethodId(AndroidJavaObject obj, string name, string sig) {
            return AndroidJNI.GetMethodID(obj.GetRawClass(), name, sig);
        }

        /// <summary>
        /// Send image and camera config to android
        /// </summary>
        private static void UpdateImageConfig(
            CameraIntrinsics cameraIntrinsics, LensFacing lensFacing, float sensorRotation
        ) {
            var args = new[] {
                CreateCameraConfig(cameraIntrinsics),
                new jvalue {i = (int) lensFacing},
                new jvalue {f = sensorRotation}
            };
            AndroidJNI.CallVoidMethod(InstancePtr, UpdateConfigMethod, args);
            imageConfigUpdateSent = true;
        }

        /// <summary>
        /// Send frame data to android
        /// </summary>
        public static void UpdateFrame(
            CameraImage image,
            CameraIntrinsics cameraIntrinsics,
            TrackablePlane[] trackedHorizontalUpwardPlanes,
            LensFacing lensFacing,
            SdkPose sdkPose,
            float sensorRotation = float.NaN
        ) {
            if (!imageConfigUpdateSent) {
                UpdateImageConfig(cameraIntrinsics, lensFacing, sensorRotation);
            }

            var (jCameraPosition, jCameraRotation) = CreateJPose(sdkPose);

            // call the `updateFrame` method with all needed data on the `OverrideUnityActivity`
            var args = new [] {
                CreateJPlaneBuffer(image.y, image.pixelCount),
                CreateJPlaneBuffer(image.u, image.pixelCount / 2 - 1),
                CreateJPlaneBuffer(image.v, image.pixelCount / 2 - 1),
                new jvalue { d = image.timestamp },
                jCameraPosition,
                jCameraRotation,
                CreateJArPlanes(trackedHorizontalUpwardPlanes)
            };
            AndroidJNI.CallVoidMethod(InstancePtr, UpdateFrameMethod, args);
        }

        public static void SetupCameraSession(bool useFrontFacingCamera, int requestedWidth, int requestedHeight) {
            var args = AndroidJNIHelper.CreateJNIArgArray(new object[] {
                useFrontFacingCamera,
                requestedWidth,
                requestedHeight
            });
            AndroidJNI.CallVoidMethod(InstancePtr, SetupCameraSessionMethod, args);
        }

        public static void DestroyCameraSession() {
            AndroidJNI.CallVoidMethod(InstancePtr, DestroyCameraSessionMethod, new jvalue[] {});
        }

        public static int GetOutputTexture() {
            return AndroidJNI.CallIntMethod(InstancePtr, GetOutputTextureMethod, new jvalue[] {});
        }

        public static int GetCameraOrientation() {
            return AndroidJNI.CallIntMethod(InstancePtr, GetCameraOrientationMethod, new jvalue[] {});
        }

        public static int CameraTextureWidth() {
            return AndroidJNI.CallIntMethod(InstancePtr, CameraTextureWidthMethod, new jvalue[] {});
        }

        public static int CameraTextureHeight() {
            return AndroidJNI.CallIntMethod(InstancePtr, CameraTextureHeightMethod, new jvalue[] {});
        }

        public static void PauseCameraSession() {
            AndroidJNI.CallVoidMethod(InstancePtr, PauseCameraSessionMethod, new jvalue[] {});
        }

        public static void ResumeCameraSession() {
            AndroidJNI.CallVoidMethod(InstancePtr, ResumeCameraSessionMethod, new jvalue[] {});
        }

        private static jvalue CreateCameraConfig(CameraIntrinsics ci) {
            var focalLengthFloats = new [] { ci.focalLength.x, ci.focalLength.y };
            var principlePointFloats = new [] { ci.principalPoint.x, ci.principalPoint.y };
            var imageDimensionInts = new [] { ci.resolution.x, ci.resolution.y };

            var args = new [] {
                new jvalue { l = AndroidJNI.ToFloatArray(focalLengthFloats) },
                new jvalue { l = AndroidJNI.ToFloatArray(principlePointFloats) },
                new jvalue { l = AndroidJNI.ToIntArray(imageDimensionInts) }
            };

            return new jvalue { l = AndroidJNI.NewObject(CameraConfigClass, CameraConfigInit, args) };
        }

        private static jvalue CreateJPlaneBuffer(IntPtr planePtr, int lenght) {
            var args = new[] {
                new jvalue {j = planePtr.ToInt64()},
                new jvalue {i = lenght}
            };
            return new jvalue { l = AndroidJNI.NewObject(DirectByteBufferClass, DirectByteBufferInit, args) };
        }

        /// <summary>
        /// Create jvalue[2] with posePosition and poseRotation
        /// </summary>
        private static (jvalue position, jvalue rotation) CreateJPose(SdkPose sdkPose) {
            var posePosition = new [] { sdkPose.x, sdkPose.y, sdkPose.z };
            var poseRotation = new [] { sdkPose.qx, sdkPose.qy, sdkPose.qz, sdkPose.qw };

            return (
                new jvalue { l = AndroidJNI.ToFloatArray(posePosition) },
                new jvalue { l = AndroidJNI.ToFloatArray(poseRotation) }
            );
        }

        /// <summary>
        /// Get all tracked horizontal upward planes and create a jvalue array for it.
        /// </summary>
        private static jvalue CreateJArPlanes(TrackablePlane[] arPlanes) {
            var jplanes = AndroidJNI.NewObjectArray(arPlanes.Length, ArPlaneClass, IntPtr.Zero);

            var counter = 0;
            foreach (var arPlane in arPlanes) {
                var jplane = CreateJPlane(arPlane);
                AndroidJNI.SetObjectArrayElement(jplanes, counter++, jplane);
            }

            return new jvalue { l = jplanes };
        }

        private static IntPtr CreateJPlane(TrackablePlane arPlane) {
            var (jPosePos, jPoseRot) = CreateJPose(arPlane.pose);
            var args = new[] {
                GetJExtent(arPlane),
                jPosePos,
                jPoseRot,
                GetJPolygonsFrom(arPlane)
            };
            return AndroidJNI.NewObject(ArPlaneClass, ArPlaneInit, args);
        }

        private static jvalue GetJExtent(TrackablePlane arPlane) {
            return new jvalue() {
                l = AndroidJNI.ToFloatArray(new [] {
                    arPlane.extents.x,
                    0f,
                    arPlane.extents.y
                    })
            };
        }

        private static jvalue GetJPolygonsFrom(TrackablePlane arPlane) {
            var polygons = new float[arPlane.boundary.Length * 3];

            var counter = 0;
            foreach (var point in arPlane.boundary) {
                polygons[counter * 3] = point.x;
                polygons[counter * 3 + 1] = point.y;
                polygons[counter * 3 + 2] = point.z;
                counter++;
            }

            return new jvalue() { l = AndroidJNI.ToFloatArray(polygons) };
        }

        public enum LensFacing {
            Front = 0,
            Back = 1,
            External = 2
        }
    }
}
