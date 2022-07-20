/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

package com.coachai.engine.unity

import android.util.Log
import com.coachai.engine.AppMode
import com.coachai.engine.SDK
import com.coachai.engine.logging.LogLevel
import com.unity3d.player.UnityPlayer

object CoachSdk {
    val unitySession = UnitySession()

    @Suppress("UNUSED")
    fun startSdk(logLevel: String, appMode: String) {
        try {
            SDK.appMode = AppMode.valueOf(appMode)
        } catch (_: IllegalArgumentException) {
            Log.w(COACH_TAG, "Ignoring attempt to set invalid app mode $logLevel")
        }

        try {
            SDK.logLevel = LogLevel.valueOf(logLevel)
        } catch (_: IllegalArgumentException) {
            Log.w(COACH_TAG, "Ignoring attempt to set invalid log level $logLevel")
        }

        SDK.initialize(UnityPlayer.currentActivity.application)

        Log.i(COACH_TAG, "CoachAI Sdk v${SDK.version} initialized")
    }
}
