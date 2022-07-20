/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import android.util.Log
import com.coachai.engine.ar.components.ARPublisher
import com.coachai.engine.ar.components.AugmentedRealitySourceParameter
import com.coachai.engine.camera.components.CameraPublisher
import com.coachai.engine.camera.components.VideoSourceParameter
import com.coachai.engine.concurrent.SubscriptionId
import com.coachai.engine.concurrent.dispatchqueue.DispatchQueue
import com.coachai.engine.recording.misc.serializeToJson
import com.coachai.engine.runtime.parameter.getIdentifier

@Suppress("UNUSED")
internal class ComponentRuntime(private val callbackHandler: IComponentRuntimeCallbacks) {
    private val queue = DispatchQueue.mainThreadQueue("UnityComponentRuntime")
    private val runtime = com.coachai.engine.components.ComponentRuntime()

    /**
     * Load the given component.
     */
    fun load(component: String, config: String) {
        Log.d(COACH_TAG, "Loading component $component")
        val parameters = parseParameterString(config).toMutableMap()

        when(component) {
            ARPublisher.identifier -> {
                parameters[AugmentedRealitySourceParameter.getIdentifier()] = CoachSdk.unitySession
            }
            CameraPublisher.identifier -> {
                parameters[VideoSourceParameter.getIdentifier()] = CoachSdk.unitySession
            }
        }

        queue.async {
            runtime.load(component, parameters) {
                Log.d(COACH_TAG, "Component $component loaded")
                callbackHandler.OnLoaded(component, true)
            }
        }
    }

    /**
     * Configure a loaded component.
     */
    fun reconfigure(component: String, config: String) {
        Log.d(COACH_TAG, "Configuring component $component")
        val parameters = parseParameterString(config)
        queue.async {
            runtime.reconfigure(component, parameters) {
                Log.d(COACH_TAG, "Component $component configured")
                callbackHandler.OnConfigured(component, true)
            }
        }
    }

    /**
     * Resume execution of loaded components.
     */
    fun start() = queue.sync {
        Log.d(COACH_TAG, "Resuming component runtime")
        runtime.start()
    }

    /**
     * Pause execution of loaded components.
     */
    fun pause() = queue.sync {
        Log.d(COACH_TAG, "Pausing component runtime")
        runtime.pause()
    }

    /**
     * Stop execution of all loaded components and dispose of any resources held by this runtime.
     */
    fun dispose() = queue.sync {
        Log.d(COACH_TAG, "Disposing component runtime")
        runtime.dispose()
    }

    /**
     * Subscribe to the given event.
     */
    fun subscribe(eventIdentifier: String) = runtime.subscribe(eventIdentifier) {
        val json = it.serializeToJson()
        callbackHandler.OnEvent(json)
    }

    /**
     * Cancel the subscription with the given id.
     */
    fun unsubscribe(subscriptionId: SubscriptionId) = runtime.unsubscribe(subscriptionId)

    private fun parseParameterString(json: String): Map<String, Any> = try {
        val jsonObject = org.json.JSONObject(json)
        jsonObject.keys().asSequence().associateWith { jsonObject[it] }
    } catch (_: org.json.JSONException) {
        Log.e(COACH_TAG, "Failed to parse json parameter string: $json.")
        emptyMap()
    }

    @Suppress("FunctionName")
    interface IComponentRuntimeCallbacks {
        fun OnLoaded(componentId: String, result: Boolean)
        fun OnConfigured(componentId: String, result: Boolean)
        fun OnEvent(event: String)
    }
}
