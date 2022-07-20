// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections.Generic;
using System.Linq;
using CoachAiEngine.Android;
using NRKernal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CoachAiEngine.Nreal {

    /// <summary>
    /// Coach-AI engine - Nreal Bridge. If you work with Nreal glasses, the NrealSdkBridge
    /// needs to be active somewhere in the scene for the Coach-AI engine to ingest
    /// images coming from the Nreal glasses.
    /// </summary>
    public class NrealSdkBridge : MonoBehaviour {
        [HideInInspector] public Rotation sensorRotation = Rotation.Rotate90Degree;

        /// <summary> The native camera proxy. </summary>
        protected NativeCameraProxy m_NativeCameraProxy;

        public struct YUVTextureFrame {
            public NativeArray<byte> YBuf;
            public NativeSlice<byte> UBuf;
            public NativeSlice<byte> VBuf;
            public NativeArray<byte> UVBuf;
        }

        private YUVTextureFrame m_textureFrame;

        private ulong timestamp;

        public void Start() {
            // setup camera capture
            Debug.Log($"{nameof(NrealSdkBridge)}: CameraProxyFactory.CreateRGBCameraProxy()");
            m_NativeCameraProxy = CameraProxyFactory.CreateRGBCameraProxy();
            m_NativeCameraProxy.SetImageFormat(CameraImageFormat.YUV_420_888);
            m_NativeCameraProxy.Play();

            m_textureFrame = new YUVTextureFrame();

#if PLATFORM_ANDROID
            Application.onBeforeRender += OnBeforeRender;
#endif

            Debug.Log($"{nameof(NrealSdkBridge)}: Started CoachAi Nreal Bridge");
        }

#if PLATFORM_ANDROID
        private void OnDestroy() {
            Application.onBeforeRender -= OnBeforeRender;
        }
#endif

        private void Stop() {
            m_NativeCameraProxy.Stop();
        }

        /// <summary>
        /// Send updated frame to android.
        /// </summary>
        private void OnBeforeRender() {
            var frame = m_NativeCameraProxy.GetFrame();
            if (frame.data == null) {
                Debug.LogWarning($"{nameof(NrealSdkBridge)}: Did not get image :(.");
                return;
            }

            if (frame.timeStamp == timestamp) {
                return;
            }
            timestamp = frame.timeStamp;
            //Debug.Log($"{timestamp / 1000000000.0}: received frame.");

            var width = m_NativeCameraProxy.Resolution.width;
            var height = m_NativeCameraProxy.Resolution.height;
            var image = CreateARCameraImage(frame, width, height);
            if (width <= 0 || height <= 0) {
                return;
            }

            var cameraPose = CreateCameraPose();
            var intrinsics = CreateCameraIntrinsics(width, height);
            var trackablePlanes = CreateTrackablePlanes();
            const JNICameraController.LensFacing lensFacing = JNICameraController.LensFacing.External;
            var fSensorRotation = (float) sensorRotation;

            JNICameraController.UpdateFrame(image, intrinsics, trackablePlanes, lensFacing, cameraPose, fSensorRotation);
        }

        private TrackablePlane[] CreateTrackablePlanes() {
            var nrTrackablePlanes = new List<NRTrackablePlane>();
            NRFrame.GetTrackables(nrTrackablePlanes, NRTrackableQueryFilter.All);
            // Using all planes might lead to issues: If a plane you are not playing on gets larger than the playing plane,
            // the largest planes y-coordinate will be used as frame.floorY. If that is significantly different, the
            // ball detector will discard ball detections.

            return nrTrackablePlanes
                    .FindAll(x => x.GetPlaneType() == TrackablePlaneType.HORIZONTAL)
                    .Select(CreateTrackablePlane)
                    .ToArray();
        }

        private TrackablePlane CreateTrackablePlane(NRTrackablePlane nrTrackablePlane) {
            var pose = nrTrackablePlane.GetCenterPose();
            var sdkPose = SdkPose.Factory.FromSdkPR(pose.position, pose.rotation);
            var extents = new Vector3(nrTrackablePlane.ExtentX, 0f, nrTrackablePlane.ExtentZ);
            var boundary = GetBoundaryPolygon(nrTrackablePlane);

            return new TrackablePlane(sdkPose, boundary, extents);
        }

        private static Vector3[] GetBoundaryPolygon(NRTrackablePlane nrTrackablePlane) {
            var boundaryPolygonPoints = new List<Vector3>();
            nrTrackablePlane.GetBoundaryPolygon(boundaryPolygonPoints);

            var polygons = new Vector3[boundaryPolygonPoints.Count];

            var counter = 0;
            foreach (var point in boundaryPolygonPoints) {
                polygons[counter].x = point.x;
                polygons[counter].y = point.y;
                polygons[counter].z = nrTrackablePlane.GetCenterPose().position.y;
                counter++;
            }

            return polygons;
        }

        private SdkPose CreateCameraPose() {
            // determine camera pose
            var cameraPoseFromHead = NRDevice.Subsystem.NativeHMD.GetDevicePoseFromHead(NativeDevice.RGB_CAMERA);
            var headPose = NRFrame.HeadPose;
            var cameraPose = cameraPoseFromHead.GetTransformedBy(headPose);

            // Debug.Log($"NRSdkBridge: OnBeforeRender headPose: {headPose} cameraPose: {cameraPose} cameraPoseFromHead: {cameraPoseFromHead}");
            // Debug.Log($"NRSdkBridge: OnBeforeRender cameraPose: position: {cameraPose.position} / direction: {cameraPose.forward} / quaternion: {cameraPose.rotation}");
            //
            // Debug.DrawRay(cameraPose.position, cameraPose.forward, Color.red, 10.0f);
            return SdkPose.ConversionHelper.UnityPoseToSdkPose(cameraPose.position, cameraPose.rotation);
        }

        private CameraIntrinsics CreateCameraIntrinsics(int width, int height) {
            var nativeIntrinsics = NRFrame.GetRGBCameraIntrinsicMatrix();
            //var nativeDistortion = NRFrame.GetRGBCameraDistortion();
            var focalLength = new Vector2(nativeIntrinsics.column0.X, nativeIntrinsics.column1.Y);
            var principalPoint = new Vector2(nativeIntrinsics.column2.X, nativeIntrinsics.column2.Y);
            var resolution = new Vector2Int(width, height);
            return new CameraIntrinsics(focalLength, principalPoint, resolution);
        }

        private CameraImage CreateARCameraImage(FrameRawData frame, int width, int height) {
            // fill y u v buffers
            var size = frame.data.Length;
            if (!m_textureFrame.YBuf.IsCreated) {
                m_textureFrame.YBuf = new NativeArray<byte>(size * 8 / 12, Allocator.Persistent);
                m_textureFrame.UVBuf = new NativeArray<byte>(size * 4 / 12, Allocator.Persistent);

                // create strided slices for U and V
                m_textureFrame.UBuf = m_textureFrame.UVBuf.Slice(0, size * 4 / 12 - 1);
                m_textureFrame.VBuf = m_textureFrame.UVBuf.Slice(1, size * 4 / 12 - 1);
            }

            // copy contigious Y
            NativeArray<byte>.Copy(frame.data, 0, m_textureFrame.YBuf, 0, m_textureFrame.YBuf.Length);
            // copy contigous U and V to strided buffers
            unsafe {
                fixed (byte* frameDataPtr = frame.data) {
                    var uPtr = m_textureFrame.UBuf.GetUnsafePtr();
                    UnsafeUtility.MemCpyStride(
                        uPtr, 2,
                        frameDataPtr + m_textureFrame.YBuf.Length, 1,
                        UnsafeUtility.SizeOf<byte>(), m_textureFrame.UVBuf.Length/2
                    );

                    var vPtr = m_textureFrame.VBuf.GetUnsafePtr();
                    UnsafeUtility.MemCpyStride(
                        vPtr, 2,
                        frameDataPtr + m_textureFrame.YBuf.Length + m_textureFrame.UVBuf.Length/2, 1,
                        UnsafeUtility.SizeOf<byte>(), m_textureFrame.UVBuf.Length/2
                    );
                }
            }

            IntPtr y;
            IntPtr u;
            IntPtr v;

            unsafe {
                y = (IntPtr) m_textureFrame.YBuf.GetUnsafePtr();
                u = (IntPtr) m_textureFrame.UBuf.GetUnsafePtr();
                v = (IntPtr) m_textureFrame.VBuf.GetUnsafePtr();
            }

            return new CameraImage(
                    width: width,
                    height: height,
                    pixelCount: width * height,
                    yRowStride: width,
                    uvRowStride: width,
                    uvPixelStride: 2,
                    y: y,
                    u: u,
                    v: v,
                    timestamp: frame.timeStamp / 1_000_000_000.0
            );
        }
    }
}
