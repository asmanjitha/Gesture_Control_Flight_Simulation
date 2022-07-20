/*
 * Copyright (c) 2022 Coach AI GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Collections;
using Random = UnityEngine.Random;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace CoachAiSamples.Balloon
{
    public class BalloonSpawner : MonoBehaviour {
        [SerializeField]
        private GameObject balloonPrefab;

        [SerializeField]
        private int spawnInterval;

        private void Start() {
            //Start Spawner coroutine
            StartCoroutine(nameof(SpawnBalloon));
        }

        IEnumerator SpawnBalloon() {
            while (true) {
                Vector3 spawnPosition = CalculateBalloonPosition();
                var activeBalloon = Instantiate(balloonPrefab, spawnPosition, Quaternion.identity);
                yield return new WaitForSeconds(spawnInterval);
                Destroy(activeBalloon);
            }
        }

        private Vector3 CalculateBalloonPosition() {
            Debug.Assert(Camera.main != null, "Camera.main != null");
            return Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f), 93));
        }

    }
}
