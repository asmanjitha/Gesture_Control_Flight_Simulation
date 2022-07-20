/*
 * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity.serialization

import com.coachai.engine.activity.ActivityMetrics
import com.coachai.engine.serialization.RecordableRegistries
import com.coachai.engine.time.monotonicTimeFraction
import kotlinx.serialization.KSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.json.Json

private val jsonCodec = Json {
    ignoreUnknownKeys = true
}

fun <T: Any> load(type: String, json: String) : T? {
    return RecordableRegistries.all
        .mapNotNull {
            @Suppress("UNCHECKED_CAST")
            it.serializerFor(type) as? KSerializer<T>
        }
        .firstOrNull()
        ?.let { jsonCodec.decodeFromString(it, json) }
}

fun Map<String, Any>.toMetricsJson() = org.json.JSONStringer().run {
        `object`()
        key("type")
        value(ActivityMetrics::class.qualifiedName)
        key("data")
        value(org.json.JSONObject(this@toMetricsJson))
        key("timestamp")
        value(monotonicTimeFraction())
        endObject()
        toString()
}

fun errorJson(message: String, errorCode: Int = 10_000) : String {
    val encodedMsg = jsonCodec.encodeToJsonElement(String.serializer(), message)
    return """{"code":$errorCode}, "message":$encodedMsg}"""
}
