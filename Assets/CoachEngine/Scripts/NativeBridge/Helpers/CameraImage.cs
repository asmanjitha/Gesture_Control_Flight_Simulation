/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System;

namespace CoachAiEngine {

    public readonly struct CameraImage {
        public readonly int width;
        public readonly int height;
        public readonly int pixelCount;
        public readonly int yRowStride;
        public readonly int uvRowStride;
        public readonly int uvPixelStride;
        public readonly IntPtr y;
        public readonly IntPtr u;
        public readonly IntPtr v;
        public readonly double timestamp;

        public CameraImage(
            int width,
            int height,
            int pixelCount,
            int yRowStride,
            int uvRowStride,
            int uvPixelStride,
            IntPtr y,
            IntPtr u,
            IntPtr v,
            double timestamp
        ) {
            this.width = width;
            this.height = height;
            this.pixelCount = pixelCount;
            this.yRowStride = yRowStride;
            this.uvRowStride = uvRowStride;
            this.uvPixelStride = uvPixelStride;
            this.y = y;
            this.u = u;
            this.v = v;
            this.timestamp = timestamp;

        }
    }
}
