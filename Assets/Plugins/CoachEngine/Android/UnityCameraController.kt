/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import android.content.Context.DISPLAY_SERVICE
import android.hardware.display.DisplayManager
import android.media.ImageReader
import android.media.ImageWriter
import android.util.Log
import android.view.Display
import android.view.Surface
import com.coachai.engine.buffer.ByteBufferPool
import com.coachai.engine.camera.Camera
import com.coachai.engine.camera.CameraFrame
import com.coachai.engine.camera.CameraSessionConfig
import com.coachai.engine.camera.Position
import com.coachai.engine.camera.VideoSourceDelegate
import com.coachai.engine.device.Rotation
import com.coachai.engine.image.Yuv420Image
import com.coachai.engine.time.MonotonicTimeFraction
import com.coachai.engine.time.monotonicTimeFraction
import com.coachai.engine.unity.camera.UnityCamera2Session
import com.coachai.engine.unity.camera.UnityCameraRenderer
import com.coachai.engine.unity.camera.UnityEglConfig
import com.coachai.engine.unity.camera.getSensorRotation
import com.unity3d.player.UnityPlayer.currentActivity
import java.nio.ByteBuffer

@Suppress("UNUSED")
object UnityCameraController {
    private var cameraConfig: UnityCameraConfig? = null
    private var cameraSession: UnityCamera2Session? = null
    private var cameraRenderer: UnityCameraRenderer? = null

    private var sessionReady = false
    private var firstFrameMonotonicTimeFraction: MonotonicTimeFraction = 0.0
    private var firstFrameTimestamp: Double = 0.0
    private lateinit var pool: ByteBufferPool
    private var reader: ImageReader? = null
    private var writer: ImageWriter? = null

    fun updateFrame(
        imageYBuffer: ByteBuffer,
        imageUBuffer: ByteBuffer,
        imageVBuffer: ByteBuffer,
        timestamp: Double,
        cameraPosePosition: FloatArray,
        cameraPoseRotation: FloatArray,
        unityPlanes: Array<out UnityArPlaneData>?
    ) {
        if (!sessionReady) {
            sessionReady = true
            firstFrameMonotonicTimeFraction = monotonicTimeFraction()
            firstFrameTimestamp = timestamp
        }

        writer?.dequeueInputImage()?.use { newImage ->
            val (y, u, v) = newImage.planes
            y.buffer.slice().put(imageYBuffer.duplicate())
            u.buffer.slice().put(imageUBuffer.duplicate())
            v.buffer.slice().put(imageVBuffer.duplicate())
            val image = Yuv420Image.copyFrom(newImage, pool, false)
            val planes = unityPlanes ?: emptyArray()
            val currentFrameMonotonicTimeFraction = timestamp - firstFrameTimestamp + firstFrameMonotonicTimeFraction
            CoachSdk.unitySession.update(image, cameraPosePosition, cameraPoseRotation, planes, currentFrameMonotonicTimeFraction)
        }
    }

    // this method must be invoked from one of Unity's render threads
    fun setupCameraSession(useFrontFacingCamera: Boolean, requestedWidth: Int, requestedHeight: Int) {
        if (cameraSession != null) return
        // init context and create output texture while on Unity render thread
        UnityEglConfig.initialize()
        val outputTexture = UnityCameraRenderer.createOutputTexture()
        cameraSession = UnityCamera2Session(
            currentActivity,
            CameraSessionConfig.build {
                imageFormat = null
                targetFPSRange = 20..30
                targetWidth = requestedWidth
                targetHeight = requestedHeight
                position = if (useFrontFacingCamera) Position.FRONT else Position.BACK
            },
            onCameraOpened = {
                cameraRenderer = UnityCameraRenderer(outputTexture, it.getCameraWidth(), it.getCameraHeight(), it.getRotation())
                it.previewSurface = cameraRenderer!!.surface
            }
        ).apply {
            subscribe(object : VideoSourceDelegate {
                override fun session(didUpdate: CameraFrame) {
                    didUpdate.image?.let {
                        CoachSdk.unitySession.update(didUpdate)
                    }
                }
            })
            start()
            Log.d(COACH_TAG, "camera session successfully started")
        }
    }

    // this method must be invoked from one of Unity's render threads
    fun destroyCameraSession() {
        cameraRenderer?.also {
            cameraRenderer = null
            it.release()
        }

        cameraSession?.also {
            cameraSession = null
            it.stop()
            it.destroy()
            Log.d(COACH_TAG, "camera session successfully destroyed")
        }
    }

    fun getOutputTexture(): Int {
        return cameraRenderer?.outputTexture ?: 0
    }

    fun getCameraOrientation(): Int {
        val displayManager = currentActivity.getSystemService(DISPLAY_SERVICE) as DisplayManager
        val display = displayManager.getDisplay(Display.DEFAULT_DISPLAY)
        return when (display.rotation) {
            Surface.ROTATION_0 -> 0
            Surface.ROTATION_90 -> 90
            Surface.ROTATION_180 -> 180
            Surface.ROTATION_270 -> 270
            else -> error("Unknown rotation!")
        }
    }

    fun getCameraWidth() = cameraSession?.getCameraWidth() ?: -1

    fun getCameraHeight() = cameraSession?.getCameraHeight() ?: -1

    fun pauseCameraSession() {
        cameraSession?.stop()
    }

    fun resumeCameraSession() {
        cameraSession?.start()
    }

    fun updateConfig(
        cameraConfig: UnityCameraConfig,
        lensFacing: Int,
        sensorRotationDegrees: Float
    ) {
        if (!::pool.isInitialized) {
            pool = ByteBufferPool(13 * 6)
        }
        this.cameraConfig = cameraConfig
        val lensFacingEnum = Camera.LensFacing.values().first { it.ordinal == lensFacing }
        val sensorRotation = if (sensorRotationDegrees.isNaN()) {
            currentActivity.getSensorRotation(lensFacingEnum)
        } else {
            Rotation.degreeToRotation(sensorRotationDegrees)
        }
        updateConfig(cameraConfig, lensFacingEnum, sensorRotation)
    }

    private fun updateConfig(
        cameraConfig: UnityCameraConfig,
        lensFacing: Camera.LensFacing,
        sensorRotation: Rotation
    ) {
        val (focalLength, principalPoint, imageDimensions) = cameraConfig
        val (width, height) = imageDimensions
        writer?.close()
        reader?.close()
        ImageReader.newInstance(width, height, android.graphics.ImageFormat.YUV_420_888, 2).also {
            reader = it
            writer = ImageWriter.newInstance(it.surface, 32)
        }

        CoachSdk.unitySession.updateCamera(sensorRotation, focalLength, principalPoint, imageDimensions, lensFacing)
    }
}
