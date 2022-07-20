/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

#if PLATFORM_ANDROID && COACH_AI_AR_FOUNDATION
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
#endif

namespace CoachAiEngine.Android {
    [AddComponentMenu("Coach-AI Engine/Coach-AI Android AR Bridge")]
    public partial class CoachAndroidArBridge : MonoBehaviour {

    }

#if PLATFORM_ANDROID && COACH_AI_AR_FOUNDATION
    public partial class CoachAndroidArBridge {
        private ARCameraManager _cameraManager;
        private ARPlaneManager _planeManager;
        private Camera _camera;
        private long _prevFrameTimestamp = long.MinValue;

        private void Start() {
            var arSession = FindObjectOfType<ARSession>(true);
            Assert.IsNotNull(arSession);
            if (arSession.matchFrameRateEnabled) {
                LogWarning("ARSession is set to match frame rate which can cause lags on detections!");
            }

            _cameraManager = FindObjectOfType<ARCameraManager>(true);
            Assert.IsNotNull(_cameraManager);

            _cameraManager.frameReceived += OnFrameReceived;

            var faceManager = FindObjectOfType<ARFaceManager>();
            if (faceManager != null) {
                // if face manager is active and we are not using front camera we need to disable it
                // otherwise it will force the camera to front instead of back camera
                faceManager.enabled &= _cameraManager.requestedFacingDirection == CameraFacingDirection.User;
            }

            _camera = _cameraManager.GetComponent<Camera>();
            Assert.IsNotNull(_camera);

            _planeManager = FindObjectOfType<ARPlaneManager>(true);
            if (_planeManager == null) {
                LogWarning("No ARPlaneManager found. Plane tracking will be unavailable.");
            }
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args) {
            var timestamp = args.timestampNs;
            if (timestamp == null || timestamp == _prevFrameTimestamp) return;
            _prevFrameTimestamp = timestamp.Value;
            UpdateArFrame();
        }

        private void UpdateArFrame() {
            // on front camera we never seem get past ARSessionState.Ready
            if (ARSession.state < ARSessionState.Ready) return;

            if (!_cameraManager.TryAcquireLatestCpuImage(out var xrImage)) {
                LogWarning("Did not get image :(.");
                xrImage.Dispose();
                return;
            }

            try {
                if (!_cameraManager.TryGetIntrinsics(out var xrCameraIntrinsics)) {
                    return;
                }

                var image = GetCameraImage(xrImage);
                var lensFacing = _cameraManager.currentFacingDirection switch {
                    CameraFacingDirection.World => JNICameraController.LensFacing.Back,
                    CameraFacingDirection.User => JNICameraController.LensFacing.Front,
                    _ => JNICameraController.LensFacing.External
                };

                var intrinsics = new CameraIntrinsics(
                    xrCameraIntrinsics.focalLength,
                    xrCameraIntrinsics.principalPoint,
                    xrCameraIntrinsics.resolution);
                var trackablePlanes = GetTrackablePlanes();
                var sdkPose = SdkPose.Factory.FromUnityCamera(_camera);

                JNICameraController.UpdateFrame(image, intrinsics, trackablePlanes, lensFacing, sdkPose);
            } finally {
                xrImage.Dispose();
            }
        }

        private TrackablePlane[] GetTrackablePlanes() {
            // ReSharper disable once Unity.NoNullPropagation
            var arPlanes = _planeManager?.AllTrackedHorizontalUpwardPlanes() ?? new List<ARPlane>();
            var trackablePlanes = new TrackablePlane[arPlanes.Count];
            var index = 0;
            foreach (var arPlane in arPlanes) {
                var extents = new Vector3(arPlane.extents.x, 0f, arPlane.extents.y);
                var pose = FromArPlane(arPlane);
                var boundary = GetBoundingPolygon(arPlane);
                trackablePlanes[index++] = new TrackablePlane(pose, boundary, extents);
            }

            return trackablePlanes;
        }

        private Vector3[] GetBoundingPolygon(ARPlane arPlane) {
            var polygon = new Vector3[arPlane.boundary.Length];

            var counter = 0;
            foreach (var point in arPlane.boundary) {
                polygon[counter].x = point.x;
                polygon[counter].y = point.y;
                polygon[counter].z = arPlane.transform.position.y;
                counter++;
            }

            return polygon;
        }

        private SdkPose FromArPlane(ARPlane plane) {
            var planeTransform = plane.transform;
            return SdkPose.ConversionHelper.UnityPoseToSdkPose(planeTransform.position, planeTransform.rotation);
        }

        private CameraImage GetCameraImage(XRCpuImage image) {
            var yPlane = image.GetPlane(0);
            var uPlane = image.GetPlane(1);
            var vPlane = image.GetPlane(2);
            IntPtr y, u, v;
            unsafe {
                y = (IntPtr) yPlane.data.GetUnsafePtr();
                u = (IntPtr) uPlane.data.GetUnsafePtr();
                v = (IntPtr) vPlane.data.GetUnsafePtr();
            }

            return new CameraImage(
                width: image.width,
                height: image.height,
                pixelCount: image.width * image.height,
                yRowStride: yPlane.rowStride,
                uvRowStride: uPlane.rowStride,
                uvPixelStride: uPlane.pixelStride,
                y: y,
                u: u,
                v: v,
                timestamp: image.timestamp
            );
        }

        private void OnDestroy() {
            _cameraManager.frameReceived -= OnFrameReceived;
        }

        private void LogWarning(string msg) =>
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, this, msg);
    }
#endif
}
