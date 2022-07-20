/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

#if COACH_AI_AR_FOUNDATION

using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CoachAiEngine {

    public static class ARPlaneManagerExtensions {
        public static void HideTrackedPlanes(this ARPlaneManager planeManager) {
            foreach (var plane in planeManager.trackables) {
                plane.gameObject.SetActive(false);
            }
        }

        public static void HideSubsumedPlanes(this ARPlaneManager planeManager) {
            foreach (var plane in planeManager.trackables) {
                plane.gameObject.SetActive(plane.subsumedBy == null);
            }
        }

        public static void HidePlanesAbove(this ARPlaneManager planeManager, double y) {
            foreach (var plane in planeManager.trackables) {
                plane.gameObject.SetActive(plane.transform.position.y <= y);
            }
        }

        /// <summary>
        /// Get all horizontal upward subsumed planes
        /// </summary>
        public static List<ARPlane> AllTrackedHorizontalUpwardSubsumedPlanes(this ARPlaneManager planeManager) {
            var allTrackedPlanes = planeManager.trackables;
            var trackedHorizontalUpwardPlanes = new List<ARPlane>();

            foreach (var plane in allTrackedPlanes) {
                if (plane.trackingState == TrackingState.Tracking && plane.alignment == PlaneAlignment.HorizontalUp) {
                    var subsumedPlane = plane.subsumedBy;
                    if (subsumedPlane == null) {
                        trackedHorizontalUpwardPlanes.Add(plane);
                    } else {
                        trackedHorizontalUpwardPlanes.Add(subsumedPlane);
                    }
                }
            }

            return trackedHorizontalUpwardPlanes;
        }

        /// <summary>
        /// Get all horizontal upward planes
        /// </summary>
        public static List<ARPlane> AllTrackedHorizontalUpwardPlanes(this ARPlaneManager planeManager) {
            var trackedHorizontalUpwardPlanes = new List<ARPlane>();

            foreach (var plane in planeManager.trackables) {
                if (plane.trackingState == TrackingState.Tracking && plane.alignment == PlaneAlignment.HorizontalUp && plane.subsumedBy != null) {
                    trackedHorizontalUpwardPlanes.Add(plane);
                }
            }

            return trackedHorizontalUpwardPlanes;
        }

        /// <summary>
        /// Returns the largest horizontal plane or null
        /// </summary>
        /// <param name="planeManager"></param>
        /// <returns></returns>
        public static ARPlane LargestHorizontalPlane(this ARPlaneManager planeManager) {
            ARPlane largestPlane = null;
            var currentSize = 0f;

            foreach (var plane in planeManager.AllTrackedHorizontalUpwardSubsumedPlanes()) {
                var magnitude = plane.size.magnitude;
                if (magnitude > currentSize) {
                    currentSize = magnitude;
                    largestPlane = plane;
                }
            }

            return largestPlane;
        }
    }
}

#endif
