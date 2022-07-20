// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using CoachAiEngine;

namespace CoachAiEngine {

    public static class CoachEngine {

        public static bool IsInitialized { get; private set; }

        public static void Initialize(CoachEngineSettings settings) {
            if (IsInitialized)
                return;

            IsInitialized = true;
            NativeLayerAPI.SetupNativeBridge(settings.logLevel);
        }
    }

}
