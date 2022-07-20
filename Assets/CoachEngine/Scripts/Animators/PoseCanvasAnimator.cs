/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
*/

using CoachAiEngine.Analytics.Pose;
using JetBrains.Annotations;
using UnityEngine;

namespace CoachAiEngine.Animation {

    [AddComponentMenu("Coach-AI Engine/Animator/Body Pose/Canvas Animator")]
    public class PoseCanvasAnimator : PoseAnimator {
        [SerializeField] private Camera screenCamera;
        [SerializeField] private Canvas canvas;
        private RectTransform canvasRect;

        [Header("Joints")]
        [SerializeField] private GameObject LeftEye;
        [SerializeField] private GameObject RightEye;
        [SerializeField] private GameObject LeftEar;
        [SerializeField] private GameObject RightEar;
        [SerializeField] private GameObject Nose;
        [SerializeField] private GameObject LeftShoulder;
        [SerializeField] private GameObject LeftElbow;
        [SerializeField] private GameObject LeftWrist;
        [SerializeField] private GameObject RightShoulder;
        [SerializeField] private GameObject RightElbow;
        [SerializeField] private GameObject RightWrist;
        [SerializeField] private GameObject LeftHip;
        [SerializeField] private GameObject LeftKnee;
        [SerializeField] private GameObject LeftAnkle;
        [SerializeField] private GameObject RightHip;
        [SerializeField] private GameObject RightKnee;
        [SerializeField] private GameObject RightAnkle;

        private void Start() {
            if (!screenCamera) {
                Debug.LogWarning("No camera assigned to pose drawer.");
            }
            canvasRect = canvas.GetComponent<RectTransform>();
        }


        public override void UpdatePose(BodyPose bodyPose) {
            UpdateJoint(bodyPose, bodyPose.Nose, Nose);
            UpdateJoint(bodyPose, bodyPose.LeftEye, LeftEye);
            UpdateJoint(bodyPose, bodyPose.RightEye, RightEye);
            UpdateJoint(bodyPose, bodyPose.LeftEar, LeftEar);
            UpdateJoint(bodyPose, bodyPose.RightEar, RightEar);
            UpdateJoint(bodyPose, bodyPose.LeftShoulder, LeftShoulder);
            UpdateJoint(bodyPose, bodyPose.LeftElbow, LeftElbow);
            UpdateJoint(bodyPose, bodyPose.LeftWrist, LeftWrist);
            UpdateJoint(bodyPose, bodyPose.RightShoulder, RightShoulder);
            UpdateJoint(bodyPose, bodyPose.RightElbow, RightElbow);
            UpdateJoint(bodyPose, bodyPose.RightWrist, RightWrist);
            UpdateJoint(bodyPose, bodyPose.LeftHip, LeftHip);
            UpdateJoint(bodyPose, bodyPose.LeftKnee, LeftKnee);
            UpdateJoint(bodyPose, bodyPose.LeftAnkle, LeftAnkle);
            UpdateJoint(bodyPose, bodyPose.RightHip, RightHip);
            UpdateJoint(bodyPose, bodyPose.RightKnee, RightKnee);
            UpdateJoint(bodyPose, bodyPose.RightAnkle, RightAnkle);
        }

        private void UpdateJoint(BodyPose bodyPose, [CanBeNull] BodyPose.Joint joint, GameObject jointGo) {
            if (joint == null) {
                // Debug.Log("Joint is null");
                jointGo.SetActive(false);
            } else {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    bodyPose.ToScreenCoordinates(joint),
                    screenCamera,
                    out var pos
                );

                jointGo.transform.localPosition = pos;
                jointGo.SetActive(true);
            }
        }
    }
}
