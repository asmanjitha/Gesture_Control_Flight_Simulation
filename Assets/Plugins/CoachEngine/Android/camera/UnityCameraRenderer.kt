/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.camera

import android.graphics.SurfaceTexture
import android.opengl.*
import android.util.Log
import android.view.Surface
import com.coachai.engine.unity.COACH_TAG
import javax.microedition.khronos.opengles.GL10

/**
 * Sets up a new egl context, handles camera texture & SurfaceTexture.
 */
class UnityCameraRenderer(
    val outputTexture: Int, cameraWidth: Int, cameraHeight: Int, rotation: Int
) : SurfaceTexture.OnFrameAvailableListener {
    private val cameraTexture: Int
    private val transformationMatrix = FloatArray(16)
    private val surfaceTexture: SurfaceTexture
    private var fboRenderer: FboRenderer?

    private var eglDisplay = EGL14.EGL_NO_DISPLAY
    private var eglContext = EGL14.EGL_NO_CONTEXT
    private var eglSurface = EGL14.EGL_NO_SURFACE
    private var eglConfig: EGLConfig? = null

    val surface: Surface

    init {
        initEgl(cameraWidth, cameraHeight)
        makeCurrent()
        fboRenderer = FboRenderer(cameraWidth, cameraHeight, outputTexture)
        cameraTexture = createCameraTexture()
        surfaceTexture = SurfaceTexture(cameraTexture)
        surface = Surface(surfaceTexture)

        val bufferWidth = if (rotation == 90 || rotation == 270) cameraHeight else cameraWidth
        val bufferHeight = if (rotation == 90 || rotation == 270) cameraWidth else cameraHeight
        surfaceTexture.setDefaultBufferSize(bufferWidth, bufferHeight)
        surfaceTexture.setOnFrameAvailableListener(this)
    }

    private fun getConfig(version: Int): EGLConfig? {
        val renderableType = if (version >= 3) EGLExt.EGL_OPENGL_ES3_BIT_KHR else EGL14.EGL_OPENGL_ES2_BIT

        // The actual surface is generally RGBA or RGBX, so situationally omitting alpha
        // doesn't really help.  It can also lead to a huge performance hit on glReadPixels()
        // when reading into a GL_RGBA buffer.
        val attribList = intArrayOf(
            EGL14.EGL_RED_SIZE, 8,
            EGL14.EGL_GREEN_SIZE, 8,
            EGL14.EGL_BLUE_SIZE, 8,
            EGL14.EGL_ALPHA_SIZE, 8,
            EGL14.EGL_RENDERABLE_TYPE, renderableType,
            EGL14.EGL_NONE, 0,
            EGL14.EGL_NONE
        )
        attribList[attribList.size - 3] = EGL_RECORDABLE_ANDROID
        attribList[attribList.size - 2] = 1
        val configs = arrayOfNulls<EGLConfig>(1)
        val numConfigs = IntArray(1)
        if (!EGL14.eglChooseConfig(eglDisplay, attribList, 0, configs, 0, configs.size,
                numConfigs, 0)) {
            Log.w(TAG, "unable to find RGB8888 / $version EGLConfig")
            return null
        }
        return configs[0]
    }

    //    TODO: Check if a new context needs to be created for single threaded mode.
    private fun initEgl(width: Int, height: Int) {
        eglDisplay = EGL14.eglGetDisplay(EGL14.EGL_DEFAULT_DISPLAY)
        if (eglDisplay === EGL14.EGL_NO_DISPLAY) {
            throw java.lang.RuntimeException("unable to get EGL14 display")
        }
        val version = IntArray(2)
        if (!EGL14.eglInitialize(eglDisplay, version, 0, version, 1)) {
            eglDisplay = null
            throw java.lang.RuntimeException("unable to initialize EGL14")
        }

        val config: EGLConfig? = getConfig(3)
        val attrib3List = intArrayOf(
            EGL14.EGL_CONTEXT_CLIENT_VERSION, 3,
            EGL14.EGL_NONE
        )
        val context = EGL14.eglCreateContext(eglDisplay, config, UnityEglConfig.unityContext,
            attrib3List, 0)
        if (EGL14.eglGetError() == EGL14.EGL_SUCCESS) {
            eglConfig = config
            eglContext = context
        }

        // Confirm with query.
        val values = IntArray(1)
        EGL14.eglQueryContext(eglDisplay, eglContext, EGL14.EGL_CONTEXT_CLIENT_VERSION,
            values, 0)
        Log.d(TAG, "EGLContext created, client version " + values[0])
        val surfaceAttribs = intArrayOf(
            EGL14.EGL_WIDTH, width,
            EGL14.EGL_HEIGHT, height,
            EGL14.EGL_NONE
        )
        val eglSurface = EGL14.eglCreatePbufferSurface(eglDisplay, eglConfig,
            surfaceAttribs, 0)
        checkEglError("eglCreatePbufferSurface")
        if (eglSurface == null) {
            throw java.lang.RuntimeException("surface was null")
        }
        this.eglSurface = eglSurface
        Log.i(TAG, "Finished EGL Setup")
    }

    private fun makeCurrent() {
        if (eglSurface == EGL14.EGL_NO_SURFACE) {
            Log.w(TAG, "No surface!")
            return
        }
        if (eglDisplay === EGL14.EGL_NO_DISPLAY) {
            // called makeCurrent() before create?
            Log.d(TAG, "NOTE: makeCurrent w/o display")
        }
        if (!EGL14.eglMakeCurrent(eglDisplay, eglSurface, eglSurface, eglContext)) {
            throw java.lang.RuntimeException("eglMakeCurrent failed")
        }
    }

    override fun onFrameAvailable(surfaceTexture: SurfaceTexture) {
        makeCurrent()
        surfaceTexture.updateTexImage()
        checkGlError("updateTexImage")
        surfaceTexture.getTransformMatrix(transformationMatrix)
        fboRenderer?.renderCameraTexture(cameraTexture, transformationMatrix)
    }

    fun release() {
        surface.release()
        GLES20.glDeleteTextures(1, intArrayOf(cameraTexture), 0)
        GLES20.glDeleteTextures(1, intArrayOf(outputTexture), 0)
        fboRenderer?.also {
            // invalidate before calling destroy because of onFrameAvailable
            fboRenderer = null
            it.destroy()
        }
    }

    protected fun finalize() {
        release()
    }

    companion object {
        private val TAG = UnityCameraRenderer::class.java.simpleName
        private const val EGL_RECORDABLE_ANDROID: Int = 0x3142

        fun createCameraTexture() = createTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES)

        fun createOutputTexture() = createTexture(GLES20.GL_TEXTURE_2D)

        private fun createTexture(textureType: Int): Int {
            val texture = IntArray(1)
            GLES20.glGenTextures(1, texture, 0)
            checkGlError("glGenTextures")
            GLES20.glBindTexture(textureType, texture[0])
            checkGlError("glBindTexture")
            GLES20.glTexParameterf(textureType,
                GL10.GL_TEXTURE_MIN_FILTER, GL10.GL_LINEAR.toFloat())
            GLES20.glTexParameterf(textureType,
                GL10.GL_TEXTURE_MAG_FILTER, GL10.GL_LINEAR.toFloat())
            GLES20.glTexParameteri(textureType,
                GL10.GL_TEXTURE_WRAP_S, GL10.GL_CLAMP_TO_EDGE)
            GLES20.glTexParameteri(textureType,
                GL10.GL_TEXTURE_WRAP_T, GL10.GL_CLAMP_TO_EDGE)
            checkGlError("glTexParameter")
            return texture[0]
        }

        private fun checkGlError(op: String) {
            var error = GLES20.glGetError()
            var lastError: Int
            if (error != GLES20.GL_NO_ERROR) {
                do {
                    lastError = error
                    Log.e(COACH_TAG, op + ":glError " + GLU.gluErrorString(lastError))
                    error = GLES20.glGetError()
                } while (error != GLES20.GL_NO_ERROR)
            }
        }

        private fun checkEglError(msg: String) {
            var error: Int
            if (EGL14.eglGetError().also { error = it } != EGL14.EGL_SUCCESS) {
                throw java.lang.RuntimeException(msg + ": EGL error: 0x" + Integer.toHexString(error))
            }
        }
    }
}
