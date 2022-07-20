// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine {

    public readonly struct CameraIntrinsics {
        public readonly Vector2 focalLength;
        public readonly Vector2 principalPoint;
        public readonly Vector2Int resolution;

        public CameraIntrinsics(Vector2 focalLength, Vector2 principalPoint, Vector2Int resolution) {
            this.focalLength = focalLength;
            this.principalPoint = principalPoint;
            this.resolution = resolution;
        }
    }
}
