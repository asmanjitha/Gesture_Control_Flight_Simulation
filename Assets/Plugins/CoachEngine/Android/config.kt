/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import com.coachai.engine.activity.ActivityMetrics
import com.coachai.engine.activity.ActivityParameterSet
import com.coachai.engine.activity.ActivitySpec
import com.coachai.engine.activity.ActivityVariant

data class UnityArPlaneData(val extentXYZ: FloatArray,
                            val centerPosePosition: FloatArray,
                            val centerPoseRotation: FloatArray,
                            val polygonArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as UnityArPlaneData

        if (!extentXYZ.contentEquals(other.extentXYZ)) return false
        if (!centerPosePosition.contentEquals(other.centerPosePosition)) return false
        if (!centerPoseRotation.contentEquals(other.centerPoseRotation)) return false
        if (!polygonArray.contentEquals(other.polygonArray)) return false

        return true
    }

    override fun hashCode(): Int {
        var result = extentXYZ.contentHashCode()
        result = 31 * result + centerPosePosition.contentHashCode()
        result = 31 * result + centerPoseRotation.contentHashCode()
        result = 31 * result + polygonArray.contentHashCode()
        return result
    }
}

data class UnityCameraConfig(val focalLength: FloatArray,
                             val principalPoint: FloatArray,
                             val imageDimensions: IntArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as UnityCameraConfig

        if (!focalLength.contentEquals(other.focalLength)) return false
        if (!principalPoint.contentEquals(other.principalPoint)) return false
        if (!imageDimensions.contentEquals(other.imageDimensions)) return false

        return true
    }

    override fun hashCode(): Int {
        var result = focalLength.contentHashCode()
        result = 31 * result + principalPoint.contentHashCode()
        result = 31 * result + imageDimensions.contentHashCode()
        return result
    }
}

data class ActivityServiceConfig(
    val identifier: String,
    val parameters: Map<String, Any>,
    val variant: String?
) {
    companion object {
        fun fromJson(json: String) : ActivityServiceConfig {
            val jsonObject = org.json.JSONObject(json)
            val activitySpecName = jsonObject.getString("Identifier")
            val variantName = jsonObject.optString("Variant")
            val parameters = jsonObject.optJSONObject("Parameters")
                ?.let { params ->
                    params.keys().asSequence().associateWith { params[it] }
                } ?: emptyMap()
            return ActivityServiceConfig(
                identifier = activitySpecName,
                variant = variantName,
                parameters = parameters
            )
        }
    }

    val activitySpec: ActivitySpec<*, ActivityParameterSet, ActivityMetrics, *, *>
        by lazy {
            @Suppress("UNCHECKED_CAST")
            requireNotNull(
                Class.forName(identifier).kotlin.objectInstance as? ActivitySpec<*, ActivityParameterSet, ActivityMetrics, *, *>
            ) { "$identifier is not a valid activity spec class name!" }
        }

    val activityVariant: ActivityVariant<*, ActivityParameterSet>?
        by lazy {
            if (variant.isNullOrBlank()) null else {
                @Suppress("UNCHECKED_CAST")
                Class.forName(variant).kotlin.objectInstance as? ActivityVariant<*, ActivityParameterSet>
            }
        }

    val activityParameterSet: ActivityParameterSet
        by lazy {
            activitySpec.availableParameters().apply {
                setValues(parameters)
            }
        }
}
