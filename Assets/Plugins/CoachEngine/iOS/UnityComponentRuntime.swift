/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

import Foundation
import CoachAIKit
import ARKit
import AVFoundation
import Dispatch

public typealias OnComponentLoadedCallback = @convention(c) (Int32, UnsafePointer<CChar>, Bool) -> Void
public typealias OnComponentConfgiuredCallback = @convention(c) (Int32, UnsafePointer<CChar>, Bool) -> Void
public typealias OnEvent = @convention(c) (Int32, UnsafePointer<CChar>) -> Void

@objc public class UnityComponentRuntime: NSObject {
    private static var runtimes = [Int32: ComponentRuntime]()
    private static var lastHandle : Int32 = 0
    private static let queue = DispatchQueue(label: "ComponentRuntimeHandles")

    private static func NextHandle() -> Int32 {
        return queue.sync {
            lastHandle += 1
            return lastHandle
        }
    }

    @objc public static func create() -> Int32 {
        let runtime = ComponentRuntime.init()
        let handle = NextHandle()
        UnityComponentRuntime.runtimes[handle] = runtime
        return handle
    }

    @objc public static func load(
        handle: Int32,
        component: String,
        config: String,
        runtimeHandle: Int32,
        callback: @escaping OnComponentLoadedCallback
    ) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring load for unknown component runtime %@", handle);
            return
        }

        var parameters = try? JSONSerialization.jsonObject(with: Data(config.utf8)) as? [String: Any] ?? [String: Any]()

        switch component {
        case "com.coachai.engine.ar.components.ARPublisher":
            ARSession.addCallbackOnInit {
                parameters!["com.coachai.engine.ar.components.AugmentedRealitySourceParameter"] = $0.asAugmentedRealitySource()
                runtime.load(runtimeComponentSpec: component, configOverwrites: parameters!, onLoaded: {
                    callback(runtimeHandle, component, true)
                })
            }
        case "com.coachai.engine.camera.components.CameraPublisher":
            AVCaptureSession.addCallbackOnInit {
                parameters!["com.coachai.engine.camera.components.VideoSourceParameter"] = $0.asVideoSource()
                runtime.load(runtimeComponentSpec: component, configOverwrites: parameters!, onLoaded: {
                    callback(runtimeHandle, component, true)
                })
            }
        default:
            runtime.load(runtimeComponentSpec: component, configOverwrites: parameters ?? [String: Any](), onLoaded: {
                callback(runtimeHandle, component, true)
            })
        }
    }

    @objc public static func configure(
        handle: Int32,
        component: String,
        config: String,
        runtimeHandle: Int32,
        callback: @escaping OnComponentConfgiuredCallback
    ) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring configure for unknown component runtime %@", handle);
            return
        }

        let parameters = try? JSONSerialization.jsonObject(with: Data(config.utf8)) as? [String: Any]

        runtime.reconfigure(runtimeComponentSpec: component, configOverwrites: parameters ?? [String: Any]()) {
            callback(runtimeHandle, component, true)
        }
    }

    @objc public static func resume(handle: Int32) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring resume for unknown component runtime %@", handle);
            return
        }

        runtime.start { /* nothing */ }
    }

    @objc public static func pause(handle: Int32) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring dispose for unknown component runtime %@", handle);
            return
        }
        runtime.pause { /* nothing */ }
    }

    @objc public static func dispose(handle: Int32) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring dispose for unknown component runtime %@", handle);
            return
        }
        runtime.dispose()
    }

    @objc public static func subscribe(
        handle: Int32,
        event: String,
        runtimeHandle: Int32,
        callback: @escaping OnEvent
    ) -> Int64 {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring subscribe for unknown component runtime %@", handle);
            return -1
        }
        let subscriptionId = runtime.subscribe(eventClass: event) { (event) in
            let json = Serialization_helpersKt.serializeToJson(event)
            callback(runtimeHandle, json)
        }
        return subscriptionId
    }

    @objc public static func unsubscribe(handle: Int32, subscriptionId: Int64) {
        guard let runtime = UnityComponentRuntime.runtimes[handle] else {
            NSLog("Ignoring unsubscribe for unknown component runtime %@", handle);
            return
        }
        runtime.unsubscribe(subscription: subscriptionId)
    }
}
