package com.coachai.engine.unity

/**
 * This interface is implemented in C# on Unity side.
 */
@Suppress("FunctionName")
interface IActivityCallbacks {
    fun OnDecisionRequest(requestId: Int, json: String)
    fun OnFinish(flags: Int)
    fun OnInit()
    fun OnError(json: String)
    fun OnRequire(requirementId: Int, json: String)
}

typealias JsonCallback = (String?) -> Unit

enum class FinishFlags(val value: Int) {
    FINISH(0x00),
    ABORT(0x01)
}
