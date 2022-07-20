// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine {

    public readonly struct TrackablePlane {
        public readonly SdkPose pose;
        public readonly Vector3[] boundary;
        public readonly Vector3 extents;

        public TrackablePlane(SdkPose pose, Vector3[] boundary, Vector3 extents) {
            this.pose = pose;
            this.boundary = boundary;
            this.extents = extents;
        }
    }
}
