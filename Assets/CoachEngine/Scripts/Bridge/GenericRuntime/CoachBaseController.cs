// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine {

    public class CoachBaseController : MonoBehaviour {
        [Header("Coach-AI Setup")] [Tooltip("CoachAI settings")] [SerializeField]
        private CoachEngineSettings settings;

        public CoachEngineSettings Settings {
            get {
                return settings ??= ScriptableObject.CreateInstance<CoachEngineSettings>();
            }
        }

        [Tooltip("Whether to start the Coach-AI engine, when this game object is instantiated (on Awake)")]
        [SerializeField]
        protected bool initializeEngineOnStartup = true;

        [Tooltip("Automatically add Android AR Bridge on AR Scenes")] [SerializeField]
        protected bool automaticallyAddAndroidARBridgeIfNecessary = true;

        [Tooltip("Prevent the display from going to sleep when a sdk activity or component is active.")]
        [SerializeField]
        protected bool disableScreenSleep = true;

        protected virtual void Awake() {
            if (initializeEngineOnStartup)
                CoachEngine.Initialize(Settings);
        }
    }
}
