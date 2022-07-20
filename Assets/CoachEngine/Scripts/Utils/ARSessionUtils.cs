// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System.Collections;
#if COACH_AI_AR_FOUNDATION
using UnityEngine;
using UnityEngine.XR.ARFoundation;
#endif

namespace CoachAiEngine {
    public static class ARSessionUtils {

        public static IEnumerator WaitForARSession() {
#if COACH_AI_AR_FOUNDATION
            var session = Object.FindObjectOfType<ARSession>(true);
            if (session == null) {
                return null;
            }

            var cameraManager = Object.FindObjectOfType<ARCameraManager>(true);
            if (cameraManager == null) {
                return null;
            }

            if (cameraManager.requestedFacingDirection == CameraFacingDirection.World) {
                return new WaitUntil(() => ARSession.state >= ARSessionState.SessionInitializing);
            }

            // backhanded check to see if ar session is configured. ARSession.state value will
            // never go past ARSessionState.Ready on front camera because there is no tracking.
            var configured = false;
            void FrameReceived(ARCameraFrameEventArgs _) {
                configured = true;
                cameraManager.frameReceived -= FrameReceived;
            }
            cameraManager.frameReceived += FrameReceived;
            return new WaitUntil(() => configured);
#else
            return null;
#endif
        }
    }
}
