/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID
using System;
using CoachAiEngine.Android;
using UnityEngine.Android;
using System.Runtime.InteropServices;
#endif

namespace CoachAiEngine {

    /**
     * <summary>
     * When not using Unity's ArFoundation the CoachAi engine will open the camera on native side.
     * This provides access to the camera feed inside Unity so it can be shown to the user.
     * </summary>
     */
    [AddComponentMenu("Coach-AI Engine/Camera Frame Provider")]
    public class CoachCameraFrameProvider : MonoBehaviour {
        [SerializeField]
        [Tooltip("Whether to use the front camera. Otherwise the back camera is used.")]
        private bool useFrontFacingCamera;

        [SerializeField]
        [Tooltip("Specifies the camera's resolution. Also see isHeight.")]
        private int sideLength = 1280;

        [SerializeField]
        [Tooltip("Whether the specified sideLength defines the height or width axis.")]
        private bool isHeight = true;

        [SerializeField]
        [Tooltip("Controls the camera capture rate. The property is ignored on Android which defaults to 30 FPS.")]
#pragma warning disable 0414
        private int requestedFps = 30;
#pragma warning restore 0414

        [SerializeField]
        [Tooltip("A raw image into which to render the camera content.")]
        private RawImage image;

        [SerializeField]
        [Tooltip("")]
        private AspectRatioFitter aspectRatioFitter;

        private Texture _cameraTexture;

        private bool _cameraInitialized;

        public Texture CameraTexture => _cameraTexture;
        public bool CameraAvailable { get; private set; }
        public bool DidUpdateThisFrame => _cameraTexture is WebCamTexture ? ((WebCamTexture)_cameraTexture).didUpdateThisFrame : true;
        public int CameraHeight => _cameraTexture.height;
        public int CameraWidth => _cameraTexture.width;
        public int CameraRequestedHeight => isHeight ? sideLength : (int) (sideLength * aspect);
        public int CameraRequestedWidth => !isHeight ? sideLength : (int) (sideLength * aspect);
        public bool IsFrontFacingCamera => useFrontFacingCamera;
        public bool IsVerticallyMirrored => _cameraTexture is WebCamTexture ? ((WebCamTexture)_cameraTexture).videoVerticallyMirrored : false;

        private float aspect;

        private bool _isShuttingDown;
        private int _nativeTextureId = 0;

#if UNITY_ANDROID
        private delegate void PluginCallback(int eventId);

        private static class MarshalParameters {
            internal static int requestedWidth, requestedHeight;
            internal static bool useFrontFacingCamera;
            internal static bool graphicsMultiThreaded;
        };

        private void Awake() {
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                Permission.RequestUserPermission(Permission.Camera);
            }
        }

        private void StartCamera() {
            MarshalParameters.requestedWidth = CameraRequestedWidth;
            MarshalParameters.requestedHeight = CameraRequestedHeight;
            MarshalParameters.useFrontFacingCamera = useFrontFacingCamera;
            MarshalParameters.graphicsMultiThreaded = SystemInfo.graphicsMultiThreaded;
            CameraAvailable = true;
            TriggerPluginCallback(PluginEvent.Initialize);
        }

        private enum PluginEvent {
            Initialize = 1,
            Destroy = 2
        }

        private void TriggerPluginCallback(PluginEvent @event) {
            if (SystemInfo.graphicsMultiThreaded) {
                PluginCallback callback = NativeCameraPluginCallback;
                var fp = Marshal.GetFunctionPointerForDelegate(callback);
                GL.IssuePluginEvent(fp, (int) @event);
            } else {
                NativeCameraPluginCallback((int) @event);
            }
        }
#else
        private void StartCamera() {
            foreach (var webCamDevice in WebCamTexture.devices) {
                if (webCamDevice.isFrontFacing != useFrontFacingCamera) continue;
                var webCamTexture = new WebCamTexture(webCamDevice.name, CameraRequestedWidth, CameraRequestedHeight, requestedFps);
                CameraAvailable = true;

                Debug.Log($"{nameof(CoachCameraFrameProvider)}: Using camera: {webCamTexture.deviceName}");
                Debug.Log($"{nameof(CoachCameraFrameProvider)}: Front-facing is: {webCamDevice.isFrontFacing}");
                webCamTexture.Play();
                _cameraTexture = webCamTexture;
                StartCoroutine(WaitForInitializeTexture(webCamDevice));

                break;
            }
        }

        private IEnumerator WaitForInitializeTexture(WebCamDevice webCamDevice) {
            while (_cameraTexture.width < 100) {
                yield return new WaitForSeconds(0.25f);
            }

            var s = new StringBuilder($"{nameof(CoachCameraFrameProvider)}: Available resolutions: ");
            if (webCamDevice.availableResolutions != null) {
                foreach (var resolution in webCamDevice.availableResolutions) {
                    s.AppendLine($"{nameof(CoachCameraFrameProvider)}: {resolution.width}x{resolution.height}@{resolution.refreshRate}fps");
                }
            }

            Debug.Log(s.ToString());

            image.texture = _cameraTexture;
            _cameraInitialized = true;
            Debug.Log($"{nameof(CoachCameraFrameProvider)}: Requested camera resolution: {CameraRequestedWidth}x{CameraRequestedHeight}");
            Debug.Log($"{nameof(CoachCameraFrameProvider)}: Actual camera resolution: {_cameraTexture.width}x{_cameraTexture.height}");
        }
#endif

        private IEnumerator Start() {
            Debug.Log($"{nameof(CoachCameraFrameProvider)}: Start");
            var camera = GameObject.FindObjectOfType<Camera>();
            aspect = camera.aspect;
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                StartCamera();
            }
            yield return null;
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
                StartCamera();
            }

            if (CameraAvailable != true) {
    #if UNITY_EDITOR
                Debug.Log($"{nameof(CoachCameraFrameProvider)}: Running in editor. Coach platform inactive and no camera found.");
    #else
                Debug.LogError($"{nameof(CoachCameraFrameProvider)}: There is no suitable camera available (useFrontFacingCamera={useFrontFacingCamera})!");
    #endif
            }
#endif
        }

#if UNITY_ANDROID
        [AOT.MonoPInvokeCallback(typeof(PluginCallback))]
        static void NativeCameraPluginCallback(int @event) {
            if (!Enum.IsDefined(typeof(PluginEvent), @event)) {
                Debug.LogError($"{nameof(NativeCameraPluginCallback)}: invalid {nameof(PluginEvent)} of value {@event}");
                return;
            }

            if (MarshalParameters.graphicsMultiThreaded) {
                AndroidJNI.AttachCurrentThread();
            }

            switch ((PluginEvent) @event) {
                case PluginEvent.Initialize: {
                    JNICameraController.SetupCameraSession(MarshalParameters.useFrontFacingCamera,
                        MarshalParameters.requestedWidth,
                        MarshalParameters.requestedHeight);
                    break;
                }
                case PluginEvent.Destroy: {
                    JNICameraController.DestroyCameraSession();
                    break;
                }
            }

            if (MarshalParameters.graphicsMultiThreaded) {
                AndroidJNI.DetachCurrentThread();
            }
        }
#endif

        void Update() {
#if UNITY_ANDROID
            if (_isShuttingDown) return;
            if (!CameraAvailable && Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                StartCamera();
            }

            if (_nativeTextureId == 0) {
                _nativeTextureId = JNICameraController.GetOutputTexture();
                return;
            }
            if (_cameraTexture == null) {
                var cameraWidth = JNICameraController.CameraTextureWidth();
                var cameraHeight = JNICameraController.CameraTextureHeight();
                var texture2D = new Texture2D(cameraWidth, cameraHeight, TextureFormat.ARGB32, false);
                texture2D.Apply();
                texture2D.UpdateExternalTexture((IntPtr) _nativeTextureId);

                if (null != image) {
                    image.texture = texture2D;
                }

                _cameraTexture = texture2D;
                _cameraInitialized = true;
                Debug.Log($"{nameof(CoachCameraFrameProvider)}: Requested camera resolution: {CameraRequestedWidth}x{CameraRequestedHeight}");
                Debug.Log($"{nameof(CoachCameraFrameProvider)}: Actual camera resolution: {_cameraTexture.width}x{_cameraTexture.height}");
            }

            var orientation = JNICameraController.GetCameraOrientation();

            if (null != aspectRatioFitter)
                aspectRatioFitter.aspectRatio = 1;

            float xScale = 1, yScale = 1;
            float cameraRatio = (CameraWidth / (float) CameraHeight);
            xScale *= cameraRatio;
            image.rectTransform.localScale = new Vector3(xScale, yScale, 1f);
            image.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);
#else
            if (!_cameraInitialized || !CameraAvailable || !DidUpdateThisFrame) return;
            // update on every frame in case camera orientation changes
            var orientation = ((WebCamTexture)_cameraTexture).videoRotationAngle;

            if (null != aspectRatioFitter)
                aspectRatioFitter.aspectRatio = 1;

            float xScale = 1, yScale = 1;
            if (IsVerticallyMirrored) {
                //invert y-axis if camera is mirrored
                // orientation => camera rotation + device orientation
                if (!IsFrontFacingCamera) {
                    yScale = -1;
                } else if (orientation == 0 || orientation == 180) {   // landscape
                    xScale = -1;
                    yScale = -1;
                }
            }
            float cameraRatio = (CameraWidth / (float) CameraHeight);
            yScale /= cameraRatio;
            image.rectTransform.localScale = new Vector3(xScale, yScale, 1f);
            image.rectTransform.localEulerAngles = new Vector3(0, 0, -orientation);
#endif
        }

#if UNITY_ANDROID
        private void OnApplicationPause(bool isPaused) {
            if (!_cameraInitialized) return;
            if (isPaused) {
                JNICameraController.PauseCameraSession();
            } else {
                JNICameraController.ResumeCameraSession();
            }
        }

        public void StopCamera() {
            if (!_cameraInitialized || _isShuttingDown) return;
            _isShuttingDown = true;
            // image.texture = null;
            _nativeTextureId = 0;
            _cameraTexture = null;
            TriggerPluginCallback(PluginEvent.Destroy);
        }

        private void OnDestroy() => StopCamera();
#endif
    }
}
