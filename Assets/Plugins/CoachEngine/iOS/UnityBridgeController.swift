/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

import Foundation
import CoachAIKit
import ARKit
import AVFoundation
import Dispatch

public typealias OnDecisionRequest = @convention(c) (Int32, UnsafePointer<CChar>) -> Void
public typealias OnFinish = @convention(c) (Int32) -> Void
public typealias OnInit = @convention(c) () -> Void
public typealias OnError = @convention(c) (UnsafePointer<CChar>) -> Void
public typealias OnRequire = @convention(c) (Int32, UnsafePointer<CChar>) -> Void

@objc public class UnityBridgeController: NSObject {
    @objc public static let instance = UnityBridgeController()
    private var activityService: UnityActivityService?

    private var publicEventSubscriptions = [String: Int64]()
    private var eventBuffer = [String: String]()
    private let eventBufferQueue = DispatchQueue(label: "eventbuffer.lock", attributes: .concurrent)
    private var metricBuffer = [String:Any]()
    private let metricBufferQueue = DispatchQueue(label: "metricbuffer.lock", attributes: .concurrent)

    @objc public static func setupCallbacks(onDecisionRequest: @escaping OnDecisionRequest,
                                            onFinish: @escaping OnFinish,
                                            onInit: @escaping OnInit,
                                            onError: @escaping OnError,
                                            onRequire: @escaping OnRequire) {
        UnityActivityService.decisionRequestHandler = onDecisionRequest
        UnityActivityService.finishHandler = onFinish
        UnityActivityService.initHandler = onInit
        UnityActivityService.errorHandler = onError
        UnityActivityService.requirementHandler = onRequire
    }

    @objc public func setup(logLevel: String) {
        ARSession.swizzle() // capture ARSession
        AVCaptureSession.swizzle() // captures AVCaptureSession
        AVCaptureVideoDataOutput.swizzle() // capture sample buffer delegate
        AVCaptureDataOutputSynchronizer.swizzle() // capture synchronized data delegate
        OperationQueue.swizzle()
        CoachAISDK.initializeOnce(logLevel: logLevel)
    }

    @objc public func isSdkInitialized() -> Bool {
        return CoachAISDK.isInitialized()
    }

    @objc public func isActivityInitialized() -> Bool {
        return isSdkInitialized() && activityService?.activityHandler != nil
    }

    @objc public func startActivity(activityConfigJson: String) {
        let jsonData = Data(activityConfigJson.utf8)
        guard let config = try? JSONSerialization.jsonObject(with: jsonData) as? [String: Any] else {
            fatalError("invalid activity config: \(activityConfigJson)")
        }

        guard let id = config["Identifier"] as? String else {
            fatalError("invalid activity identifier: \(activityConfigJson)")
        }

        let params = config["Parameters"] as? [String:Any] ?? [String:Any]()
        let variant = config["Variant"] as? String

        activityService = UnityActivityService(worldObjectEventHandler: dumpWorldObjectEvent(event: eventType:))
        activityService?.start(activity: id, parameters: params, variant: variant)
    }

    @objc public func stopActivity() {
        activityService?.stop()
        publicEventSubscriptions.removeAll()
        eventBuffer.removeAll()
        metricBuffer.removeAll()
    }

    @objc public func subscribeWorldObjectEvents(eventTypes: Int) {
        activityService?.subscribeWorldObjectEvents(eventTypes: eventTypes)
    }

    @objc public func unsubscribeWorldObjectEvents(eventTypes: Int) {
        activityService?.unsubscribeWorldObjectEvents(eventTypes: eventTypes)
    }

    @objc public func sendCommand(command: String, jsonData: String) {
        guard let handler = activityService?.activityHandler else {
            NSLog("Command %@ discarded. Activity is not initialized yet!", command)
            return
        }

        guard let serializer = RecordableRegistries.serializer(for: command) else {
            NSLog("Could not find a suitable deserializer for command %@.", command)
            return
        }

        let json = JsonKt.Json(from: Json.Default.init()) {
            $0.ignoreUnknownKeys = true
        }
        let activityCommand = json.decodeFromString(deserializer: serializer, string: command)
        handler.sendCommand(command: activityCommand as! ActivityCommand)
    }

    @objc public func takeDecision(requestId: Int32, json: String) {
        activityService?.takeDecision(requestId: requestId, json: json)
    }

    @objc public func fulfillRequirement(requirementId: Int32, json: String) {
        activityService?.fulfillRequirement(requirementId: requirementId, json: json)
    }

    @objc public func subscribeMetric(metricId: String) {
        activityService?.subscribeActivityMetric(metricId: metricId, callback: { (value: Any) -> Void in
            self.metricBufferQueue.async(flags: .barrier) {
                self.metricBuffer[metricId] = value
            }
        })
    }

    @objc public func subscribePublicEvent(eventClassName: String) {
        guard publicEventSubscriptions[eventClassName] == nil else {
            return
        }

        guard let handler = activityService?.activityHandler else {
            NSLog("Activity is not initialized yet!")
            return
        }

        if let eventSpec = handler.availableEvents().first(where: {
            $0.type.qualifiedName == eventClassName
        }) {
            // FIXME ensure eventSpec.type is implementing Recordable interface
            if let subscriptionId = activityService?.subscribePublicEvent(eventSpec: eventSpec, callback: dumpPublicEvent(event:)) {
                publicEventSubscriptions[eventClassName] = subscriptionId
            }
        } else if let feedbackSpec = handler.availableFeedback().first(where: {
            $0.type.qualifiedName == eventClassName
        }) {
            // FIXME ensure feedbackSpec.type is implementing Recordable interface
            if let subscriptionId = activityService?.subscribeFeedback(feedbackSpec: feedbackSpec, callback: dumpPublicEvent(event:)) {
                publicEventSubscriptions[eventClassName] = subscriptionId
            }
        } else {
            NSLog("Not subscribing to %@ - not a valid event or feedback.", eventClassName);
        }
    }

    @objc public func unsubscribePublicEvent(eventClassName: String) {
        if let subcriptionId = publicEventSubscriptions[eventClassName] {
            activityService?.unsubscribePublicEvent(subscriptionId: subcriptionId)
        }
    }

    @objc public func pollEvents() -> [String] {
        var events = [String]()

        metricBufferQueue.sync {
            if metricBuffer.isEmpty {
                return
            }

            let metricsEvent : [String:Any] = [
                "data": metricBuffer,
                "timestamp": GetTimeMillisKt.monotonicTimeFraction(),
                "type": "com.coachai.engine.activity.ActivityMetrics"]

            if let data = try? JSONSerialization.data(withJSONObject: metricsEvent) {
                let json : String = String(data: data, encoding: String.Encoding.utf8)!
                events.append(json)
            }

            metricBuffer.removeAll()
        }

        eventBufferQueue.sync {
            events.append(contentsOf: eventBuffer.values)
            eventBuffer.removeAll()
        }
        return events
    }

    @objc public static func nameFor(thermalState: ProcessInfo.ThermalState) -> String {
        return thermalState.nameFor()
    }

    private func dumpPublicEvent(event: PublicEvent) {
        let json = Serialization_helpersKt.serializeToJson(event)
        let eventClass = String(describing: type(of: event))
        eventBufferQueue.async(flags: .barrier) {
            self.eventBuffer[eventClass] = json
        }
    }

    private func dumpWorldObjectEvent(event: WorldObjectEvent, eventType: EventType) {
        let id = event.worldObject.id
        let json = event.serializeToJson(eventType: eventType)
        eventBufferQueue.async(flags: .barrier) {
            self.eventBuffer[id] = json
        }
    }
}

extension RecordableRegistries {
    static func serializer(for: String) -> KSerializer? {
        RecordableRegistries()
            .all
            .compactMap({ $0.serializerFor(type: `for`) })
            .first
    }
}

extension Json {
    static func load(json: String, serializer: KSerializer) -> Any? {
        let codec = JsonKt.Json(from: Json.Default.init()) {
            $0.ignoreUnknownKeys = true
        }
        return codec.decodeFromString(deserializer: serializer, string: json)
    }
}

fileprivate extension UnityActivityService {
    func tryGetEventSpecFromEventClassName(eventClassName: String) -> PublicEventSpec? {
        return activityHandler?.availableEvents().first(where: {
            $0.type.qualifiedName == eventClassName
        })
    }
}

// MARK: CoachAISDK

extension CoachAISDK {
    static private var didInitialize = false

    fileprivate static func isInitialized() -> Bool {
        return didInitialize
    }

    private static func discoverModelDirectories() {
        let frameworksPath = Bundle.main.bundlePath + "/Frameworks/"
        let fileManager = FileManager.default

        do {
            let frameworks = try fileManager.contentsOfDirectory(atPath: frameworksPath)
            let modelSearchPaths = Model.Companion().searchPaths

            for framework in frameworks {
                let frameworkPath = frameworksPath + "/" + framework
                var isDirectory : ObjCBool = false
                fileManager.fileExists(atPath: frameworkPath, isDirectory: &isDirectory)
                if !isDirectory.boolValue {
                    continue
                }
                let content = try fileManager.contentsOfDirectory(atPath: frameworkPath)
                if content.contains(where: {
                    $0.hasSuffix("tflite") || $0.hasSuffix("mlmodel") || $0.hasSuffix("mlmodelc")
                }) {
                    modelSearchPaths.add(frameworkPath)
                }
            }
        } catch {
            // failed to read directory - no frameworks?
        }
    }

    fileprivate static func initializeOnce(logLevel: String) {
        guard !didInitialize else { return }

        // TODO keeping this for now to support adding models via pods but should be removed in future
        discoverModelDirectories()
        // initialize mobap sdk
        let mobapSDK = CoachAISDK()
        mobapSDK.initialize()
        mobapSDK.appMode = .production
        mobapSDK.logLevel = LogLevel.valueOf(name: logLevel)
        didInitialize = true

        NSLog("Coach-AI Engine initialization complete!")
    }
}

// MARK: ProcessInfo.ThermalState

extension ProcessInfo.ThermalState {
    func nameFor() -> String {
        switch self {
        case .nominal:
            return "nominal"
        case .fair:
            return "fair"
        case .serious:
            return "serious"
        case .critical:
            return "critical"
        @unknown default:
            return "unknown"
        }
    }
}

// MARK: LogLevel

extension LogLevel {
    static func valueOf(name: String) -> LogLevel {
        switch name.uppercased() {
        case "VERBOSE":
            return LogLevel.verbose
        case "DEBUG":
            return LogLevel.debug
        case "INFO":
            return LogLevel.info
        case "ERROR":
            return LogLevel.error
        case "WARN":
            return LogLevel.warn
        case "OFF":
            return LogLevel.off
        default:
            NSLog("%@ is not a valid log level.", name);
            return LogLevel.info
        }
    }
}

// MARK: AVCaptureSession

extension AVCaptureSession {
    static private var callbackOnInit: ((AVCaptureSession) -> Void)?
    static private var didSwizzle = false
    static weak var currentSession: AVCaptureSession?

    /// The idea is that the sessoin that uses its `startRunning()` is about to become or already is the current `AVCaptureSession`. With this logic, the new implementaion  of this method will use the old method implementation first, and then set itself to `AVCaptureSession.currentSession`.
    fileprivate static func swizzle() {
        guard !didSwizzle else { return }
        didSwizzle = true

        let copiedOriginalSelector = #selector(AVCaptureSession.originalStartRunning)
        let originalSelector = #selector(AVCaptureSession.startRunning)
        let swizzledSelector = #selector(AVCaptureSession.newStartRunning)

        let copiedOriginalMethod = class_getInstanceMethod(AVCaptureSession.self, copiedOriginalSelector)
        let originalMethod = class_getInstanceMethod(AVCaptureSession.self, originalSelector)
        let swizzledMethod = class_getInstanceMethod(AVCaptureSession.self, swizzledSelector)

        let oldImp = method_getImplementation(originalMethod!)
        method_setImplementation(copiedOriginalMethod!, oldImp)

        let newImp = method_getImplementation(swizzledMethod!)
        method_setImplementation(originalMethod!, newImp)
    }

    internal static func addCallbackOnInit(
        callback: @escaping (AVCaptureSession) -> Void
    ) {
        Self.callbackOnInit = callback
    }

    /// This method implements  the original implementaion of startRunning()
    @objc func originalStartRunning() {}

    /// will be used to extend the origianl startRunning() by first calling originalStartRunning() and then
    /// executing needed tasks before returning. In our case, we are setting `AVCaptureSession.currentSession` to self.
    @objc private func newStartRunning() {
        originalStartRunning()
        Self.currentSession = self

        if let callback = Self.callbackOnInit {
            Self.callbackOnInit = nil
            DispatchQueue.main.async {
                callback(self)
            }
        }
    }
}

// MARK: OperationQueue

extension OperationQueue {
    static private var didSwizzle = false

    fileprivate static func swizzle() {
        guard !didSwizzle else { return }
        didSwizzle = true

        let copiedOriginalSelector = #selector(OperationQueue.originalAddOperation(_:))
        let originalSelector = #selector(OperationQueue.addOperation(_:) as (OperationQueue) -> (@escaping ()->Void)->Void)
        let swizzledSelector = #selector(OperationQueue.newAddOperation(_:))

        let copiedOriginalMethod = class_getInstanceMethod(OperationQueue.self, copiedOriginalSelector)
        let originalMethod = class_getInstanceMethod(OperationQueue.self, originalSelector)
        let swizzledMethod = class_getInstanceMethod(OperationQueue.self, swizzledSelector)

        let oldImp = method_getImplementation(originalMethod!)
        method_setImplementation(copiedOriginalMethod!, oldImp)

        let newImp = method_getImplementation(swizzledMethod!)
        method_setImplementation(originalMethod!, newImp)

    }

    @objc fileprivate func newAddOperation(_ block: @escaping ()->()) {
        if name == "com.unity3d.WebOperationQueue" {
            qualityOfService = .userInteractive
        }
        originalAddOperation(block)
    }

    @objc fileprivate func originalAddOperation(_ block: @escaping ()->()) {}
}

// MARK: ARSession

extension ARSession {
    static private var callbackOnInit: ((ARSession) -> Void)?
    static private var didSwizzle = false
    static var currentSession: ARSession?

    /// Since Unity is hiding its ARSession and we do not want to rely on `ARCoachingOverlayView` we use swizzeling. The idea is that the sessoin that uses its `run(_:options)` is about to become or already is the current `ARSession`. With this logic, the new implementaion  of this method will use the old method implementation first, and then set itself to `ARSession.currentSession`.
    fileprivate static func swizzle() {
        guard !didSwizzle else { return }
        didSwizzle = true

        let copiedOriginalSelector = #selector(ARSession.originalRun(_:options:))
        let originalSelector = #selector(ARSession.run(_:options:))
        let swizzledSelector = #selector(ARSession.newRun(_:options:))

        let copiedOriginalMethod = class_getInstanceMethod(ARSession.self, copiedOriginalSelector)
        let originalMethod = class_getInstanceMethod(ARSession.self, originalSelector)
        let swizzledMethod = class_getInstanceMethod(ARSession.self, swizzledSelector)

        let oldImp = method_getImplementation(originalMethod!)
        method_setImplementation(copiedOriginalMethod!, oldImp)

        let newImp = method_getImplementation(swizzledMethod!)
        method_setImplementation(originalMethod!, newImp)
    }

    internal static func addCallbackOnInit(
        callback: @escaping (ARSession) -> Void
    ) {
        Self.callbackOnInit = callback
    }

    /// This method implements  the original implementaion of run(_:options:)
    @objc func originalRun(_ configuration: ARConfiguration, options: RunOptions = []) {}

    /// will be used to extend the origianl run(_:options:) by first calling originalRun(_:options:) and then
    /// executing needed tasks before returning. In our case, we are setting `ARSession.currentSession` to self.
    @objc private func newRun(_ configuration: ARConfiguration, options: RunOptions) {
        originalRun(configuration, options: options)
        Self.currentSession = self

        if let callback = Self.callbackOnInit {
            Self.callbackOnInit = nil
            DispatchQueue.main.async {
                callback(self)
            }
        }
    }
}

// MARK: AVCapture

extension AVCaptureDataOutputSynchronizer {
    static private var didSwizzle = false
    static fileprivate var delegate: ForwardAVCaptureDataOutputSynchronizerDelegate?
    /// Since Unity is hiding its ARSession and we do not want to rely on `ARCoachingOverlayView` we use swizzeling. The idea is that the sessoin that uses its `run(_:options)` is about to become or already is the current `ARSession`. With this logic, the new implementaion  of this method will use the old method implementation first, and then set itself to `ARSession.currentSession`.
    fileprivate static func swizzle() {
        guard !didSwizzle else { return }
        didSwizzle = true

        let copiedOriginalSelector = #selector(AVCaptureDataOutputSynchronizer.originalSetDelegate(_:queue:))
        let originalSelector = #selector(AVCaptureDataOutputSynchronizer.setDelegate(_:queue:))
        let swizzledSelector = #selector(AVCaptureDataOutputSynchronizer.newSetDelegate(_:queue:))

        let copiedOriginalMethod = class_getInstanceMethod(AVCaptureDataOutputSynchronizer.self, copiedOriginalSelector)
        let originalMethod = class_getInstanceMethod(AVCaptureDataOutputSynchronizer.self, originalSelector)
        let swizzledMethod = class_getInstanceMethod(AVCaptureDataOutputSynchronizer.self, swizzledSelector)

        let oldImp = method_getImplementation(originalMethod!)
        method_setImplementation(copiedOriginalMethod!, oldImp)

        let newImp = method_getImplementation(swizzledMethod!)
        method_setImplementation(originalMethod!, newImp)
    }

    fileprivate class ForwardAVCaptureDataOutputSynchronizerDelegate : NSObject, AVCaptureDataOutputSynchronizerDelegate {
        fileprivate weak var synchronizer: AVCaptureDataOutputSynchronizer?
        fileprivate var output: AVCaptureVideoDataOutput
        fileprivate weak var delegate: AVCaptureDataOutputSynchronizerDelegate?
        fileprivate var queue: Dispatch.DispatchQueue?
        init(synchronizer: AVCaptureDataOutputSynchronizer, output: AVCaptureVideoDataOutput, delegate: AVCaptureDataOutputSynchronizerDelegate?, queue: Dispatch.DispatchQueue?) {
            self.synchronizer = synchronizer
            self.output = output
            self.delegate = delegate
            self.queue = queue
        }

        @objc func dataOutputSynchronizer(_ synchronizer: AVCaptureDataOutputSynchronizer, didOutput synchronizedDataCollection: AVCaptureSynchronizedDataCollection) {
            delegate?.dataOutputSynchronizer(synchronizer, didOutput: synchronizedDataCollection)
            guard let connection = output.connection(with: .video) else {
                return
            }
            guard let sampleData = synchronizedDataCollection.synchronizedData(for: output) as? AVCaptureSynchronizedSampleBufferData else {
                return
            }
            let output = self.output
            let queue = output.sampleBufferCallbackQueue
            if (queue != nil) {
                queue?.async {
                    output.sampleBufferDelegate?.captureOutput?(output, didOutput: sampleData.sampleBuffer, from: connection)
                }
            } else {
                output.sampleBufferDelegate?.captureOutput?(output, didOutput: sampleData.sampleBuffer, from: connection)
            }
        }
    }


    /// This method implements  the original implementaion of run(_:options:)
    @objc func originalSetDelegate(_ delegate: AVCaptureDataOutputSynchronizerDelegate?, queue: Dispatch.DispatchQueue?) {}

    /// will be used to extend the origianl run(_:options:) by first calling originalRun(_:options:) and then
    /// executing needed tasks before returning. In our case, we are setting `ARSession.currentSession` to self.
    @objc private func newSetDelegate(_ delegate: AVCaptureDataOutputSynchronizerDelegate?, queue: Dispatch.DispatchQueue?) {
        if (delegate == nil) {
            Self.delegate?.delegate = nil
            Self.delegate = nil
            return
        }

        guard let output = dataOutputs.first(where: { $0 is AVCaptureVideoDataOutput }) as? AVCaptureVideoDataOutput else {
            originalSetDelegate(delegate, queue: queue)
            return
        }
        let forwardingDelegate = Self.delegate ?? ForwardAVCaptureDataOutputSynchronizerDelegate(
                synchronizer: self, output: output, delegate: delegate, queue: queue)
        Self.delegate = forwardingDelegate
        originalSetDelegate(forwardingDelegate, queue: queue)
    }
}

extension AVCaptureVideoDataOutput {
    static private var didSwizzle = false
    fileprivate static func swizzle() {
        guard !didSwizzle else { return }
        didSwizzle = true

        let copiedOriginalSelector = #selector(AVCaptureVideoDataOutput.originalSetSampleBufferDelegate(_:queue:))
        let originalSelector = #selector(AVCaptureVideoDataOutput.setSampleBufferDelegate(_:queue:))
        let swizzledSelector = #selector(AVCaptureVideoDataOutput.newSetSampleBufferDelegate(_:queue:))

        let copiedOriginalMethod = class_getInstanceMethod(AVCaptureVideoDataOutput.self, copiedOriginalSelector)
        let originalMethod = class_getInstanceMethod(AVCaptureVideoDataOutput.self, originalSelector)
        let swizzledMethod = class_getInstanceMethod(AVCaptureVideoDataOutput.self, swizzledSelector)

        let oldImp = method_getImplementation(originalMethod!)
        method_setImplementation(copiedOriginalMethod!, oldImp)

        let newImp = method_getImplementation(swizzledMethod!)
        method_setImplementation(originalMethod!, newImp)
    }

    @objc func originalSetSampleBufferDelegate(_ delegate: AVCaptureVideoDataOutputSampleBufferDelegate?, queue: Dispatch.DispatchQueue?) {}

    @objc private func newSetSampleBufferDelegate(_ delegate: AVCaptureVideoDataOutputSampleBufferDelegate?, queue: Dispatch.DispatchQueue?) {
        originalSetSampleBufferDelegate(delegate, queue: queue)
        guard delegate == nil, let synchronizerDelegate = AVCaptureDataOutputSynchronizer.delegate, synchronizerDelegate.output == self else {
            return
        }
        AVCaptureDataOutputSynchronizer.delegate = nil
        guard let synchronizer = synchronizerDelegate.synchronizer else {
            return
        }
//        synchronizerDelegate.synchronizer = nil
        synchronizer.originalSetDelegate(synchronizerDelegate.delegate, queue: synchronizerDelegate.queue)
    }
}
