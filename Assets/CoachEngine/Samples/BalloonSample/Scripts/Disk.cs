/*
 * Copyright (c) 2022 Coach AI GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;

namespace CoachAiSamples.Balloon  {
    public class Disk : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D col) {
            if (col.gameObject.CompareTag("balloon")) {
                Destroy(col.gameObject);
            }
        }
    }
}
