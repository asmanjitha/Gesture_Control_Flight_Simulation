/*
 * Copyright (c) 2022 Coach AI GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using CoachAiEngine;
using CoachAiEngine.Analytics.Pose;
using JetBrains.Annotations;
using UnityEngine;

namespace CoachAiSamples.Balloon  {
    public class PoseController : MonoBehaviour {
        [SerializeField] private GameObject leftDisk;
        [SerializeField] private GameObject rightDisk;
        [SerializeField] private Canvas canvas;

        public void UpdatePose(PublicEvent @event) {
            // BodyPose is helper class provided by CoachAiUnity Plugin, It automatically parses the json response from the SDK.
            var poses = BodyPose.CreateFrom(@event, CoordinateSpace.World);
            if (poses.Count == 0) {
                return;
            }
            // Update noseSprite position
            UpdateJoint(poses[0], poses[0].LeftWrist, leftDisk);
            UpdateJoint(poses[0], poses[0].RightWrist, rightDisk);
        }

        private void UpdateJoint(BodyPose bodyPose, [CanBeNull] BodyPose.Joint joint, GameObject jointGo) {
            if (joint == null) {
                Debug.Log("Joint is null");
                // If joint is null, make the point invisible
                jointGo.SetActive(false);
            } else {
                // Convert to canvas local point
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(),
                    bodyPose.ToScreenCoordinates(joint),
                    Camera.main,
                    out var pos
                );
                // Update jointPosition
                jointGo.transform.localPosition = pos;
                jointGo.SetActive(true);
            }
        }
    }
}
