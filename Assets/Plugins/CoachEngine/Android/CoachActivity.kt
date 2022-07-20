/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import android.os.Handler
import android.os.Looper
import android.util.Log
import com.coachai.engine.activity.ActivityCommand
import com.coachai.engine.activity.ActivityDecision
import com.coachai.engine.activity.ActivityDecisionRequest
import com.coachai.engine.activity.ActivityDelegate
import com.coachai.engine.activity.ActivityError
import com.coachai.engine.activity.ActivityHandler
import com.coachai.engine.activity.ActivityMetrics
import com.coachai.engine.activity.ActivityParameterSet
import com.coachai.engine.activity.ActivityRequirement
import com.coachai.engine.activity.MetricSpec
import com.coachai.engine.activity.event.WorldObjectEvent
import com.coachai.engine.activity.getIdentifier
import com.coachai.engine.ar.requirement.ARSessionRequirement
import com.coachai.engine.camera.requirement.CameraSessionRequirement
import com.coachai.engine.event.PublicEvent
import com.coachai.engine.recording.internal.EventType
import com.coachai.engine.recording.misc.serializeToJson
import com.coachai.engine.serialization.Recordable
import com.coachai.engine.unity.serialization.errorJson
import com.coachai.engine.unity.serialization.load
import com.coachai.engine.unity.serialization.toMetricsJson
import java.util.concurrent.atomic.AtomicReference
import kotlin.random.Random
import kotlin.reflect.KClass

internal class CoachActivity private constructor(
    private val callbackHandler: IActivityCallbacks
) : ActivityDelegate<ActivityParameterSet, ActivityMetrics>, Handler(Looper.getMainLooper()) {
    private var sdkActivityHandler: ActivityHandler<*, *>? = null
    private val publicEventSubscriptions = mutableMapOf<KClass<out PublicEvent>, Long>()
    private var subscribedWorldObjectEvents = 0x00
    private val pendingCallbacks = mutableMapOf<Int, JsonCallback>()

    /**
     *  map only stores latest event for a type and prevents buffer from growing indefinitely
     *  when no polling is happening
     */
    private var eventBuffer = AtomicReference(emptyMap<String, String>())
    private var metricBuffer = AtomicReference(emptyMap<String, Any>())
    private val earlySubscriptions = mutableSetOf<KClass<PublicEvent>>()

    companion object {
        fun initialize(
            config: ActivityServiceConfig,
            callbackHandler: IActivityCallbacks
        ) : CoachActivity = CoachActivity(callbackHandler).also { activity ->
                activity.post {
                try {
                    with(config) {
                        activityVariant?.let {
                            activitySpec.instantiate(activityParameterSet, it, activity)
                        } ?: activitySpec.instantiate(activityParameterSet, activity)
                    }
                } catch (ex: Exception) {
                    Log.e(COACH_TAG, "crashed while instantiating spec", ex)
                }
            }
        }
    }

    override fun <D : ActivityDecision> onDecisionRequest(decisionRequest: ActivityDecisionRequest<D>) {
        val isRequestRecordable = decisionRequest is Recordable<*>
        val isDecisionRecordable = decisionRequest.decisionKotlinClass::class.isInstance(Recordable::class)

        if (!isRequestRecordable || !isDecisionRecordable) {
            Log.e(COACH_TAG,"Can't handle decision request. Make sure ${decisionRequest::class} " +
                    "and ${decisionRequest.decisionKotlinClass} both implement ${Recordable::class}. " +
                    "Falling back to default.")
            decisionRequest.chooseDefault()
            return
        }

        requestDecision(decisionRequest)
    }

    fun takeDecision(decisionId: Int, decision: String) {
        val callback = pendingCallbacks.remove(decisionId)
        if (callback == null) {
            Log.w(COACH_TAG, "Ignoring unknown decision id=$decisionId")
            return
        }
        callback.invoke(decision)
    }

    override fun onError(error: ActivityError) {
        val json = errorJson(error.message)
        callbackHandler.OnError(json)
        Log.d(COACH_TAG,"onError $error")
    }

    override fun onFinish() {
        Log.d(COACH_TAG,"onFinish")
        callbackHandler.OnFinish(FinishFlags.FINISH.value)
    }

    override fun onInit() {
        earlySubscriptions.forEach(::subscribePublicEventInternal)
        earlySubscriptions.clear()
        callbackHandler.OnInit()
    }

    override fun onObjectAdded(objEvent: WorldObjectEvent) {
        if (subscribedWorldObjectEvents.and(0x01) == 0x01) {
            dumpEvent(objEvent, EventType.ADDED)
        }
    }

    override fun onObjectUpdated(objEvent: WorldObjectEvent) {
        if (subscribedWorldObjectEvents.and(0x02) == 0x02) {
            dumpEvent(objEvent, EventType.UPDATED)
        }
    }

    override fun onObjectRemoved(objEvent: WorldObjectEvent) {
        if (subscribedWorldObjectEvents.and(0x04) == 0x04) {
            dumpEvent(objEvent, EventType.REMOVED)
        }
    }

    override fun onReady() {
        Log.d(COACH_TAG,"onReady")
        sdkActivityHandler?.start()
    }

    override fun setHandler(handler: ActivityHandler<ActivityParameterSet, ActivityMetrics>?) {
        Log.d(COACH_TAG,"setHandler")
        sdkActivityHandler = handler
    }

    override fun <T : Any> onRequire(requirement: ActivityRequirement<T>) {
        when (requirement) {
            is ARSessionRequirement -> requirement.fulfill(CoachSdk.unitySession)
            is CameraSessionRequirement -> requirement.fulfill(CoachSdk.unitySession)
            else -> {
                requirement.defaultFulfillment?.let { requirement.fulfillPreliminary(it) }
                require(requirement)
            }
        }
    }

    fun fulfillRequirement(requirementId: Int, json: String) {
        val callback = pendingCallbacks.remove(requirementId)
        if (callback == null) {
            Log.w(COACH_TAG, "Ignoring unknown requirement fulfillment id=$requirementId")
            return
        }
        callback.invoke(json)
    }

    fun finish() = post {
        sdkActivityHandler?.finish()
    }

    fun pause() = post {
        sdkActivityHandler?.pause()
    }

    fun stop() = post {
        sdkActivityHandler?.abort()
        callbackHandler.OnFinish(FinishFlags.ABORT.value)
    }

    fun sendCommand(command: ActivityCommand) {
        Log.d(COACH_TAG, "Received command $command")
        sdkActivityHandler?.sendCommand(command)
    }

    fun subscribePublicEvent(event: KClass<PublicEvent>) {
        if (sdkActivityHandler == null) {
            earlySubscriptions.add(event)
            Log.w(COACH_TAG, "Activity not ready yet, will attempt to subscribe to ${event.qualifiedName} again when ready!")
            return
        }

        subscribePublicEventInternal(event)
    }

    private fun subscribePublicEventInternal(event: KClass<PublicEvent>) {
        if (publicEventSubscriptions[event] != null) return

        if (!event::class.isInstance(Recordable::class)) {
            Log.w(COACH_TAG, "Not subscribing to ${event.qualifiedName}. " +
                "It does not implement required interface ${Recordable::class.qualifiedName}")
            return
        }

        sdkActivityHandler
            ?.availableEvents()
            ?.firstOrNull { it.type == event }
            ?.let { sdkActivityHandler?.subscribeTo(event = it, ::dumpEvent) }
            ?.also {
                Log.d(COACH_TAG, "Added subscription for: ${event.qualifiedName}")
                publicEventSubscriptions[event] = it
                return
            }

        sdkActivityHandler
            ?.availableFeedback()
            ?.firstOrNull { it.type == event }
            ?.let { sdkActivityHandler?.subscribeTo(feedback = it, ::dumpEvent) }
            ?.also {
                Log.d(COACH_TAG, "Added subscription for: ${event.qualifiedName}")
                publicEventSubscriptions[event] = it
                return
            }

        Log.e(COACH_TAG, "${event.qualifiedName} is not a valid event or " +
            "feedback for ${sdkActivityHandler?.activityIdentifier}")
    }

    fun unsubscribePublicEvent(event: KClass<PublicEvent>) {
        publicEventSubscriptions.remove(event)?.also {
            sdkActivityHandler?.unsubscribe(it)
            Log.d(COACH_TAG, "Removed subscription for: ${event.qualifiedName}")
        }
    }

    fun subscribeWorldObjectEvents(eventType: Int) {
        subscribedWorldObjectEvents = subscribedWorldObjectEvents.or(eventType)
    }

    fun unsubscribeWorldObjectEvents(eventType: Int) {
        subscribedWorldObjectEvents = subscribedWorldObjectEvents.and(eventType.inv())
    }

    fun subscribeActivityMetric(metricId: String) {
        val activityMetrics = sdkActivityHandler?.getMetrics()
        activityMetrics?.metrics
            ?.firstOrNull { it.getIdentifier() == metricId }
            ?.let { metricSpec: MetricSpec<*> ->
                Handler(Looper.getMainLooper()).post {
                    activityMetrics.subscribeTo(metricSpec) { value ->
                        metricBuffer.getAndUpdate {
                            it.plus(metricId to value)
                        }
                    }
                    Log.d(COACH_TAG, "Subscribed to metric $metricId")
                }
            }
    }

    fun popEvents() : Array<String> {
        val events = eventBuffer.getAndSet(mutableMapOf()).values
        val metrics = metricBuffer.getAndSet(mutableMapOf())

        return if (metrics.isEmpty()) {
            events
        } else {
            val jsonMetrics = metrics.toMetricsJson()
            events.plus(jsonMetrics)
        }.toTypedArray()
    }

    fun isActivityInitialized() = sdkActivityHandler != null

    private fun dumpEvent(event: PublicEvent) {
        // trade-off: if poll frequency is low we might serialize a lot unnecessarily here
        // but if we delay we will increase polling time
        if (event is Recordable<*>) {
            val json = event.serializeToJson()
            eventBuffer.getAndUpdate {
                it.plus(event::class.qualifiedName!! to json)
            }
        }
    }

    private fun dumpEvent(event: WorldObjectEvent, eventType: EventType) {
        // trade-off: if poll frequency is low we might serialize a lot unnecessarily here
        // but if we delay we will increase polling time
        val json = event.serializeToJson(eventType)
        eventBuffer.getAndUpdate {
            it.plus(event.worldObject.id to json)
        }
    }

    private fun <D : ActivityDecision> requestDecision(request: ActivityDecisionRequest<D>) {
        val requestJson = request.serializeToJson()
        val requestId: Int = Random.nextInt()
        val callback = callback@{ json: String? ->
            Log.d(COACH_TAG, "decision $requestId callback")
            if (json.isNullOrEmpty()) {
                request.chooseDefault()
                return@callback
            }
            val type = requireNotNull(request.decisionKotlinClass.qualifiedName)
            val decision = load<D>(type, json)
            if (decision == null) {
                request.chooseDefault()
                val error = errorJson("failed to deserialize decision for request $requestId")
                callbackHandler.OnError(error)
            } else {
                request.takeDecision(decision)
            }
        }
        pendingCallbacks[requestId] = callback
        callbackHandler.OnDecisionRequest(requestId, requestJson)
    }

    private fun <T : Any> require(requirement: ActivityRequirement<T>) {
        val requestId: Int = Random.nextInt()
        val fulfillmentClassName = requireNotNull(requirement.kotlinClass.qualifiedName)
        val callback = callback@{ json: String? ->
            Log.d(COACH_TAG, "requirement $requestId callback")
            if (json.isNullOrEmpty()) {
                requirement.fulfill(requirement.defaultFulfillment!!)
                return@callback
            }

            load<T>(fulfillmentClassName, json)?.let { requirement.fulfill(it) }
        }
        pendingCallbacks[requestId] = callback
        callbackHandler.OnRequire(requestId, fulfillmentClassName)
    }
}
