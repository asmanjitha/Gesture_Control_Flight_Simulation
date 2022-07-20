/*
 * Copyright (c) 2019 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#import "UnityFramework/UnityFramework-Swift.h"
#import "UnityInterface.h"

#pragma mark - C interface

char* cStringCopy(const char* string) {
    if (string == NULL)
        return NULL;

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);

    return res;
}

extern "C" {
    void setupIOSUnityBridge(const char* logLevel) {
        NSString *level = [NSString stringWithUTF8String:logLevel];
        [[UnityBridgeController instance] setupWithLogLevel: level];
    }

    bool isSdkInitialized() {
        return [[UnityBridgeController instance] isSdkInitialized];
    }

    bool isSdkActivityInitialized() {
        return [[UnityBridgeController instance] isActivityInitialized];
    }

    void startActivity(const char* activityServiceConfig) {
        NSString *strActivityConfig = [NSString stringWithUTF8String: activityServiceConfig];
        [[UnityBridgeController instance] startActivityWithActivityConfigJson: strActivityConfig];
    }

    void stopActivity() {
        [[UnityBridgeController instance] stopActivity];
    }

    void subscribePublicEvent(const char* event) {
        NSString *eventClassName = [NSString stringWithUTF8String:event];
        [[UnityBridgeController instance] subscribePublicEventWithEventClassName: eventClassName];
    }

    void unsubscribePublicEvent(const char* event) {
        NSString* eventClassName = [NSString stringWithUTF8String:event];
        [[UnityBridgeController instance] unsubscribePublicEventWithEventClassName: eventClassName];
    }

    void pollEvents(char*** dest, int32_t* length) {
        NSArray* events = [[UnityBridgeController instance] pollEvents];
        int32_t count = (int32_t) [events count];
        char **array = (char **) malloc((count + 1) * sizeof(char*));

        for (unsigned i = 0; i < count; i++) {
             array[i] = cStringCopy([[events objectAtIndex:i] UTF8String]);
        }

        *length = count;
        *dest = array;
    }

    void subscribeWorldObjectEvents(int32_t eventTypes) {
        [[UnityBridgeController instance] subscribeWorldObjectEventsWithEventTypes: eventTypes];
    }

    void unsubscribeWorldObjectEvents(int32_t eventTypes) {
        [[UnityBridgeController instance] unsubscribeWorldObjectEventsWithEventTypes: eventTypes];
    }

    void coach_activity_send_command(const char* command, const char* json) {
        NSString* commandClassName = [NSString stringWithUTF8String:command];
        NSString* jsonString = [NSString stringWithUTF8String:json];
        [[UnityBridgeController instance] sendCommandWithCommand:commandClassName jsonData:jsonString];
    }

    void coach_activity_take_decision(int32_t requestId, const char* json) {
        NSString* jsonString = [NSString stringWithUTF8String:json];
        [[UnityBridgeController instance] takeDecisionWithRequestId:requestId json:jsonString];
    }

    void coach_activity_subscribe_metric(const char* metricId) {
        NSString* metricIdString = [NSString stringWithUTF8String:metricId];
        [[UnityBridgeController instance] subscribeMetricWithMetricId: metricIdString];
    }

    void coach_activity_fulfill_requirement(int32_t requirementId, const char* requirementJson) {
        NSString* nsStringJson = [NSString stringWithUTF8String:requirementJson];
        [[UnityBridgeController instance] fulfillRequirementWithRequirementId:requirementId json:nsStringJson];
    }

    typedef void (*OnDecisionRequestT)(int32_t requestId, const char* request);
    typedef void (*OnFinishT)(int32_t flag);
    typedef void (*OnInitT)();
    typedef void (*OnErrorT)(const char* json);
    typedef void (*OnRequireT)(int32_t requirementId, const char* type);

    void coach_activity_initialize_callbacks(
        OnDecisionRequestT onDecisionRequestCallback,
        OnFinishT onFinishCallback,
        OnInitT onInitCallback,
        OnErrorT onErrorCallback,
        OnRequireT onRequireCallback
    ) {
        [UnityBridgeController setupCallbacksOnDecisionRequest:onDecisionRequestCallback onFinish:onFinishCallback onInit:onInitCallback onError:onErrorCallback onRequire:onRequireCallback];
    }

    NSProcessInfoThermalState getThermalState() {
        return [[NSProcessInfo processInfo] thermalState];
    }

    char* getBuildNumber() {
        NSString* buildNumber = [[[NSBundle mainBundle] infoDictionary] objectForKey:@"CFBundleVersion"];

        return cStringCopy([buildNumber UTF8String]);
    }

    char* getThermalStateName() {
        NSString * thermalStateName = [UnityBridgeController nameForThermalState: getThermalState()];

        return cStringCopy([thermalStateName UTF8String]);
    }

    void quit() {
        exit(-1);
    }

    // MARK: COMPONENT API

    int32_t coach_cr_create() {
        int32_t handle = [UnityComponentRuntime create];
        return handle;
    }

    typedef void (*OnComponentLoadedT)(int32_t runtimeHandle, const char *componentId, bool success);

    void coach_cr_load(int32_t handle, const char* component, const char* config, OnComponentLoadedT callback, int32_t runtimeHandle) {
        NSString* componentId = [NSString stringWithUTF8String:component];
        NSString* configString = [NSString stringWithUTF8String:config];

        [UnityComponentRuntime loadWithHandle:handle component:componentId config:configString runtimeHandle:runtimeHandle callback: callback];
    }

    typedef void (*OnComponentConfiguredT)(int32_t runtimeHandle, const char *componentId, bool success);

    void coach_cr_configure(int32_t handle, const char* component, const char* config, OnComponentConfiguredT callback, int32_t runtimeHandle) {
        NSString* componentId = [NSString stringWithUTF8String:component];
        NSString* configString = [NSString stringWithUTF8String:config];

        [UnityComponentRuntime configureWithHandle:handle component:componentId config:configString runtimeHandle:runtimeHandle callback: callback];
    }

    void coach_cr_resume(int32_t handle) {
        [UnityComponentRuntime resumeWithHandle:handle];
    }

    void coach_cr_pause(int32_t handle) {
        [UnityComponentRuntime pauseWithHandle:handle];
    }

    void coach_cr_dispose(int32_t handle) {
        [UnityComponentRuntime disposeWithHandle:handle];
    }

    typedef void (*OnEventT)(int32_t runtimeHandle, const char *event);

    int64_t coach_cr_subscribe(int32_t handle, const char* eventIdentifier, OnEventT callback, int32_t runtimeHandle) {
        NSString* eventId = [NSString stringWithUTF8String:eventIdentifier];
        int64_t subscriptionId = [UnityComponentRuntime subscribeWithHandle:handle event:eventId runtimeHandle:runtimeHandle callback:callback];
        return subscriptionId;
    }

    void coach_cr_unsubscribe(int32_t handle, int64_t subscriptionId) {
        [UnityComponentRuntime unsubscribeWithHandle:handle subscriptionId:subscriptionId];
    }
}
