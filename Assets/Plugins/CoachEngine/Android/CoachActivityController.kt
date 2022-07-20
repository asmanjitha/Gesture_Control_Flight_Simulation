/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import android.util.Log
import com.coachai.engine.activity.ActivityCommand
import com.coachai.engine.activity.ActivityService
import com.coachai.engine.event.PublicEvent
import com.coachai.engine.unity.serialization.load
import kotlin.reflect.KClass

@Suppress("UNUSED")
object CoachActivityController {
    private var activity: CoachActivity? = null

    fun startActivity(activityConfig: String, callbackHandler: IActivityCallbacks) {
        check(ActivityService.isFree) { "Only a single activity can be active at a time." }

        val config = ActivityServiceConfig.fromJson(activityConfig)
        Log.i(COACH_TAG, "Starting activity: ${config.identifier}")
        activity = CoachActivity.initialize(config, callbackHandler)
    }

    fun stopActivity() {
        activity?.let {
            activity = null
            it.stop()
        }
    }

    fun subscribePublicEvents(events: Array<out String>) {
        events.forEach { event ->
            Log.i(COACH_TAG, "subscribe $event")
            try {
                @Suppress("UNCHECKED_CAST")
                val eventClass = Class.forName(event).kotlin as KClass<PublicEvent>
                activity?.subscribePublicEvent(eventClass)
            } catch (e: ClassNotFoundException) {
                Log.w(COACH_TAG, "$event not found")
            } catch (e: ClassCastException) {
                Log.w(COACH_TAG, "$event is not a PublicEventSpec")
            }
        }
    }

    fun unsubscribePublicEvents(events: Array<out String>) {
        events.forEach { event ->
            try {
                @Suppress("UNCHECKED_CAST")
                val eventClass = Class.forName(event).kotlin as? KClass<PublicEvent> ?: return@forEach
                activity?.unsubscribePublicEvent(eventClass)
            } catch (e: ClassNotFoundException) {
                Log.w(COACH_TAG, "class $event not found")
            }
        }
    }

    fun subscribeActivityMetric(metricIds: Array<out String>) {
        metricIds.forEach { activity?.subscribeActivityMetric(it) }
    }

    fun pollEvents() : Array<String> {
        return activity?.popEvents() ?: emptyArray()
    }

    fun subscribeWorldObjectEvents(eventType: Int) {
        activity?.subscribeWorldObjectEvents(eventType)
    }

    fun unsubscribeWorldObjectEvents(eventType: Int) {
        activity?.unsubscribeWorldObjectEvents(eventType)
    }

    fun isActivityInitialized() : Boolean {
        return activity?.isActivityInitialized() == true
    }

    fun takeDecision(decisionRequestId: Int, decision: String) {
        activity?.takeDecision(decisionRequestId, decision)
    }

    fun fulfillRequirement(requirementId: Int, requirement: String) {
        activity?.fulfillRequirement(requirementId, requirement)
    }

    fun sendCommand(type: String, command: String) {
        activity?.run {
            load<ActivityCommand>(type, command)?.let { this.sendCommand(it) }
        }
    }
}
