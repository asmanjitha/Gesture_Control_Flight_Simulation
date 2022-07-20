/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using CoachAiEngine.Analytics.Pose;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace CoachAiEngine.Animation.AvatarAnimation {

    [AddComponentMenu("Coach-AI Engine/Animator/Body Pose/Avatar Animator")]
    [RequireComponent(typeof(Animator))]
    public class AvatarAnimator : PoseAnimator {
        protected Animator animator;

        public GameObject cylinderObject;
        //private BodyPose bodyPose; // input data representation
        private TPose tPose = new TPose();
        [SerializeField]private bool useAdditionalRotation = false;
        [SerializeField]private Quaternion preRotation;
        private BoneRotations initialBoneLocalRotations;
        private BoneRotations currentBoneGlobalRotations;
        private BoneRotations boneRotations;

        private AvatarPoseObject _debugPose = null;

        // debug switches
        [Tooltip("Display debug lines")]
        [SerializeField] private bool showRawPose;
        [Tooltip("Enable root motion")]
        [SerializeField] private bool rootMotion;
        [Tooltip("Flip IK around Y axis.")]
        [SerializeField] private bool flip;
        [Tooltip("Modify for spawn object around map")]
        [SerializeField] private Vector3 Offset = Vector3.zero;
        [Tooltip("Modify for spawn debug object around map")]
        [SerializeField] private Vector3 DebugOffset = Vector3.zero;
        [Tooltip("Root for debug pose")]
        [SerializeField] private GameObject rootObject = null;

        private AvatarPose _currentAvatarPose = null;


        public void SetRootMotion(bool _rootMotion) {
            rootMotion = _rootMotion;
            animator.ApplyBuiltinRootMotion();

        }

        public void SetDebugPose(bool isDebugPose) {
            showRawPose = isDebugPose;
        }

        public void SetDebugPoseOffset(Vector3 posePosition) {
            DebugOffset = posePosition;
        }

        public bool GetRootMotion() {
            return rootMotion;
        }

        private void Start() {
            animator = GetComponent<Animator>();

            if (animator) {
                //Debug.Log(Utils.AvatarToString(animator.avatar));

                initialBoneLocalRotations = Utils.CreateInitialLocalRotations(animator);
                currentBoneGlobalRotations = Utils.CreateInitialGlobalRotations(animator);
                boneRotations.Init();
            }
        }

        void OnAnimatorMove() {
            if (animator && _currentAvatarPose != null) {
                string sLeft = "left";
                string sRight = "right";
                if (flip) {
                    sLeft = "right";
                    sRight = "left";
                }

                Vector3 vLeftHip = _currentAvatarPose.GetJoint(sLeft + " hip");
                Vector3 vRightHip = _currentAvatarPose.GetJoint(sRight + " hip");

                animator.rootPosition = (vLeftHip + vRightHip) / 2f + Offset;
                if (rootMotion) {
                    animator.ApplyBuiltinRootMotion();
                }
            }
        }


        void OnAnimatorIK() {
            if (animator && _currentAvatarPose != null) {
                string sLeft = "left";
                string sRight = "right";
                if (flip) {
                    sLeft = "right";
                    sRight = "left";
                }

                Vector3 vLeftShoulder = _currentAvatarPose.GetJoint(sLeft + " shoulder");
                Vector3 vRightShoulder = _currentAvatarPose.GetJoint(sRight + " shoulder");
                Vector3 vLeftHip = _currentAvatarPose.GetJoint(sLeft + " hip");
                Vector3 vRightHip = _currentAvatarPose.GetJoint(sRight + " hip");
                Vector3 vLeftKnee = _currentAvatarPose.GetJoint(sLeft + " knee");
                Vector3 vRightKnee = _currentAvatarPose.GetJoint(sRight + " knee");
                Vector3 vLeftAnkle = _currentAvatarPose.GetJoint(sLeft + " ankle");
                Vector3 vRightAnkle = _currentAvatarPose.GetJoint(sRight + " ankle");
                Vector3 vLeftElbow = _currentAvatarPose.GetJoint(sLeft + " elbow");
                Vector3 vRightElbow = _currentAvatarPose.GetJoint(sRight + " elbow");
                Vector3 vLeftWrist = _currentAvatarPose.GetJoint(sLeft + " wrist");
                Vector3 vRightWrist = _currentAvatarPose.GetJoint(sRight + " wrist");

                Vector3 vRoot = (vLeftHip + vRightHip) / 2.0f;
                Vector3 vNeck = (vLeftShoulder + vRightShoulder) / 2.0f;

                JointNode rootNode = new JointNode("root", vRoot, tPose.vRootLeftHip);
                JointNode rootMirrorNode = new JointNode("root mirror", vRoot, tPose.vHipNeck);
                JointNode leftHip = new JointNode("left hip", vLeftHip, tPose.vLeftHipLeftLeg);
                JointNode rightHip = new JointNode("right hip", vRightHip, tPose.vRightHipRightLeg);
                JointNode leftKnee = new JointNode("left knee", vLeftKnee, tPose.vLeftLegLeftFoot);
                JointNode rightKnee = new JointNode("right knee", vRightKnee, tPose.vRightLegRightFoot);
                JointNode leftAnkle = new JointNode("left ankle", vLeftAnkle, Vector3.zero);
                JointNode rightAnkle = new JointNode("right ankle", vRightAnkle, Vector3.zero);
                JointNode neckNode = new JointNode("neck", vNeck, tPose.vNeckLeftShoulder);
                JointNode leftShoulder = new JointNode("left shoulder", vLeftShoulder, tPose.vLeftShoulderLeftElbow);
                JointNode rightShoulder = new JointNode("right shoulder", vRightShoulder, tPose.vRightShoulderRightElbow);
                JointNode leftElbow = new JointNode("left elbow", vLeftElbow, tPose.vLeftElbowArmLeftWrist);
                JointNode rightElbow = new JointNode("right elbow", vRightElbow, tPose.vRightElbowRightWrist);
                JointNode leftWrist = new JointNode("left wrist", vLeftWrist, Vector3.zero);
                JointNode rightWrist = new JointNode("right wrist", vRightWrist, Vector3.zero);
                rootNode.AddChild(leftHip);
                rootNode.AddChild(rightHip);
                rootNode.AddChild(rootMirrorNode);
                leftHip.AddChild(leftKnee);
                rightHip.AddChild(rightKnee);
                leftKnee.AddChild(leftAnkle);
                rightKnee.AddChild(rightAnkle);
                rootMirrorNode.AddChild(neckNode);
                neckNode.AddChild(leftShoulder);
                neckNode.AddChild(rightShoulder);
                leftShoulder.AddChild(leftElbow);
                rightShoulder.AddChild(rightElbow);
                leftElbow.AddChild(leftWrist);
                rightElbow.AddChild(rightWrist);
                rootNode.ComputeNodeLocals();

                // hip
                boneRotations.Hips = rootNode.GetRotation();

                // legs
                Vector3 vLeftHipLeftLeg = vLeftKnee - vLeftHip;
                Vector3 vLeftLegLeftFoot = vLeftAnkle - vLeftKnee;
                Vector3 vRightHipRightLeg = vRightKnee - vRightHip;
                Vector3 vRightLegRightFoot = vRightAnkle - vRightKnee;

                if (vLeftKnee != Vector3.zero) {
                    boneRotations.LeftUpperLeg = Quaternion.Inverse(currentBoneGlobalRotations.Hips) *
                                                 leftHip.GetRotation() *
                                                 currentBoneGlobalRotations.Hips;
                    if (vLeftAnkle != Vector3.zero) {
                        boneRotations.LeftLowerLeg = Quaternion.Inverse(currentBoneGlobalRotations.LeftUpperLeg) *
                                                     leftKnee.GetRotation() *
                                                     currentBoneGlobalRotations.LeftUpperLeg;
                    }
                }

                if (vRightKnee != Vector3.zero) {
                    boneRotations.RightUpperLeg = Quaternion.Inverse(currentBoneGlobalRotations.Hips) *
                                                  rightHip.GetRotation() *
                                                  currentBoneGlobalRotations.Hips;
                    if (vRightAnkle != Vector3.zero) {
                        boneRotations.RightLowerLeg = Quaternion.Inverse(currentBoneGlobalRotations.RightUpperLeg) *
                                                      rightKnee.GetRotation() *
                                                      currentBoneGlobalRotations.RightUpperLeg;
                    }
                }

                // shoulder and spine
                //Vector3 vRootPos = MyAnimator.Utils.GetLocalPositionFromAvatar(animator.avatar, "Root");
                Vector3 vSpinePos = Utils.GetLocalPositionFromAvatar(animator.avatar, "Spine");
                Vector3 vChestPos = Utils.GetLocalPositionFromAvatar(animator.avatar, "Chest");
                float fSpineWeight = vSpinePos.y / (vSpinePos.y + vChestPos.y);
                float fChestWeight = vChestPos.y / (vSpinePos.y + vChestPos.y);

                Vector3 vLeftShoulderRightShoulder = (vRightShoulder - vLeftShoulder).normalized;
                boneRotations.Spine = Quaternion.Slerp(Quaternion.identity, rootMirrorNode.GetRotation(), fSpineWeight);
                boneRotations.Chest = Quaternion.Slerp(Quaternion.identity, rootMirrorNode.GetRotation(), fChestWeight);

                // arms
                boneRotations.LeftUpperArm = Quaternion.Inverse(currentBoneGlobalRotations.LeftShoulder) *
                                             leftShoulder.GetRotation() *
                                             currentBoneGlobalRotations.LeftShoulder;
                boneRotations.LeftLowerArm = Quaternion.Inverse(currentBoneGlobalRotations.LeftUpperArm) *
                                             leftElbow.GetRotation() *
                                             currentBoneGlobalRotations.LeftUpperArm;

                boneRotations.RightUpperArm = Quaternion.Inverse(currentBoneGlobalRotations.RightShoulder) *
                                              rightShoulder.GetRotation() *
                                              currentBoneGlobalRotations.RightShoulder;
                boneRotations.RightLowerArm = Quaternion.Inverse(currentBoneGlobalRotations.RightUpperArm) *
                                              rightElbow.GetRotation() *
                                              currentBoneGlobalRotations.RightUpperArm;
            }

            if (animator) {
                //animator.SetBoneLocalRotation(HumanBodyBones.Hips, boneRotations.Hips * initialBoneLocalRotations.Hips);
                if(useAdditionalRotation) {
                    animator.SetBoneLocalRotation(HumanBodyBones.Hips, preRotation * boneRotations.Hips);
                } else {
                    animator.SetBoneLocalRotation(HumanBodyBones.Hips, boneRotations.Hips);
                }
                animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg,
                    boneRotations.LeftUpperLeg * initialBoneLocalRotations.LeftUpperLeg);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg,
                    boneRotations.LeftLowerLeg * initialBoneLocalRotations.LeftLowerLeg);
                animator.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg,
                    boneRotations.RightUpperLeg * initialBoneLocalRotations.RightUpperLeg);
                animator.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg,
                    boneRotations.RightLowerLeg * initialBoneLocalRotations.RightLowerLeg);
                animator.SetBoneLocalRotation(HumanBodyBones.Spine,
                    boneRotations.Spine * initialBoneLocalRotations.Spine);
                animator.SetBoneLocalRotation(HumanBodyBones.Chest,
                    boneRotations.Chest * initialBoneLocalRotations.Chest);

                // arms and shoulder
                animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm,
                    initialBoneLocalRotations.LeftUpperArm * boneRotations.LeftUpperArm);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm,
                    initialBoneLocalRotations.LeftLowerArm * boneRotations.LeftLowerArm);
                animator.SetBoneLocalRotation(HumanBodyBones.RightUpperArm,
                    initialBoneLocalRotations.RightUpperArm * boneRotations.RightUpperArm);
                animator.SetBoneLocalRotation(HumanBodyBones.RightLowerArm,
                    initialBoneLocalRotations.RightLowerArm * boneRotations.RightLowerArm);

                // hands and fingers
                animator.SetBoneLocalRotation(HumanBodyBones.LeftHand, initialBoneLocalRotations.LeftHand);
                animator.SetBoneLocalRotation(HumanBodyBones.RightHand, initialBoneLocalRotations.RightHand);

                animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbProximal,
                    initialBoneLocalRotations.LeftThumbProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbIntermediate,
                    initialBoneLocalRotations.LeftThumbIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftThumbDistal,
                    initialBoneLocalRotations.LeftThumbDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexProximal,
                    initialBoneLocalRotations.LeftThumbProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexIntermediate,
                    initialBoneLocalRotations.LeftThumbIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftIndexDistal,
                    initialBoneLocalRotations.LeftThumbDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleProximal,
                    initialBoneLocalRotations.LeftMiddleProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleIntermediate,
                    initialBoneLocalRotations.LeftMiddleIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftMiddleDistal,
                    initialBoneLocalRotations.LeftMiddleDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftRingProximal,
                    initialBoneLocalRotations.LeftRingProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftRingIntermediate,
                    initialBoneLocalRotations.LeftRingIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftRingDistal, initialBoneLocalRotations.LeftRingDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleProximal,
                    initialBoneLocalRotations.LeftLittleProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleIntermediate,
                    initialBoneLocalRotations.LeftLittleIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.LeftLittleDistal,
                    initialBoneLocalRotations.LeftLittleDistal);

                animator.SetBoneLocalRotation(HumanBodyBones.RightThumbProximal,
                    initialBoneLocalRotations.RightThumbProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightThumbIntermediate,
                    initialBoneLocalRotations.RightThumbIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.RightThumbDistal,
                    initialBoneLocalRotations.RightThumbDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightIndexProximal,
                    initialBoneLocalRotations.RightIndexProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightIndexIntermediate,
                    initialBoneLocalRotations.RightIndexIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.RightIndexDistal,
                    initialBoneLocalRotations.RightIndexDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleProximal,
                    initialBoneLocalRotations.RightMiddleProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleIntermediate,
                    initialBoneLocalRotations.RightMiddleIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.RightMiddleDistal,
                    initialBoneLocalRotations.RightMiddleDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightRingProximal,
                    initialBoneLocalRotations.RightRingProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightRingIntermediate,
                    initialBoneLocalRotations.RightRingIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.RightRingDistal,
                    initialBoneLocalRotations.RightRingDistal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightLittleProximal,
                    initialBoneLocalRotations.RightLittleProximal);
                animator.SetBoneLocalRotation(HumanBodyBones.RightLittleIntermediate,
                    initialBoneLocalRotations.RightLittleIntermediate);
                animator.SetBoneLocalRotation(HumanBodyBones.RightLittleDistal,
                    initialBoneLocalRotations.RightLittleDistal);
            }
        }

        public override void UpdatePose(BodyPose _bodyPose) {
            foreach (var joint in _bodyPose.Joints) {
                joint.Location = Quaternion.Euler(0, 0, 90) * joint.Location;
            }
            if (_currentAvatarPose == null) {
                _currentAvatarPose = createAvatarPoseFromBodyPose(_bodyPose);
            } else {
                convertBodyPoseToAvatarPose(_currentAvatarPose, _bodyPose);
            }
            if(showRawPose) {
                Assert.IsFalse(_currentAvatarPose == null);
                if (_debugPose == null) {
                    _debugPose = createDebugPoseFromAvatarPose(_currentAvatarPose);
                } else {
                    updateDebugPoseFromAvatarPose(_currentAvatarPose);
                }
            }
        }

        public override void UpdatePose(float[] jointArray) {
            List<Vector3> jointList = new List<Vector3>();
            int jointIndex = 0;
            for (int i = 0; i < (jointArray.Length / 3); i++) {
                Vector3 jointPositionVector = new Vector3(
                    jointArray[jointIndex],
                    jointArray[jointIndex + 1],
                    jointArray[jointIndex + 2]);
                jointList.Add(jointPositionVector);
                jointIndex += 3;
            }
            if (_currentAvatarPose == null) {
                _currentAvatarPose = createAvatarPoseFromJointArray(jointList);
            } else {
                convertJointArrayToAvatarPose(_currentAvatarPose, jointList);
            }
        }

        private void convertJointArrayToAvatarPose(AvatarPose _avatarPose, List<Vector3> jointList) {
            try {
                _avatarPose.UpdateJoint("central nose", jointList[0], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left shoulder", jointList[1], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right shoulder", jointList[2], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left elbow", jointList[3], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right elbow", jointList[4], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left wrist", jointList[5], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right wrist", jointList[6], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left hip", jointList[7], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right hip", jointList[8], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left knee", jointList[9], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right knee", jointList[10], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left ankle", jointList[11], 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right ankle", jointList[12], 1f);
            } catch (NullReferenceException) {}
        }

        private void convertBodyPoseToAvatarPose(AvatarPose _avatarPose, BodyPose _bodyPose) {
            try {
                _avatarPose.UpdateJoint("central nose", _bodyPose.Nose.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left shoulder", _bodyPose.LeftShoulder.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right shoulder", _bodyPose.RightShoulder.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left elbow", _bodyPose.LeftElbow.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right elbow", _bodyPose.RightElbow.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left wrist", _bodyPose.LeftWrist.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right wrist", _bodyPose.RightWrist.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left hip", _bodyPose.LeftHip.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right hip", _bodyPose.RightHip.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left knee", _bodyPose.LeftKnee.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right knee", _bodyPose.RightKnee.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("left ankle", _bodyPose.LeftAnkle.Location, 1f);
            } catch (NullReferenceException) {}

            try {
                _avatarPose.UpdateJoint("right ankle", _bodyPose.RightAnkle.Location, 1f);
            } catch (NullReferenceException) {}
        }

        private AvatarPose createAvatarPoseFromBodyPose(BodyPose _bodyPose) {
            AvatarPose _avatarPose = new AvatarPose();
            try {
                _avatarPose.AddJoint("central nose", _bodyPose.Nose.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("central nose", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left shoulder", _bodyPose.LeftShoulder.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left shoulder", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right shoulder", _bodyPose.RightShoulder.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right shoulder", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left elbow", _bodyPose.LeftElbow.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left elbow", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right elbow", _bodyPose.RightElbow.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right elbow", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left wrist", _bodyPose.LeftWrist.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left wrist", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right wrist", _bodyPose.RightWrist.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right wrist", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left hip", _bodyPose.LeftHip.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left hip", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right hip", _bodyPose.RightHip.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right hip", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left knee", _bodyPose.LeftKnee.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left knee", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right knee", _bodyPose.RightKnee.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right knee", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left ankle", _bodyPose.LeftAnkle.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left ankle", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right ankle", _bodyPose.RightAnkle.Location, 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right ankle", Vector3.zero, 0f);
            }

            return _avatarPose;
        }

        private AvatarPose createAvatarPoseFromJointArray(List<Vector3> jointList) {
            AvatarPose _avatarPose = new AvatarPose();

            try {
                _avatarPose.AddJoint("central nose", jointList[0], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("central nose", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left shoulder", jointList[1], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left shoulder", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right shoulder", jointList[2], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right shoulder", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left elbow", jointList[3], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left elbow", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right elbow", jointList[4], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right elbow", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left wrist", jointList[5], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left wrist", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right wrist", jointList[6], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right wrist", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left hip", jointList[7], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left hip", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right hip", jointList[8], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right hip", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left knee", jointList[9], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left knee", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right knee", jointList[10], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right knee", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("left ankle", jointList[11], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("left ankle", Vector3.zero, 0f);
            }

            try {
                _avatarPose.AddJoint("right ankle", jointList[12], 1f);
            } catch (NullReferenceException) {
                _avatarPose.AddJoint("right ankle", Vector3.zero, 0f);
            }

            return _avatarPose;
        }

        private AvatarPoseObject createDebugPoseFromAvatarPose(AvatarPose _avatarPose) {
            AvatarPoseObject debugPose = new AvatarPoseObject(rootObject);

            string sLeft = "left";
            string sRight = "right";
            if (flip) {
                sLeft = "right";
                sRight = "left";
            }
            Vector3 vCentralNose = _currentAvatarPose.GetJoint("central nose");
            Vector3 vLeftShoulder = _currentAvatarPose.GetJoint(sLeft + " shoulder");
            Vector3 vRightShoulder = _currentAvatarPose.GetJoint(sRight + " shoulder");
            Vector3 vLeftHip = _currentAvatarPose.GetJoint(sLeft + " hip");
            Vector3 vRightHip = _currentAvatarPose.GetJoint(sRight + " hip");
            Vector3 vLeftKnee = _currentAvatarPose.GetJoint(sLeft + " knee");
            Vector3 vRightKnee = _currentAvatarPose.GetJoint(sRight + " knee");
            Vector3 vLeftAnkle = _currentAvatarPose.GetJoint(sLeft + " ankle");
            Vector3 vRightAnkle = _currentAvatarPose.GetJoint(sRight + " ankle");

            Vector3 vLeftElbow = _currentAvatarPose.GetJoint(sLeft + " elbow");
            Vector3 vRightElbow = _currentAvatarPose.GetJoint(sRight + " elbow");
            Vector3 vLeftWrist = _currentAvatarPose.GetJoint(sLeft + " wrist");
            Vector3 vRightWrist = _currentAvatarPose.GetJoint(sRight + " wrist");

            debugPose.AddJoint("central nose", vCentralNose + DebugOffset);
            debugPose.AddJoint("left shoulder", vLeftShoulder + DebugOffset);
            debugPose.AddJoint("right shoulder", vRightShoulder + DebugOffset);
            debugPose.AddJoint("left hip", vLeftHip + DebugOffset);
            debugPose.AddJoint("right hip", vRightHip + DebugOffset);
            debugPose.AddJoint("left knee", vLeftKnee + DebugOffset);
            debugPose.AddJoint("right knee", vRightKnee + DebugOffset);
            debugPose.AddJoint("left ankle", vLeftAnkle + DebugOffset);
            debugPose.AddJoint("right ankle", vRightAnkle + DebugOffset);
            debugPose.AddJoint("left elbow", vLeftElbow + DebugOffset);
            debugPose.AddJoint("right elbow", vRightElbow + DebugOffset);
            debugPose.AddJoint("left wrist", vLeftWrist + DebugOffset);
            debugPose.AddJoint("right wrist", vRightWrist + DebugOffset);
            debugPose.UpdateConnections();

            return debugPose;
        }

        private void updateDebugPoseFromAvatarPose(AvatarPose _avatarPose) {
            Assert.IsFalse(_debugPose == null);

            string sLeft = "left";
            string sRight = "right";
            if (flip) {
                sLeft = "right";
                sRight = "left";
            }
            Vector3 vCentralNose = _currentAvatarPose.GetJoint("central nose");
            Vector3 vLeftShoulder = _currentAvatarPose.GetJoint(sLeft + " shoulder");
            Vector3 vRightShoulder = _currentAvatarPose.GetJoint(sRight + " shoulder");
            Vector3 vLeftHip = _currentAvatarPose.GetJoint(sLeft + " hip");
            Vector3 vRightHip = _currentAvatarPose.GetJoint(sRight + " hip");
            Vector3 vLeftKnee = _currentAvatarPose.GetJoint(sLeft + " knee");
            Vector3 vRightKnee = _currentAvatarPose.GetJoint(sRight + " knee");
            Vector3 vLeftAnkle = _currentAvatarPose.GetJoint(sLeft + " ankle");
            Vector3 vRightAnkle = _currentAvatarPose.GetJoint(sRight + " ankle");

            Vector3 vLeftElbow = _currentAvatarPose.GetJoint(sLeft + " elbow");
            Vector3 vRightElbow = _currentAvatarPose.GetJoint(sRight + " elbow");
            Vector3 vLeftWrist = _currentAvatarPose.GetJoint(sLeft + " wrist");
            Vector3 vRightWrist = _currentAvatarPose.GetJoint(sRight + " wrist");

            _debugPose.UpdateJoint("central nose", vCentralNose + DebugOffset);
            _debugPose.UpdateJoint("left shoulder", vLeftShoulder + DebugOffset);
            _debugPose.UpdateJoint("right shoulder", vRightShoulder + DebugOffset);
            _debugPose.UpdateJoint("left hip", vLeftHip + DebugOffset);
            _debugPose.UpdateJoint("right hip", vRightHip + DebugOffset);
            _debugPose.UpdateJoint("left knee", vLeftKnee + DebugOffset);
            _debugPose.UpdateJoint("right knee", vRightKnee + DebugOffset);
            _debugPose.UpdateJoint("left ankle", vLeftAnkle + DebugOffset);
            _debugPose.UpdateJoint("right ankle", vRightAnkle + DebugOffset);
            _debugPose.UpdateJoint("left elbow", vLeftElbow + DebugOffset);
            _debugPose.UpdateJoint("right elbow", vRightElbow + DebugOffset);
            _debugPose.UpdateJoint("left wrist", vLeftWrist + DebugOffset);
            _debugPose.UpdateJoint("right wrist", vRightWrist + DebugOffset);
            _debugPose.UpdateConnections();
        }
    }

}
