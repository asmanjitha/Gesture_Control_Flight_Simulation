// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

namespace CoachAiEngine {

    public readonly struct ActivityRequirement {
        private const string ArSource = "com.coachai.engine.ar.AugmentedRealitySource";
        private const string CameraSource = "com.coachai.engine.camera.VideoSource";
        public readonly int Id;
        public readonly string Type;

        public ActivityRequirement(int id, string type) {
            Id = id;
            Type = type;
        }

        public bool RequiresArSource => Type == ArSource;

        public bool RequiresCameraSource => Type == CameraSource;

        public void Fulfill(object fulfillment) => CoachActivityRuntime.FulfillRequirement(Id, fulfillment);
    }
}
