/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import com.coachai.engine.ar.ARFrame
import com.coachai.engine.ar.ARPlane
import com.coachai.engine.ar.ARPlaneAlignment
import com.coachai.engine.ar.AugmentedRealitySource
import com.coachai.engine.ar.SessionDelegate
import com.coachai.engine.camera.Camera
import com.coachai.engine.camera.CameraFrame
import com.coachai.engine.camera.VideoSource
import com.coachai.engine.camera.VideoSourceDelegate
import com.coachai.engine.device.Rotation
import com.coachai.engine.image.Image
import com.coachai.engine.math.Float3
import com.coachai.engine.math.Float3x3
import com.coachai.engine.math.Float4x4
import com.coachai.engine.math.make_float3
import com.coachai.engine.math.shape.Anchor
import com.coachai.engine.math.shape.StaticAnchor
import com.coachai.engine.time.MonotonicTimeFraction

class UnitySession : AugmentedRealitySource, VideoSource {
    private val arDelegates: ArrayList<SessionDelegate> = arrayListOf()
    private val cameraDelegates: ArrayList<VideoSourceDelegate> = arrayListOf()
    private var camera: Camera = Camera()

    fun updateCamera(sensorRotation: Rotation,
                     focalLength: FloatArray,
                     principalPoint: FloatArray,
                     imageDimensions: IntArray,
                     lensFacing: Camera.LensFacing
    ) {
        camera = Camera(
                intrinsicsMatrix = Float3x3(
                        floatArrayOf(
                                focalLength[0], 0f, 0f,
                                0f, focalLength[1], 0f,
                                principalPoint[0], principalPoint[1], 1f
                        )
                ),
                imageDimension = imageDimensions[0] to imageDimensions[1],
                cameraPose = Float4x4(),
                viewMatrix = Float4x4(),
                sensorRotation = sensorRotation,
                lensFacing = lensFacing
        )
    }

    private fun asFloat4x4(translation: FloatArray, quaternion: FloatArray): Float4x4 {
        val (tx, ty, tz) = translation
        var (x, y, z, w) = quaternion
        var var12 = x * x + y * y + z * z + w * w
        var12 = if (var12 > 0.0f) {
            2.0f / var12
        } else {
            0.0f
        }

        var var7 = x * var12
        var var8 = y * var12
        var12 *= z
        val var9 = w * var7
        val var10 = w * var8
        w *= var12
        var7 *= x
        val var11 = x * var8
        x *= var12
        var8 *= y
        y *= var12
        var12 *= z

        // column-major
        return Float4x4(
                floatArrayOf(
                        1.0f - (var8 + var12), var11 + w, x - var10, 0f,
                        var11 - w, 1.0f - (var7 + var12), y + var9, 0f,
                        x + var10, y - var9, 1.0f - (var7 + var8), 0f,
                        tx, ty, tz, 1f
                )
        )
    }

    private fun updateCameraPose(translation: FloatArray, quaternion: FloatArray) {
        val pose = asFloat4x4(translation, quaternion)
        camera = camera.copy(
                cameraPose = pose,
                viewMatrix = pose.inverse()
        )
    }

    override fun unsubscribe(sessionDelegate: SessionDelegate) {
        arDelegates.remove(sessionDelegate)
    }

    override fun subscribe(sessionDelegate: SessionDelegate) {
        arDelegates.add(sessionDelegate)
    }

    private class UnityARFrame(
        override val image: Image,
        override val camera: Camera,
        override val trackablePlanes: Collection<ARPlane>,
        override val timestamp: MonotonicTimeFraction
    ) : ARFrame {
        /**
         * Returns the required rotation constant to rotate current camera image to UI screen orientation
         */
        override val rotation: Rotation = camera.imageToScreenRotation()

        override fun release() = image.release()

        override fun retain() = image.retain()
    }

    private class UnityARPlane(
            extentXYZ: FloatArray,
            override val pose: Float4x4,
            private val polygonArray: FloatArray
    ) : ARPlane {
        override val anchor: Anchor by lazy { StaticAnchor(pose) }

        override val size: Float = extentXYZ[0] * extentXYZ[2]

        override val float3Extent: Float3 = make_float3(extentXYZ[0], 0f, extentXYZ[2])

        override val float3Location: Float3 = pose.translation()

        override val float3RelativeCenter: Float3 = make_float3(0f, 0f, 0f)

        override val float3Geometry: List<Float3>
            by lazy {
                val geometry = mutableListOf<Float3>()
                for (i in 0 until (polygonArray.size) step 2) {
                    geometry.add(make_float3(polygonArray[i], 0f, polygonArray[i + 1]))
                }
                geometry
            }

        override val alignment: ARPlaneAlignment = ARPlaneAlignment.HORIZONTAL
    }

    private fun UnityArPlaneData.toArPlane(): ARPlane {
        return UnityARPlane(extentXYZ, asFloat4x4(centerPosePosition, centerPoseRotation), polygonArray)
    }

    fun update(image: Image,
               cameraPosition: FloatArray,
               cameraRotation: FloatArray,
               planes: Array<out UnityArPlaneData>,
               timestamp: MonotonicTimeFraction) {
        val arPlanes = planes.map { it.toArPlane() }
        updateCameraPose(cameraPosition, cameraRotation)
        val frame = UnityARFrame(image, camera, arPlanes, timestamp)
        arDelegates.forEach {
            it.session(frame)
        }
        cameraDelegates.forEach {
            it.session(frame)
        }
        frame.release()
    }

    fun update(frame: CameraFrame) {
        cameraDelegates.forEach {
            it.session(frame)
        }
        frame.release()
    }

    override fun subscribe(videoSourceDelegate: VideoSourceDelegate) {
        cameraDelegates.add(videoSourceDelegate)
    }

    override fun unsubscribe(videoSourceDelegate: VideoSourceDelegate) {
        cameraDelegates.remove(videoSourceDelegate)
    }
}
