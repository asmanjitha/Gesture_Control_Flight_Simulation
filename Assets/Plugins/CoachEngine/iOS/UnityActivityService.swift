/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

import Foundation
import CoachAIKit
import ARKit
import AVFoundation

class UnityActivityService {
    var activityHandler: ActivityHandler?

    static var decisionRequestHandler: OnDecisionRequest = {_,_ in }
    static var finishHandler: OnFinish = {_ in }
    static var initHandler: OnInit = { }
    static var errorHandler: OnError = {_ in }
    static var requirementHandler: OnRequire = {_,_ in }

    private var subscribedWorldEvents: Int = 0x00
    private var worldObjectEventHandler: (WorldObjectEvent, EventType) -> Void
    private var pendingCallbacks: [Int32: (String) -> Void] = [Int32: (String) -> Void]()
    private var earlySubscriptions = [(ActivityHandler) -> Void]()

    init(worldObjectEventHandler: @escaping (WorldObjectEvent, EventType) -> Void) {
        self.worldObjectEventHandler = worldObjectEventHandler
    }

    func start(activity: String, parameters: [String:Any] = [String:Any](), variant: String? = nil) {
        if !ActivityService().isFree {
            stop()
        }

        let activities: [GenericActivitySpec] = Activities().all as! [GenericActivitySpec]
        guard let spec = activities.first(where: { $0.identifier == activity }) else {
            fatalError("unknown activity identifier: \(activity)")
        }

        let parameterSet = spec.availableParameters()
        parameterSet.setValues(parameterMap: parameters)

        if let activityVariant = spec.availableVariants().first(where: { $0.identifier == variant }) {
            spec.instantiate(parameters: parameterSet, variant: activityVariant, delegate: self)
        } else {
            spec.instantiate(parameters: parameterSet, delegate: self)
        }
    }

    func stop() {
        activityHandler?.abort()
        onAbort()
    }

    func subscribeWorldObjectEvents(eventTypes: Int) {
        subscribedWorldEvents |= eventTypes
    }

    func unsubscribeWorldObjectEvents(eventTypes: Int) {
        subscribedWorldEvents &= ~eventTypes
    }

    func subscribePublicEvent(eventSpec: PublicEventSpec, callback: @escaping (PublicEvent) -> Void) -> Int64? {
        guard let handler = activityHandler else {
            earlySubscriptions.append({ $0.subscribeTo(event: eventSpec, callback: callback) })
            return nil
        }

        return handler.subscribeTo(event: eventSpec, callback: callback)
    }

    func subscribeFeedback(feedbackSpec: FeedbackSpec, callback: @escaping (PublicEvent) -> Void) -> Int64? {
        guard let handler = activityHandler else {
            earlySubscriptions.append({ $0.subscribeTo(feedback: feedbackSpec, callback: callback) })
            return nil
        }
        return handler.subscribeTo(feedback: feedbackSpec, callback: callback)
    }

    func unsubscribePublicEvent(subscriptionId: Int64) {
        activityHandler?.unsubscribe(handle: subscriptionId)
    }

    func takeDecision(requestId: Int32, json: String) {
        guard let callback = pendingCallbacks[requestId] else {
            NSLog("Ignoring invalid decision request with id=%@.", requestId)
            return
        }
        callback(json)
    }

    func fulfillRequirement(requirementId: Int32, json: String) {
        guard let callback = pendingCallbacks[requirementId] else {
            NSLog("Ignoring attempt to fulfill requirement with invalid id=%@.", requirementId)
            return
        }
        callback(json)
    }

    func subscribeActivityMetric(metricId: String, callback: @escaping (Any) -> Void) {
        guard let activityMetrics = activityHandler?.getMetrics() else {
            NSLog("Failed to subscribe to metric %@. Activity handler not available.", metricId)
            return
        }
        let metricSpecs = activityMetrics.metrics as NSArray as! [MetricSpec]
        if let metricSpec = metricSpecs.first(where: { $0.identifier == metricId}) {
            activityMetrics.subscribeTo(metric: metricSpec, callback: callback)
        } else {
            NSLog("Failed to subscribe to unknown metric %@", metricId)
        }
    }
}

enum FinishFlags: Int32 {
    case Finish = 0
    case Abort = 1
}

extension UnityActivityService: ActivityDelegate {
    func onError(error: ActivityError) {
        UnityActivityService.errorHandler(error.message)
    }

    func setHandler(handler: ActivityHandler?) {
        activityHandler = handler
    }

    func onInit() {
        guard let handler = activityHandler else {
            return
        }

        earlySubscriptions.forEach { $0(handler) }
        earlySubscriptions.removeAll()
        UnityActivityService.initHandler()
    }

    func onAbort() {
        UnityActivityService.finishHandler(FinishFlags.Abort.rawValue)
    }

    func onFinish() {
        UnityActivityService.finishHandler(FinishFlags.Finish.rawValue)
    }

    func onDecisionRequest(decisionRequest: ActivityDecisionRequest<ActivityDecision>) {
        let json = Serialization_helpersKt.serializeToJson(decisionRequest)
        let requestId = Int32.random(in: 1...Int32.max)

        let callback : (String) -> Void = {
            guard self.pendingCallbacks.removeValue(forKey: requestId) != nil else {
                return
            }
            guard let clazz = decisionRequest.decisionKotlinClass.qualifiedName else {
                decisionRequest.chooseDefault()
                return
            }
            guard let serializer = RecordableRegistries.serializer(for: clazz) else {
                NSLog("Could not find a suitable deserializer for %@.", clazz)
                decisionRequest.chooseDefault()
                return
            }
            guard let decision = Json.load(json: $0, serializer: serializer) else {
                NSLog("Could not load decision %@.", clazz)
                decisionRequest.chooseDefault()
                return
            }
            decisionRequest.takeDecision(decision: decision as! ActivityDecision)
        }

        pendingCallbacks[requestId] = callback
        UnityActivityService.decisionRequestHandler(requestId, json)
    }

    func onRequire(requirement: Requirement<AnyObject, AnyObject>) {
        switch requirement as AnyObject {
        case let requirement as ARSessionRequirement:
            guard let session = ARSession.currentSession else {
                ARSession.addCallbackOnInit {
                    requirement.fulfill(fulfillment: $0.asAugmentedRealitySource())
                }
                return
            }
            requirement.fulfill(fulfillment: session.asAugmentedRealitySource())
        case let requirement as CameraSessionRequirement:
            guard let session = AVCaptureSession.currentSession, session.isRunning else {
                AVCaptureSession.addCallbackOnInit {
                    requirement.fulfill(fulfillment: $0.asVideoSource())
                }
                return
            }
            requirement.fulfill(fulfillment: session.asVideoSource())
        default:
            requirement.fulfillPreliminary(fulfillment: requirement.defaultFulfillment as AnyObject)
            require(requirement: requirement)
//            fatalError("Unsupported requirement \(requirement)")
        }
    }

    private func require(requirement: Requirement<AnyObject, AnyObject>) {
        guard let clazz = requirement.kotlinClass.qualifiedName else {
            requirement.fulfill(fulfillment: requirement.defaultFulfillment as AnyObject)
            return
        }
        let requestId = Int32.random(in: 1...Int32.max)

        let callback : (String) -> Void = {
            guard self.pendingCallbacks.removeValue(forKey: requestId) != nil else {
                return
            }
            guard let serializer = RecordableRegistries.serializer(for: clazz) else {
                NSLog("Could not find a suitable deserializer for %@.", clazz)
                requirement.fulfill(fulfillment: requirement.defaultFulfillment as AnyObject)
                return
            }
            guard let fulfillment = Json.load(json: $0, serializer: serializer) else {
                NSLog("Could not load requirement %@.", clazz)
                requirement.fulfill(fulfillment: requirement.defaultFulfillment as AnyObject)
                return
            }
            requirement.fulfill(fulfillment: fulfillment as AnyObject )
        }

        pendingCallbacks[requestId] = callback
        UnityActivityService.requirementHandler(requestId, clazz)
    }

    func onReady() {
        activityHandler?.start()
    }

    func onObjectAdded(objEvent: WorldObjectEvent) {
        if ((subscribedWorldEvents & 0x01) != 0) {
            worldObjectEventHandler(objEvent, EventType.added)
        }
    }

    func onObjectUpdated(objEvent: WorldObjectEvent) {
        if ((subscribedWorldEvents & 0x02) != 0) {
            worldObjectEventHandler(objEvent, EventType.updated)
        }
    }

    func onObjectRemoved(objEvent: WorldObjectEvent) {
        if ((subscribedWorldEvents & 0x04) != 0) {
            worldObjectEventHandler(objEvent, EventType.removed)
        }
    }
}
