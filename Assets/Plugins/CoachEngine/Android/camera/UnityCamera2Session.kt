/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.camera

import android.annotation.SuppressLint
import android.content.Context
import android.content.Context.CAMERA_SERVICE
import android.graphics.ImageFormat
import android.graphics.Rect
import android.hardware.camera2.CameraCaptureSession
import android.hardware.camera2.CameraCharacteristics
import android.hardware.camera2.CameraDevice
import android.hardware.camera2.CameraManager
import android.hardware.camera2.CameraMetadata
import android.hardware.camera2.CaptureRequest
import android.hardware.camera2.CaptureResult
import android.hardware.camera2.TotalCaptureResult
import android.media.ImageReader
import android.os.Handler
import android.os.HandlerThread
import android.util.Range
import android.util.Size
import android.util.SizeF
import android.view.Surface
import com.coachai.engine.buffer.ByteBufferPool
import com.coachai.engine.camera.Camera
import com.coachai.engine.camera.CameraFrame
import com.coachai.engine.camera.CameraFrameAndroid
import com.coachai.engine.camera.CameraSessionConfig
import com.coachai.engine.camera.VideoSource
import com.coachai.engine.camera.VideoSourceDelegate
import com.coachai.engine.device.Rotation
import com.coachai.engine.device.SensorRotation
import com.coachai.engine.image.Yuv420Image
import com.coachai.engine.logging.Logger
import com.coachai.engine.math.Float3x3
import com.coachai.engine.math.Float4x4
import com.coachai.engine.math.make_float2
import java.util.concurrent.CopyOnWriteArrayList
import java.util.concurrent.Semaphore
import java.util.concurrent.atomic.AtomicInteger

// duplicate of com.coachai.engine.camera.camera2.Camera2Session
/**
 * Camera session implementation based on Camera2 API.
 */
class UnityCamera2Session(
    context: Context,
    private var _sessionConfig: CameraSessionConfig,
    private val onCameraOpened: (UnityCamera2Session) -> Unit
) : CameraCaptureSession.CaptureCallback(), VideoSource, ImageReader.OnImageAvailableListener {

    /** Detects, characterizes, and connects to a CameraDevice (used for all camera operations) */
    private val cameraManager: CameraManager = context.getSystemService(CAMERA_SERVICE) as CameraManager
    private val logger = Logger("UnityCamera2Session")

    class CameraConfig(
            val cameraId: String,
            val characteristics: CameraCharacteristics,
            val imageSize: Size,
            val imageFormat: Int,
            val fpsRange: Range<Int>
    )

    val sessionConfig: CameraSessionConfig
        get() = _sessionConfig

    var cameraConfig: CameraConfig = configure(sessionConfig)
        private set

    /** [HandlerThread] where all camera operations run */
    private val cameraThread = HandlerThread("CameraThread").apply { start() }

    /** [Handler] corresponding to [cameraThread] */
    private val cameraHandler = Handler(cameraThread.looper)

    private val imageReader: ImageReader = ImageReader.newInstance(
            cameraConfig.imageSize.width,
            cameraConfig.imageSize.height,
            cameraConfig.imageFormat,
            IMAGE_BUFFER_SIZE
    ).also {
        it.setOnImageAvailableListener(this, cameraHandler)
    }
    private val pool = ByteBufferPool(IMAGE_BUFFER_SIZE)
    private val maxImages = Semaphore(IMAGE_BUFFER_SIZE)

    /** Where the camera preview is displayed */
    var previewSurface: Surface? = null

    private var surfaces: MutableList<Surface> = mutableListOf()

    private lateinit var previewBuilder: CaptureRequest.Builder

    /** Internal reference to the ongoing [CameraCaptureSession] configured with our parameters */
    private lateinit var session: CameraCaptureSession

    /**
     * This function:
     * - Opens the camera
     * - Configures the camera session
     * - Starts the preview by dispatching a repeating capture request
     * - Sets up the image reader listeners
     */
    private fun initializeCamera() {
        openCamera { camera ->
            previewBuilder = camera.createCaptureRequest(
                    CameraDevice.TEMPLATE_PREVIEW)

            // Create list of Surfaces where the camera will output frames
            surfaces.add(imageReader.surface)
            previewBuilder.addTarget(imageReader.surface)

            onCameraOpened(this)

            previewSurface?.also {
                surfaces.add(it)
                previewBuilder.addTarget(it)
            }

            startCaptureSession(camera) {
                session = it
                // This will keep sending the capture request as frequently as possible until the
                // session is torn down or session.stopRepeating() is called
                session.setRepeatingRequest(previewBuilder.build(), this, cameraHandler)
            }
        }
    }

    private lateinit var sensorOrientation: Rotation
    private lateinit var lensFacing: Camera.LensFacing
    private lateinit var physicalSensorSize: SizeF
    private lateinit var pixelArraySize: Size

    // TODO: if CaptureRequest#DISTORTION_CORRECTION_MODE off:
    //  use CameraCharacteristics#SENSOR_INFO_PRE_CORRECTION_ACTIVE_ARRAY_SIZE
    //  instead of CameraCharacteristics.SENSOR_INFO_ACTIVE_ARRAY_SIZE
    private lateinit var activeArraySize: Rect

    private fun configureCamera(characteristics: CameraCharacteristics,
                                captureRequestBuilder: CaptureRequest.Builder) {
        captureRequestBuilder.set(CaptureRequest.CONTROL_AF_MODE, CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO)
        captureRequestBuilder.set(CaptureRequest.CONTROL_AE_MODE, CaptureRequest.CONTROL_AE_MODE_ON)
        captureRequestBuilder.set(CaptureRequest.CONTROL_AE_TARGET_FPS_RANGE, cameraConfig.fpsRange)
        sensorOrientation = when (characteristics.get(CameraCharacteristics.SENSOR_ORIENTATION)
                ?: 0) {
            0 -> Rotation.Rotate0Degree
            90 -> Rotation.Rotate90Degree
            180 -> Rotation.Rotate180Degree
            270 -> Rotation.Rotate270Degree
            else -> error("Unsupported camera sensor orientation")
        }
        lensFacing = when (characteristics.get(CameraCharacteristics.LENS_FACING)
                ?: CameraMetadata.LENS_FACING_BACK) {
            CameraMetadata.LENS_FACING_FRONT -> Camera.LensFacing.FRONT
            CameraMetadata.LENS_FACING_BACK -> Camera.LensFacing.BACK
            CameraMetadata.LENS_FACING_EXTERNAL -> Camera.LensFacing.EXTERNAL
            else -> error("Unsupported camera lens facing")
        }

        // The physical dimensions of the full pixel array in millimeters supported on all devices
        physicalSensorSize = characteristics.get(CameraCharacteristics.SENSOR_INFO_PHYSICAL_SIZE)!!
        pixelArraySize = characteristics.get(CameraCharacteristics.SENSOR_INFO_PIXEL_ARRAY_SIZE)!!
        activeArraySize = characteristics.get(CameraCharacteristics.SENSOR_INFO_ACTIVE_ARRAY_SIZE)!!
    }

    private val delegates: CopyOnWriteArrayList<VideoSourceDelegate> = CopyOnWriteArrayList()

    private var lastFrameTime: Long = 0

    // Sufficient to run a session for quite some time...
    private var frameCount: Long = 0
    private var lastFpsReport: Long = 0

    override fun onImageAvailable(imageReader: ImageReader?) {
        val reader = imageReader ?: return
        if (reader.maxImages == 0) {
            logger.warn("No images available. Seems image.close() not been called?")
            return
        }
        val image = try {
            reader.acquireLatestImage()
        } catch (e: IllegalStateException) {
            logger.warn("Too many images are currently acquired!")
            return
        }

        if (image == null) {
            logger.debug { "No image data available!" }
            return
        }

        if (!::captureResult.isInitialized || delegates.isEmpty()) {
            image.close()
            return
        }

        frameCount++

        val currentTimestamp = image.timestamp
        if (currentTimestamp <= lastFrameTime) {
            image.close()
            return
        }
        lastFrameTime = currentTimestamp
        // Report once per second FPS
        if ((currentTimestamp - lastFpsReport) > 1_000_000_000) {
            logger.debug { "fps: $frameCount" }
            lastFpsReport = currentTimestamp
            frameCount = 0
        }

        val cameraCharacteristics = cameraConfig.characteristics
        val width = image.width
        val height = image.height

        // TODO: Read out sensor data once, not in every frame. Sensor data shouldn't change?
        val (f_x, f_y, c_x, c_y, s) = when {
            android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.M &&
                    cameraCharacteristics.get(CameraCharacteristics.LENS_INTRINSIC_CALIBRATION) != null
            -> requireNotNull(cameraCharacteristics.get(CameraCharacteristics.LENS_INTRINSIC_CALIBRATION))
            android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.M &&
                    captureResult.get(CaptureResult.LENS_INTRINSIC_CALIBRATION) != null
            -> requireNotNull(captureResult.get(CaptureResult.LENS_INTRINSIC_CALIBRATION))
            captureResult.get(CaptureResult.LENS_FOCAL_LENGTH) != null
            -> getCameraIntrinsicsManually(width, height)
            else -> {
                image.close()
                error("Unable to retrieve lens intrinsics calibration")
            }
        }

        val camera = Camera(
                intrinsicsMatrix = Float3x3(floatArrayOf(
                        f_x, 0f, 0f,
                        s, f_y, 0f,
                        c_x, c_y, 1f
                )),
                imageDimension = width to height,
                cameraPose = Float4x4.identity,
                viewMatrix = Float4x4.identity,
                sensorRotation = sensorOrientation,
                lensFacing = lensFacing
        )
        if (!maxImages.tryAcquire()) {
            logger.warn("Rejected inbound camera image because too many images are currently processing!")
            image.close()
            return
        }
        val frame = CameraFrameAndroid(Yuv420Image.copyFrom(image, pool, false), currentTimestamp, camera)
        logger.debug { "w: ${image.width}; h: ${image.height}; r: ${frame.rotation}; t: ${frame.timestamp}" }
        image.close()
        val refFrame = object : CameraFrame by frame {
            val refCount = AtomicInteger(1)
            override fun retain() {
                frame.retain()
                refCount.incrementAndGet()
            }

            override fun release() {
                frame.release()
                if (refCount.decrementAndGet() <= 0) {
                    maxImages.release()
                }
            }
        }
        try {
            for (delegate in delegates) {
                delegate.session(refFrame)
            }
        } finally {
            refFrame.release()
        }
    }

    /**
     * calculate fx, fy, cx, cy manually with the info provided by the camera
     */
    private fun getCameraIntrinsicsManually(dstWidth: Int, dstHeight: Int): FloatArray {
        // The lens focal length in millimeters supported on all devices
        val focalLength = captureResult.get(CaptureResult.LENS_FOCAL_LENGTH)!!

        val cropRegion = captureResult.get(CaptureResult.SCALER_CROP_REGION)!!

        val cropAspect = cropRegion.width().toFloat() / cropRegion.height().toFloat()
        val dstAspect = dstWidth.toFloat() / dstHeight.toFloat()

        @Suppress("LocalVariableName") val f_full = make_float2(
                focalLength / physicalSensorSize.width * pixelArraySize.width,
                focalLength / physicalSensorSize.height * pixelArraySize.height
        )

        @Suppress("LocalVariableName") val c_full = make_float2(
                pixelArraySize.width / 2f,
                pixelArraySize.height / 2f
        )

        val shiftActiveArray = make_float2(
                ((pixelArraySize.width - activeArraySize.right) - activeArraySize.left).toFloat(),
                ((pixelArraySize.height - activeArraySize.bottom) - activeArraySize.top).toFloat()
        )

        val shiftCrop = make_float2(
                ((activeArraySize.width() - cropRegion.right) - cropRegion.left).toFloat(),
                ((activeArraySize.height() - cropRegion.bottom) - cropRegion.top).toFloat()
        )

        // from the cropped region, our image is cropped further. Check aspect ratio
        val unscaledImageSize: Size = when {
            dstAspect == cropAspect -> Size(cropRegion.width(), cropRegion.height())
            dstAspect > cropAspect -> Size(cropRegion.width(), (cropRegion.width() / dstAspect).toInt()) // crop vertically
            else -> Size((cropRegion.height() / dstAspect).toInt(), cropRegion.height()) // crop horizontally
        }
        val scaleFactor = dstWidth.toFloat() / unscaledImageSize.width

        val shiftAspect = make_float2(
                (unscaledImageSize.width - cropRegion.width()) / 2f,
                (unscaledImageSize.height - cropRegion.height()) / 2f
        )

        @Suppress("LocalVariableName") val f_scaled = f_full * scaleFactor

        @Suppress("LocalVariableName") val c_scaled = (c_full + shiftActiveArray + shiftCrop + shiftAspect) * scaleFactor

        return floatArrayOf(
                f_scaled.x,
                f_scaled.y,
                c_scaled.x,
                c_scaled.y,
                0f
        )
    }

    private fun createCameraConfig(sessionConfig: CameraSessionConfig): CameraConfig? {
        val cameraId = cameraManager.getLensCameraIds(sessionConfig.position.toLensFacing()).firstOrNull()
        cameraId ?: return null

        val characteristics: CameraCharacteristics = cameraManager.getCameraCharacteristics(cameraId)

        val cameraSize = chooseOptimalResolution(
                sessionConfig.targetWidth,
                sessionConfig.targetHeight,
                characteristics,
                sessionConfig.imageFormat ?: DEFAULT_IMAGE_FORMAT
        )
        val fpsRange = chooseOptimalFpsRange(Range(sessionConfig.targetFPSRange.first, sessionConfig.targetFPSRange.last), characteristics)
        fpsRange ?: return null

        return CameraConfig(cameraId, characteristics, cameraSize, sessionConfig.imageFormat
                ?: DEFAULT_IMAGE_FORMAT, fpsRange)
    }

    /** Opens the camera */
    @SuppressLint("MissingPermission")
    private fun openCamera(cb: (CameraDevice) -> Unit) {
        cameraManager.openCamera(this.cameraConfig.cameraId, object : CameraDevice.StateCallback() {
            override fun onDisconnected(device: CameraDevice) {
                logger.warn("Camera $this.camera.cameraId has been disconnected")
                // TODO: finish for activity or exception?
            }

            override fun onError(device: CameraDevice, error: Int) {
                val exc = RuntimeException("Camera $cameraConfig.cameraId open error: $error")
                logger.error(exc, exc.message!!)
                throw exc
            }

            override fun onOpened(device: CameraDevice) = cb(device)
        }, cameraHandler)
    }

    /**
     * Starts a [CameraCaptureSession].
     */
    private fun startCaptureSession(device: CameraDevice, cb: (CameraCaptureSession) -> Unit) {
        configureCamera(cameraConfig.characteristics, previewBuilder)

        // Create a capture session using the predefined targets; this also involves defining the
        // session state callback to be notified of when the session is ready
        device.createCaptureSession(surfaces, object : CameraCaptureSession.StateCallback() {
            override fun onConfigureFailed(session: CameraCaptureSession) {
                val exc = RuntimeException(
                        "Camera ${device.id} session configuration failed, see log for details")
                logger.error(exc, exc.message!!)
                throw exc
            }

            override fun onConfigured(session: CameraCaptureSession) = cb(session)
        }, cameraHandler)
    }

    fun stop() {
        logger.verbose { "$TAG: stop" }

        surfaces.clear()
        if (::session.isInitialized) {
            session.close()
            session.device.close()
        }
    }

    fun destroy() {
        logger.verbose { "$TAG: destroy" }
        cameraThread.quitSafely()
    }

    /**
     * Starts the session.
     */
    fun start() {
        logger.verbose { "$TAG: start" }
        initializeCamera()
    }

    fun getCameraWidth(): Int {
        return if (getRotation() == 90 || getRotation() == 270) cameraConfig.imageSize.height else
            cameraConfig.imageSize.width
    }

    fun getCameraHeight(): Int {
        return if (getRotation() == 90 || getRotation() == 270) cameraConfig.imageSize.width else
            cameraConfig.imageSize.height
    }

    fun getCameraId(): String = cameraConfig.cameraId

    fun getRotation(): Int = cameraConfig.characteristics.get(CameraCharacteristics.SENSOR_ORIENTATION)
            ?: 0

    protected fun finalize() {
        destroy()
    }

    private fun configure(cameraSessionConfig: CameraSessionConfig): CameraConfig {
        return createCameraConfig(cameraSessionConfig)
                ?: error("Invalid camera session parameters!")
    }

    /**
     * Restarts the session:
     * - stop session
     * - apply new params
     * - start session again
     */
    fun restart(cameraSessionConfig: CameraSessionConfig) {
        logger.verbose { "$TAG: restart" }
        stop()
        cameraConfig = configure(cameraSessionConfig)
        this._sessionConfig = cameraSessionConfig
        start()
    }

    override fun subscribe(videoSourceDelegate: VideoSourceDelegate) {
        delegates.add(videoSourceDelegate)
    }

    override fun unsubscribe(videoSourceDelegate: VideoSourceDelegate) {
        delegates.remove(videoSourceDelegate)

        if (delegates.isEmpty()) {
            SensorRotation.stopSensorRotation()
        }
    }

    private lateinit var captureResult: CaptureResult

    override fun onCaptureCompleted(session: CameraCaptureSession, request: CaptureRequest, result: TotalCaptureResult) {
        captureResult = result
    }

    companion object {
        private val TAG = UnityCamera2Session::class.java.simpleName

        /** Maximum number of images that will be held in the reader's buffer */
        private const val IMAGE_BUFFER_SIZE: Int = 16

        /**
         * The camera session produces images [ImageFormat.YUV_420_888] format
         */
        private const val DEFAULT_IMAGE_FORMAT = ImageFormat.YUV_420_888
    }
}
