/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

namespace CoachAiEngine {

    public static class TransformUtils2d {

        /**
         * Create a centered region of interest in unit coordinates.
         *
         * @return Vector4(topLeftX, topLeftY, width, height)
         */
        public static Vector4 CreateRegionOfInterest(
            int srcWidth,
            int srcHeight,
            int dstWidth,
            int dstHeight,
            Rotation srcToDstRotation
        ) {
            if (srcToDstRotation == Rotation.Rotate90Degree || srcToDstRotation == Rotation.Rotate270Degree) {
                (srcWidth, srcHeight) = (srcHeight, srcWidth);
            }

            var dstAspectRatio = dstWidth / (float) dstHeight;
            var srcAspectRatio = srcWidth / (float) srcHeight;
            var w = dstAspectRatio < srcAspectRatio ? dstAspectRatio / srcAspectRatio : 1f;
            var h = dstAspectRatio >= srcAspectRatio ? srcAspectRatio / dstAspectRatio : 1f;
            var topLeftX = (1f - w) / 2f;  // divide by 2 as we move halfway in
            var topLeftY = (1f - h) / 2f;

            return new Vector4(topLeftX, topLeftY, w, h);
        }

        /**
         * Note that the resulting 2D transformation is embedded in a 4x4 matrix. Thus 2d points need to be
         * translated using Transform * (x,y,0,1).
         */
        public static Matrix4x4 CreateTransform2d(
            int srcWidth,
            int srcHeight,
            int dstWidth,
            int dstHeight,
            Rotation srcToDstRotation,
            bool flipHorizontally
        ) {
            var roi = CreateRegionOfInterest(srcWidth, srcHeight, dstWidth, dstHeight, srcToDstRotation);
            return CreateTransform2d(srcWidth, srcHeight, dstWidth, dstHeight, srcToDstRotation, roi, flipHorizontally);
        }

        public static Matrix4x4 CreateTransform2d(
            int srcWidth,
            int srcHeight,
            int dstWidth,
            int dstHeight,
            Rotation srcToDstRotation,
            Vector4 regionOfInterestInTarget,
            bool flipHorizontally
        ) {
            // scale from source coordinates into unit coordinates (0 1 region)
            // have 0.5 shift to achieve pixel centers on full coordinates.
            var transform = Matrix4x4.Scale(new Vector3(1f / srcWidth, 1f / srcHeight, 0)) *
                            Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));

            // adjust coordinate systems. Unity has screen space with 0,0 lower left.
            // we thus subtract the height (1 as we are in unit coordinates) and flip the y axis
            // i.e, 0.3 -> (0.3 - 1) * -1 = 0.7
            transform =
                Matrix4x4.Scale(new Vector3(1, -1, 0)) *
                Matrix4x4.Translate(new Vector3(0, -1, 0)) *
                transform;

            // adjust the rotation difference between source and target
            transform =
                Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * //move it back
                Matrix4x4.Rotate(Quaternion.Euler(0,0,-srcToDstRotation.ToDegree())) * // - z rotation as we are in a left handed coordinated system
                Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0)) * // move image to have it in center for rotation
                transform;

            // ensure that the points are centered with respect to the region of interest and that its scaled
            // according to the rois aspect ratio
            // roi.x, roi.y -> top left corner of the roi rect
            // roi.z, roi.w -> width and height
            transform =
                Matrix4x4.Scale(new Vector3(1f / regionOfInterestInTarget.z, 1 / regionOfInterestInTarget.w, 0)) *
                Matrix4x4.Translate(new Vector3(-regionOfInterestInTarget.x, -regionOfInterestInTarget.y, 0)) * // center in new region
                transform;

            if (flipHorizontally) {
                transform =
                    Matrix4x4.Translate(new Vector3(0.5f,0,0)) *
                    new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1)) *
                    Matrix4x4.Translate(new Vector3(-0.5f,0,0)) *
                    transform;
            }

            // if (flipVertically) {
            //     transform =
            //         Matrix4x4.Translate(new Vector3(0, 0.5f)) *
            //         new Matrix4x4(new Vector4(1, 0), new Vector4(0, -1), new Vector4(0, 0, 1), new Vector4(0, 0, 0, 1)) *
            //         Matrix4x4.Translate(new Vector3(0, -0.5f)) *
            //         transform;
            // }

            // scale the coordinates up again to the target height and width
            transform =
                Matrix4x4.Scale(new Vector3(dstWidth, dstHeight, 0)) * transform;

            // adjust for pixel perfectness. see first step
            transform = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0)) * transform;

            return transform;
        }
    }

}
