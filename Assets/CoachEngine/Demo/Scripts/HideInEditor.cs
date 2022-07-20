/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

namespace CoachAi.Samples {
    public class HideInEditor : MonoBehaviour {
        void Awake() {
#if UNITY_EDITOR
            gameObject.SetActive(false);
#endif
        }
    }
}
