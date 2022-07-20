/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.camera

import android.opengl.EGL14
import android.opengl.EGLContext
import android.opengl.EGLDisplay
import android.opengl.EGLSurface
import android.util.Log
import com.coachai.engine.unity.COACH_TAG
import kotlin.Throws
import java.lang.IllegalStateException

object UnityEglConfig {
    var unityContext: EGLContext = EGL14.EGL_NO_CONTEXT
    var unityDisplay: EGLDisplay = EGL14.EGL_NO_DISPLAY
    var unityDrawSurface: EGLSurface = EGL14.EGL_NO_SURFACE
    var unityReadSurface: EGLSurface = EGL14.EGL_NO_SURFACE
    @Throws(IllegalStateException::class)
    fun initialize(): Boolean {
        Log.d(COACH_TAG, "setting up unity context : BEGIN")
        Log.d(COACH_TAG, "current thread = " + Thread.currentThread().name)
        unityContext = EGL14.eglGetCurrentContext()
        unityDisplay = EGL14.eglGetCurrentDisplay()
        unityDrawSurface = EGL14.eglGetCurrentSurface(EGL14.EGL_DRAW)
        unityReadSurface = EGL14.eglGetCurrentSurface(EGL14.EGL_READ)
        checkInitialized()
        Log.d(COACH_TAG, "setting unity context: SUCCESS")
        return true
    }

//    @Throws(IllegalStateException::class)
//    fun makeCurrent(): Boolean {
//        checkInitialized()
//        return EGL14.eglMakeCurrent(unityDisplay, unityDrawSurface, unityReadSurface, unityContext)
//    }

    private fun checkInitialized() {
        check(unityContext != EGL14.EGL_NO_CONTEXT) { "unityContext == EGL_NO_CONTEXT" }
        check(unityDisplay != EGL14.EGL_NO_DISPLAY) { "unityDisplay == EGL_NO_DISPLAY" }
        check(unityDrawSurface != EGL14.EGL_NO_SURFACE) { "unityContext == EGL_NO_CONTEXT" }
        check(unityReadSurface != EGL14.EGL_NO_SURFACE) { "unityReadSurface == EGL_NO_SURFACE" }
    }
}
