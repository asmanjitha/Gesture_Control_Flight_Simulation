/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.camera

import android.content.Context
import android.content.Context.CAMERA_SERVICE
import android.hardware.camera2.CameraCharacteristics
import android.hardware.camera2.CameraManager
import android.hardware.camera2.CameraMetadata
import android.util.Log
import android.util.Range
import android.util.Size
import com.coachai.engine.unity.COACH_TAG
import kotlin.Comparator
import kotlin.collections.ArrayList
import kotlin.math.abs
import kotlin.math.sign
import com.coachai.engine.camera.Camera
import com.coachai.engine.camera.Position
import com.coachai.engine.device.Rotation

/**
 * Gets the camera ids with the specified lens facing. Returns empty list if there's no
 * camera with specified lens facing.
 */
internal fun CameraManager.getLensCameraIds(lensFacing: Int): List<String> {
    return cameraIdList.mapNotNull { cameraId: String ->
        val characteristics = getCameraCharacteristics(cameraId)
        val facing = characteristics.get(CameraCharacteristics.LENS_FACING)
        if (facing == null || facing != lensFacing) null else cameraId
    }.toList()
}

/**
 * Converts [Position] to lens facing values of [CameraCharacteristics]
 */
internal fun Position.toLensFacing(): Int {
    return when (this) {
        Position.FRONT -> CameraCharacteristics.LENS_FACING_FRONT
        Position.BACK -> CameraCharacteristics.LENS_FACING_BACK
    }
}

internal fun Context.getSensorRotation(lensFacing: Camera.LensFacing) : Rotation {
    val cameraManager = getSystemService(CAMERA_SERVICE) as CameraManager

    for (cameraId in cameraManager.cameraIdList) {
        val characteristics = cameraManager.getCameraCharacteristics(cameraId)

        val cameraLensFacing = when (characteristics[CameraCharacteristics.LENS_FACING]) {
            CameraMetadata.LENS_FACING_FRONT -> Camera.LensFacing.FRONT
            null, CameraMetadata.LENS_FACING_BACK -> Camera.LensFacing.BACK
            CameraMetadata.LENS_FACING_EXTERNAL -> Camera.LensFacing.EXTERNAL
            else -> error("Unsupported camera lens facing")
        }

        if (cameraLensFacing != lensFacing) continue

        return when (characteristics[CameraCharacteristics.SENSOR_ORIENTATION]) {
            null, 0 -> Rotation.Rotate0Degree
            90 -> Rotation.Rotate90Degree
            180 -> Rotation.Rotate180Degree
            270 -> Rotation.Rotate270Degree
            else -> error("Unsupported camera sensor orientation")
        }
    }

    error("Could not determine sensor rotation")
}

/**
 * Choose the smallest resolution supported by camera that is at least as large as the respective texture view size,
 * and whose aspect ratio matches to the aspect ratio of target width and height.
 * If such size doesn't exist, choose the largest one,
 * and whose aspect ratio matches with the aspect ratio of target width and height.
 *
 * @param targetWidth The target width
 * @param targetHeight The target height
 * @param characteristics The capabilities of a camera device
 * @param format The target image format
 * @return Returns null if there's no supported resolutions by camera.
 */
internal fun chooseOptimalResolution(
        targetWidth: Int,
        targetHeight: Int,
        characteristics: CameraCharacteristics,
        format: Int
): Size {
    // If image format is provided, use it to determine supported sizes; else use target class
    val config = characteristics.get(
            CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP)!!
    require(config.isOutputSupportedFor(format))

    return chooseOptimalResolution(config.getOutputSizes(format), targetWidth, targetHeight)
}

/**
 * Choose the optimal fps range supported by camera.
 * @param targetFpsRange The target fps range
 * @param characteristics The capabilities of a camera device
 * @return Returns null if there's no supported fps ranges by camera.
 */
internal fun chooseOptimalFpsRange(targetFpsRange: Range<Int>, characteristics: CameraCharacteristics): Range<Int>? {
    val allRanges = characteristics.get(
            CameraCharacteristics.CONTROL_AE_AVAILABLE_TARGET_FPS_RANGES) ?: return null
    var bestRange: Range<Int>? = null
    var bestDelta = 0
    allRanges.reversed().forEach {
        if (bestRange == null) {
            bestRange = it
        } else {
            val delta = abs(it.lower - targetFpsRange.lower) + abs(it.upper - targetFpsRange.upper)
            if (delta <= bestDelta) {
                bestDelta = delta
                bestRange = it
            }
        }
    }
    return bestRange
}

/**
 * Compares two `Size`s based on their areas.
 */
private class CompareSizesByArea : Comparator<Size> {

    // We cast here to ensure the multiplications won't overflow
    override fun compare(lhs: Size, rhs: Size) =
            (lhs.width.toLong() * lhs.height - rhs.width.toLong() * rhs.height).sign
}

/**
 * Given `choices` of `Size`s supported by a camera, choose the smallest one that is at least as large as the respective texture view size,
 * and that is at most as large as the respective max size, and whose aspect ratio matches with the specified value.
 * If such size doesn't exist, choose the largest one that is at most as large as the respective max size,
 * and whose aspect ratio matches with the specified value.
 *
 * @param choices The list of sizes that the camera supports for the intended output class
 * @param targetWidth The target width
 * @param targetHeight The target height
 * @return The optimal `Size`, or an arbitrary one if none were big enough
 */
private fun chooseOptimalResolution(
        choices: Array<Size>,
        targetWidth: Int,
        targetHeight: Int,
): Size {
    // Collect the supported resolutions that are at least as big as the preview Surface
    val bigEnough = ArrayList<Size>()
    // Collect the supported resolutions that are smaller than the preview Surface
    val notBigEnough = ArrayList<Size>()
    for (option in choices) {
        // NOTE: We check for the closest resolution, rather than using aspect ratio
        // unlike com.coachai.engine.camera.camera2.chooseOptimalResolution
        if (option.width >= targetWidth && option.height >= targetHeight
                || option.width >= targetHeight && option.height >= targetWidth) {
            bigEnough.add(option)
        } else {
            notBigEnough.add(option)
        }
    }

    // Pick the smallest of those big enough. If there is no one big enough, pick the
    // largest of those not big enough.
    return when {
        bigEnough.size > 0 -> bigEnough.minOfWith(CompareSizesByArea()) { it }
        notBigEnough.size > 0 -> notBigEnough.maxOfWith(CompareSizesByArea()) { it }
        else -> {
            Log.w(COACH_TAG, "Couldn't find any suitable preview size")
            choices[0]
        }
    }
}
