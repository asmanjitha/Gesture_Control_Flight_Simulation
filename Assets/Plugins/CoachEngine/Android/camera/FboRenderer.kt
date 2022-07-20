/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.camera

import android.opengl.*
import android.util.Log
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.IntBuffer

/**
 * A simple OpenGL renderer, to render a camera texture into a 2d texture using an
 * intermediate FBO.
 */
class FboRenderer(private val cameraWidth: Int, private val cameraHeight: Int, private val textureId: Int) {
    private val imageFormat = GLES30.GL_RGBA
    private val bufferCount = 1
    private val frameBuffer = IntArray(bufferCount)
    private val vao = IntArray(bufferCount)
    private val vbo = IntArray(bufferCount)

    private var texMatrixLoc = 0
    private var quadProgram = 0

    init {
        create()
    }

    private fun checkGlError(op: String) {
        var error = GLES20.glGetError()
        var lastError: Int
        if (error != GLES20.GL_NO_ERROR) {
            do {
                lastError = error
                Log.e(TAG, op + ":glError " + GLU.gluErrorString(lastError))
                error = GLES20.glGetError()
            } while (error != GLES20.GL_NO_ERROR)
        }
    }

    //    Create an fbo, attach it to the output 2d texture, create a program to draw the camera texture
    private fun create() {
        GLES20.glGenFramebuffers(bufferCount, frameBuffer, 0)
        GLES30.glGenBuffers(1, vbo, 0)
        GLES30.glGenVertexArrays(1, vao, 0)
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, frameBuffer[0])
        GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, textureId)
        GLES30.glTexImage2D(
            GLES30.GL_TEXTURE_2D,
            0,
            imageFormat,
            this.cameraWidth,
            this.cameraHeight,
            0,
            imageFormat,
            GLES30.GL_UNSIGNED_BYTE,
            null)
        checkGlError("glTexImage2D")
        GLES20.glTexParameteri(
            GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE)
        GLES20.glTexParameteri(
            GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE)
        GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR)
        GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR)
        GLES20.glFramebufferTexture2D(
            GLES20.GL_FRAMEBUFFER, GLES20.GL_COLOR_ATTACHMENT0, GLES20.GL_TEXTURE_2D, textureId, 0)
        val status = GLES20.glCheckFramebufferStatus(GLES20.GL_FRAMEBUFFER)
        if (status != GLES20.GL_FRAMEBUFFER_COMPLETE) {
            throw RuntimeException(
                this
                    .toString() + ": Failed to set up render buffer with status "
                    + status
                    + " and error "
                    + GLES20.glGetError())
        }
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, 0)
        checkGlError("glBindFramebuffer")

        // Load shader program.
        val vertexShader = loadGLShader(GPU_DOWNLOAD_VERT, GLES20.GL_VERTEX_SHADER)
        val fragmentShader = loadGLShader(GPU_DOWNLOAD_FRAG, GLES20.GL_FRAGMENT_SHADER)

        quadProgram = GLES20.glCreateProgram()
        GLES20.glAttachShader(quadProgram, vertexShader)
        GLES20.glAttachShader(quadProgram, fragmentShader)
        GLES20.glLinkProgram(quadProgram)
        GLES20.glUseProgram(quadProgram)
        checkGlError("glUseProgram")

        val quadPositionAttrib = GLES20.glGetAttribLocation(quadProgram, "a_Position")
        val quadTexCoordAttrib = GLES20.glGetAttribLocation(quadProgram, "a_TexCoord")
        val texLoc = GLES20.glGetUniformLocation(quadProgram, "sTexture")
        texMatrixLoc = GLES20.glGetUniformLocation(quadProgram, "uTexMatrix")
        GLES20.glUniform1i(texLoc, 0)

        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, vbo[0])
        checkGlError("glBindBuffer GL_ARRAY_BUFFER")

        val bbVerticesTextCoords = ByteBuffer.allocateDirect((QUAD_COORDS.size + QUAD_TEXCOORDS.size) * FLOAT_SIZE)
        bbVerticesTextCoords.order(ByteOrder.nativeOrder())
        val buffer = bbVerticesTextCoords.asFloatBuffer()
        buffer.put(QUAD_COORDS)
        buffer.put(QUAD_TEXCOORDS)
        buffer.position(0)
        GLES20.glBufferData(GLES20.GL_ARRAY_BUFFER, (QUAD_COORDS.size + QUAD_TEXCOORDS.size) * FLOAT_SIZE, bbVerticesTextCoords, GLES20.GL_STATIC_DRAW)
        checkGlError("glBufferData")

        GLES30.glBindVertexArray(vao[0])
        GLES20.glEnableVertexAttribArray(quadPositionAttrib)
        GLES20.glVertexAttribPointer(quadPositionAttrib, COORDS_PER_VERTEX, GLES20.GL_FLOAT, false, 0, 0)
        GLES20.glEnableVertexAttribArray(quadTexCoordAttrib)
        GLES20.glVertexAttribPointer(quadTexCoordAttrib, TEXCOORDS_PER_VERTEX, GLES20.GL_FLOAT, false, 0, QUAD_COORDS.size * FLOAT_SIZE)
        GLES30.glBindVertexArray(0)
        checkGlError("glBindVertexArray")

        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0)
        checkGlError("glBindBuffer")
    }

    private fun loadGLShader(code: String, type: Int): Int {
        // Compiles shader code.
        var shader = GLES20.glCreateShader(type)
        GLES20.glShaderSource(shader, code)
        GLES20.glCompileShader(shader)

        // Get the compilation status.
        val compileStatus = IntArray(1)
        GLES20.glGetShaderiv(shader, GLES20.GL_COMPILE_STATUS, compileStatus, 0)

        // If the compilation failed, delete the shader.
        if (compileStatus[0] == 0) {
            Log.e(TAG, "Error compiling shader: " + GLES20.glGetShaderInfoLog(shader));
            GLES20.glDeleteShader(shader)
            shader = 0
        }
        if (shader == 0) {
            throw java.lang.RuntimeException("Error creating shader.")
        }
        return shader
    }

    private fun drawTexture(textureId: Int, texMatrix: FloatArray) {
        // Clear buffers.
        GLES20.glClearColor(0f, 0f, 0f, 0f)
        GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT or GLES20.GL_DEPTH_BUFFER_BIT)
        checkGlError("glClear")

        // Use red-blue switch shader
        GLES20.glUseProgram(quadProgram)
        checkGlError("glUseProgram")

        GLES20.glUniformMatrix4fv(texMatrixLoc, 1, false, texMatrix, 0)
        checkGlError("glUniformMatrix4fv")

        // Select input texture.
        GLES20.glActiveTexture(GLES20.GL_TEXTURE0)
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, textureId);
        GLES30.glBindVertexArray(vao[0])
        checkGlError("glBindVertexArray")

        // Draw a quad with texture.
        GLES20.glDrawArrays(GLES20.GL_TRIANGLE_STRIP, 0, 4)
        checkGlError("glDrawArrays")

        // Reset bindings.
        GLES30.glBindVertexArray(0)
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, 0);
        checkGlError("glBindTexture")
    }

    //    Draw camera texture into created fbo (connected to output texture)
    fun renderCameraTexture(textureId: Int, texMatrix: FloatArray) {
        // Bind both read and write to framebuffer.
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, frameBuffer[0])
        checkGlError("glBindFramebuffer")

        // Save and setup viewport
        val viewport = IntBuffer.allocate(4)
        GLES20.glGetIntegerv(GLES20.GL_VIEWPORT, viewport)

        GLES20.glViewport(0, 0, cameraWidth, cameraHeight)
        checkGlError("glViewport")

        // Draw texture to framebuffer.
        drawTexture(textureId, texMatrix)

        // Restore viewport.
        GLES20.glViewport(viewport[0], viewport[1], viewport[2], viewport[3])
        GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, 0)
        checkGlError("glBindFramebuffer")
    }

    fun destroy() {
        //FIXME deleting buffer will hang rendering
        // GLES20.glDeleteFramebuffers(bufferCount, frameBuffer, 0)
        GLES20.glDeleteProgram(quadProgram)
        GLES30.glDeleteBuffers(1, vbo, 0)
        GLES30.glDeleteVertexArrays(1, vao, 0)
    }

    protected fun finalize() {
        destroy()
    }

    companion object {
        private const val COORDS_PER_VERTEX = 3
        private const val TEXCOORDS_PER_VERTEX = 2
        private const val FLOAT_SIZE = 4

        private val QUAD_COORDS = floatArrayOf(
            -1.0f, -1.0f, 0.0f,
            -1.0f, +1.0f, 0.0f,
            +1.0f, -1.0f, 0.0f,
            +1.0f, +1.0f, 0.0f,
        )

        private val QUAD_TEXCOORDS = floatArrayOf(
            0.0f, 0.0f,
            0.0f, 1.0f,
            1.0f, 0.0f,
            1.0f, 1.0f,
        )
        private const val TAG = "FboRenderer"

        private const val GPU_DOWNLOAD_VERT = """
attribute vec4 a_Position;
attribute vec4 a_TexCoord;
varying vec2 v_TexCoord;
uniform mat4 uTexMatrix;
void main() {
    gl_Position = a_Position;
    v_TexCoord = (uTexMatrix * a_TexCoord).xy;
}"""

        private const val GPU_DOWNLOAD_FRAG = """
#extension GL_OES_EGL_image_external : require
precision mediump float;
varying vec2 v_TexCoord;
uniform samplerExternalOES sTexture;
void main() {
    gl_FragColor = texture2D(sTexture, v_TexCoord);
}"""
    }
}
