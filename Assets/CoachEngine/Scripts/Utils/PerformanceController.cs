// /*
//  * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine {

    /**
     * Allows to specify a performance profile for the app.
     */
    public class PerformanceController : MonoBehaviour {

        [SerializeField] private PerformanceProfile profile;
        [SerializeField] private bool enablePerformanceProfile = true;
        private void Start() {
            if (!enablePerformanceProfile) return;
            PerformanceUtils.ConfigureFor(profile);
        }
    }
}
