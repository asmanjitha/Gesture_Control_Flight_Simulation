/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Collections.Generic;
using UnityEngine;

namespace CoachAiEngine.Animation.AvatarAnimation {

    public enum Bones {
        Hips = 0,
        LeftUpperLeg = 1,
        RightUpperLeg = 2,
        LeftLowerLeg = 3,
        RightLowerLeg = 4,
        LeftFoot = 5,
        RightFoot = 6,
        Spine = 7,
        Chest = 8,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftUpperArm = 13,
        RightUpperArm = 14,
        LeftLowerArm = 15,
        RightLowerArm = 16,
        LeftHand = 17,
        RightHand = 18,
        LeftThumbProximal = 24,
        LeftThumbIntermediate = 25,
        LeftThumbDistal = 26,
        LeftIndexProximal = 27,
        LeftIndexIntermediate = 28,
        LeftIndexDistal = 29,
        LeftMiddleProximal = 30,
        LeftMiddleIntermediate = 31,
        LeftMiddleDistal = 32,
        LeftRingProximal = 33,
        LeftRingIntermediate = 34,
        LeftRingDistal = 35,
        LeftLittleProximal = 36,
        LeftLittleIntermediate = 37,
        LeftLittleDistal = 38,
        RightThumbProximal = 39,
        RightThumbIntermediate = 40,
        RightThumbDistal = 41,
        RightIndexProximal = 42,
        RightIndexIntermediate = 43,
        RightIndexDistal = 44,
        RightMiddleProximal = 45,
        RightMiddleIntermediate = 46,
        RightMiddleDistal = 47,
        RightRingProximal = 48,
        RightRingIntermediate = 49,
        RightRingDistal = 50,
        RightLittleProximal = 51,
        RightLittleIntermediate = 52,
        RightLittleDistal = 53,
        LastBone = 55
    }

    class TPose {
        public TPose() {
            // we assume, that limbs from t-pose are axis aligned
            // lower body
            vLeftHipRightHip = Vector3.right;
            vRootLeftHip = Vector3.left;
            vRootRightHip = Vector3.right;
            vLeftHipLeftLeg = Vector3.down;
            vLeftLegLeftFoot = Vector3.down;
            vRightHipRightLeg = Vector3.down;
            vRightLegRightFoot = Vector3.down;

            // upper body
            vHipNeck = Vector3.up;
            vNeckLeftShoulder = Vector3.left;
            vNeckRightShoulder = Vector3.right;
            vLeftHipLeftShoulder = Vector3.up;
            vRightHipRightShoulder = Vector3.up;
            vLeftShoulderRightShoulder = Vector3.right;
            vLeftShoulderLeftElbow = Vector3.left;
            vRightShoulderRightElbow = Vector3.right;
            vLeftElbowArmLeftWrist = Vector3.left;
            vRightElbowRightWrist = Vector3.right;

            bLeftKneeRotationEulerMask = new bool[] {true, false, false};
            bRightKneeRotationEulerMask = new bool[] {true, false, false};
        }

        public Vector3 vLeftHipRightHip { get; }
        public Vector3 vRootLeftHip { get; }
        public Vector3 vRootRightHip { get; }
        public Vector3 vLeftHipLeftLeg { get; }
        public Vector3 vLeftLegLeftFoot { get; }
        public Vector3 vRightHipRightLeg { get; }
        public Vector3 vRightLegRightFoot { get; }
        public Vector3 vHipNeck { get; }
        public Vector3 vNeckLeftShoulder { get; }
        public Vector3 vNeckRightShoulder { get; }
        public Vector3 vLeftHipLeftShoulder { get; }
        public Vector3 vRightHipRightShoulder { get; }
        public Vector3 vLeftShoulderRightShoulder { get; }
        public Vector3 vLeftShoulderLeftElbow { get; }
        public Vector3 vRightShoulderRightElbow { get; }
        public Vector3 vLeftElbowArmLeftWrist { get; }
        public Vector3 vRightElbowRightWrist { get; }

        public bool[] bLeftKneeRotationEulerMask { get; }
        public bool[] bRightKneeRotationEulerMask { get; }
    }

    public struct BoneRotations {
        public void Init() {
            Hips = Quaternion.Euler(0, 0, 0);
            Spine = Quaternion.Euler(0, 0, 0);
            Chest = Quaternion.Euler(0, 0, 0);
            LeftUpperLeg = Quaternion.Euler(0, 0, 0);
            RightUpperLeg = Quaternion.Euler(0, 0, 0);
            LeftLowerLeg = Quaternion.Euler(0, 0, 0);
            RightLowerLeg = Quaternion.Euler(0, 0, 0);
            LeftShoulder = Quaternion.Euler(0, 0, 0);
            RightShoulder = Quaternion.Euler(0, 0, 0);
            LeftUpperArm = Quaternion.Euler(0, 0, 0);
            RightUpperArm = Quaternion.Euler(0, 0, 0);
            LeftLowerArm = Quaternion.Euler(0, 0, 0);
            RightLowerArm = Quaternion.Euler(0, 0, 0);
            LeftHand = Quaternion.Euler(0, 0, 0);
            RightHand = Quaternion.Euler(0, 0, 0);
            LeftThumbProximal = Quaternion.Euler(0, 0, 0);
            LeftThumbIntermediate = Quaternion.Euler(0, 0, 0);
            LeftThumbDistal = Quaternion.Euler(0, 0, 0);
            LeftIndexProximal = Quaternion.Euler(0, 0, 0);
            LeftIndexIntermediate = Quaternion.Euler(0, 0, 0);
            LeftIndexDistal = Quaternion.Euler(0, 0, 0);
            LeftMiddleProximal = Quaternion.Euler(0, 0, 0);
            LeftMiddleIntermediate = Quaternion.Euler(0, 0, 0);
            LeftMiddleDistal = Quaternion.Euler(0, 0, 0);
            LeftRingProximal = Quaternion.Euler(0, 0, 0);
            LeftRingIntermediate = Quaternion.Euler(0, 0, 0);
            LeftRingDistal = Quaternion.Euler(0, 0, 0);
            LeftLittleProximal = Quaternion.Euler(0, 0, 0);
            LeftLittleIntermediate = Quaternion.Euler(0, 0, 0);
            LeftLittleDistal = Quaternion.Euler(0, 0, 0);
            RightThumbProximal = Quaternion.Euler(0, 0, 0);
            RightThumbIntermediate = Quaternion.Euler(0, 0, 0);
            RightThumbDistal = Quaternion.Euler(0, 0, 0);
            RightIndexProximal = Quaternion.Euler(0, 0, 0);
            RightIndexIntermediate = Quaternion.Euler(0, 0, 0);
            RightIndexDistal = Quaternion.Euler(0, 0, 0);
            RightMiddleProximal = Quaternion.Euler(0, 0, 0);
            RightMiddleIntermediate = Quaternion.Euler(0, 0, 0);
            RightMiddleDistal = Quaternion.Euler(0, 0, 0);
            RightRingProximal = Quaternion.Euler(0, 0, 0);
            RightRingIntermediate = Quaternion.Euler(0, 0, 0);
            RightRingDistal = Quaternion.Euler(0, 0, 0);
            RightLittleProximal = Quaternion.Euler(0, 0, 0);
            RightLittleIntermediate = Quaternion.Euler(0, 0, 0);
            RightLittleDistal = Quaternion.Euler(0, 0, 0);
        }

        public Quaternion Hips { get; set; }
        public Quaternion Spine { get; set; }
        public Quaternion Chest { get; set; }
        public Quaternion LeftUpperLeg { get; set; }
        public Quaternion RightUpperLeg { get; set; }
        public Quaternion LeftLowerLeg { get; set; }
        public Quaternion RightLowerLeg { get; set; }
        public Quaternion LeftShoulder { get; set; }
        public Quaternion RightShoulder { get; set; }
        public Quaternion LeftUpperArm { get; set; }
        public Quaternion RightUpperArm { get; set; }
        public Quaternion LeftLowerArm { get; set; }
        public Quaternion RightLowerArm { get; set; }
        public Quaternion LeftHand { get; set; }
        public Quaternion RightHand { get; set; }
        public Quaternion LeftThumbProximal { get; set; }
        public Quaternion LeftThumbIntermediate { get; set; }
        public Quaternion LeftThumbDistal { get; set; }
        public Quaternion LeftIndexProximal { get; set; }
        public Quaternion LeftIndexIntermediate { get; set; }
        public Quaternion LeftIndexDistal { get; set; }
        public Quaternion LeftMiddleProximal { get; set; }
        public Quaternion LeftMiddleIntermediate { get; set; }
        public Quaternion LeftMiddleDistal { get; set; }
        public Quaternion LeftRingProximal { get; set; }
        public Quaternion LeftRingIntermediate { get; set; }
        public Quaternion LeftRingDistal { get; set; }
        public Quaternion LeftLittleProximal { get; set; }
        public Quaternion LeftLittleIntermediate { get; set; }
        public Quaternion LeftLittleDistal { get; set; }
        public Quaternion RightThumbProximal { get; set; }
        public Quaternion RightThumbIntermediate { get; set; }
        public Quaternion RightThumbDistal { get; set; }
        public Quaternion RightIndexProximal { get; set; }
        public Quaternion RightIndexIntermediate { get; set; }
        public Quaternion RightIndexDistal { get; set; }
        public Quaternion RightMiddleProximal { get; set; }
        public Quaternion RightMiddleIntermediate { get; set; }
        public Quaternion RightMiddleDistal { get; set; }
        public Quaternion RightRingProximal { get; set; }
        public Quaternion RightRingIntermediate { get; set; }
        public Quaternion RightRingDistal { get; set; }
        public Quaternion RightLittleProximal { get; set; }
        public Quaternion RightLittleIntermediate { get; set; }
        public Quaternion RightLittleDistal { get; set; }
    }

    class Utils {
        public static BoneRotations CreateInitialLocalRotations(Avatar avatar) {
            BoneRotations initialRotations = new BoneRotations();

            initialRotations.Hips = GetRotationFromAvatar(avatar, "Hips");
            initialRotations.Spine = GetRotationFromAvatar(avatar, "Spine");
            initialRotations.Chest = GetRotationFromAvatar(avatar, "Chest");
            initialRotations.LeftUpperLeg = GetRotationFromAvatar(avatar, "LeftUpperLeg");
            initialRotations.RightUpperLeg = GetRotationFromAvatar(avatar, "RightUpperLeg");
            initialRotations.LeftLowerLeg = GetRotationFromAvatar(avatar, "LeftLowerLeg");
            initialRotations.RightLowerLeg = GetRotationFromAvatar(avatar, "RightLowerLeg");
            initialRotations.LeftShoulder = GetRotationFromAvatar(avatar, "LeftShoulder");
            initialRotations.RightShoulder = GetRotationFromAvatar(avatar, "RightShoulder");
            initialRotations.LeftUpperArm = GetRotationFromAvatar(avatar, "LeftUpperArm");
            initialRotations.RightUpperArm = GetRotationFromAvatar(avatar, "RightUpperArm");
            initialRotations.LeftLowerArm = GetRotationFromAvatar(avatar, "LeftLowerArm");
            initialRotations.RightLowerArm = GetRotationFromAvatar(avatar, "RightLowerArm");
            initialRotations.LeftHand = GetRotationFromAvatar(avatar, "LeftHand");
            initialRotations.RightHand = GetRotationFromAvatar(avatar, "RightHand");
            initialRotations.LeftThumbProximal = GetRotationFromAvatar(avatar, "LeftThumbProximal");
            initialRotations.LeftThumbIntermediate = GetRotationFromAvatar(avatar, "LeftThumbIntermediate");
            initialRotations.LeftThumbDistal = GetRotationFromAvatar(avatar, "LeftThumbDistal");
            initialRotations.LeftIndexProximal = GetRotationFromAvatar(avatar, "LeftIndexProximal");
            initialRotations.LeftIndexIntermediate = GetRotationFromAvatar(avatar, "LeftIndexIntermediate");
            initialRotations.LeftIndexDistal = GetRotationFromAvatar(avatar, "LeftIndexDistal");
            initialRotations.LeftMiddleProximal = GetRotationFromAvatar(avatar, "LeftMiddleProximal");
            initialRotations.LeftMiddleIntermediate = GetRotationFromAvatar(avatar, "LeftMiddleIntermediate");
            initialRotations.LeftMiddleDistal = GetRotationFromAvatar(avatar, "LeftMiddleDistal");
            initialRotations.LeftRingProximal = GetRotationFromAvatar(avatar, "LeftRingProximal");
            initialRotations.LeftRingIntermediate = GetRotationFromAvatar(avatar, "LeftRingIntermediate");
            initialRotations.LeftRingDistal = GetRotationFromAvatar(avatar, "LeftRingDistal");
            initialRotations.LeftLittleProximal = GetRotationFromAvatar(avatar, "LeftLittleProximal");
            initialRotations.LeftLittleIntermediate = GetRotationFromAvatar(avatar, "LeftLittleIntermediate");
            initialRotations.LeftLittleDistal = GetRotationFromAvatar(avatar, "LeftLittleDistal");
            initialRotations.RightThumbProximal = GetRotationFromAvatar(avatar, "RightThumbProximal");
            initialRotations.RightThumbIntermediate = GetRotationFromAvatar(avatar, "RightThumbIntermediate");
            initialRotations.RightThumbDistal = GetRotationFromAvatar(avatar, "RightThumbDistal");
            initialRotations.RightIndexProximal = GetRotationFromAvatar(avatar, "RightIndexProximal");
            initialRotations.RightIndexIntermediate = GetRotationFromAvatar(avatar, "RightIndexIntermediate");
            initialRotations.RightIndexDistal = GetRotationFromAvatar(avatar, "RightIndexDistal");
            initialRotations.RightMiddleProximal = GetRotationFromAvatar(avatar, "RightMiddleProximal");
            initialRotations.RightMiddleIntermediate = GetRotationFromAvatar(avatar, "RightMiddleIntermediate");
            initialRotations.RightMiddleDistal = GetRotationFromAvatar(avatar, "RightMiddleDistal");
            initialRotations.RightRingProximal = GetRotationFromAvatar(avatar, "RightRingProximal");
            initialRotations.RightRingIntermediate = GetRotationFromAvatar(avatar, "RightRingIntermediate");
            initialRotations.RightRingDistal = GetRotationFromAvatar(avatar, "RightRingDistal");
            initialRotations.RightLittleProximal = GetRotationFromAvatar(avatar, "RightLittleProximal");
            initialRotations.RightLittleIntermediate = GetRotationFromAvatar(avatar, "RightLittleIntermediate");
            initialRotations.RightLittleDistal = GetRotationFromAvatar(avatar, "RightLittleDistal");

            return initialRotations;
        }

        public static BoneRotations CreateInitialLocalRotations(Animator animator) {
            return new BoneRotations {
                Hips = animator.GetBoneTransform(HumanBodyBones.Hips).localRotation,
                Spine = animator.GetBoneTransform(HumanBodyBones.Spine).localRotation,
                Chest = animator.GetBoneTransform(HumanBodyBones.Chest).localRotation,
                LeftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation,
                RightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).localRotation,
                LeftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).localRotation,
                RightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).localRotation,
                LeftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).localRotation,
                RightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).localRotation,
                LeftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).localRotation,
                RightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation,
                LeftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).localRotation,
                RightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).localRotation,
                LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand).localRotation,
                RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand).localRotation,
                LeftThumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal).localRotation,
                LeftThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate).localRotation,
                LeftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal).localRotation,
                LeftIndexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).localRotation,
                LeftIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate).localRotation,
                LeftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal).localRotation,
                LeftMiddleProximal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).localRotation,
                LeftMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate).localRotation,
                LeftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal).localRotation,
                LeftRingProximal = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal).localRotation,
                LeftRingIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate).localRotation,
                LeftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal).localRotation,
                LeftLittleProximal = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal).localRotation,
                LeftLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate).localRotation,
                LeftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal).localRotation,
                RightThumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal).localRotation,
                RightThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate).localRotation,
                RightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal).localRotation,
                RightIndexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal).localRotation,
                RightIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate).localRotation,
                RightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal).localRotation,
                RightMiddleProximal = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).localRotation,
                RightMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate).localRotation,
                RightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal).localRotation,
                RightRingProximal = animator.GetBoneTransform(HumanBodyBones.RightRingProximal).localRotation,
                RightRingIntermediate = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate).localRotation,
                RightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal).localRotation,
                RightLittleProximal = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal).localRotation,
                RightLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate).localRotation,
                RightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal).localRotation
            };
        }

        public static BoneRotations CreateInitialGlobalRotations(Animator animator) {
            BoneRotations initialRotations = new BoneRotations();
            initialRotations.Hips = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
            initialRotations.Spine = animator.GetBoneTransform(HumanBodyBones.Spine).rotation;
            initialRotations.Chest = animator.GetBoneTransform(HumanBodyBones.Chest).rotation;
            initialRotations.LeftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation;
            initialRotations.RightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).rotation;
            initialRotations.LeftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
            initialRotations.RightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
            initialRotations.LeftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).rotation;
            initialRotations.RightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).rotation;
            initialRotations.LeftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).rotation;
            initialRotations.RightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).rotation;
            initialRotations.LeftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).rotation;
            initialRotations.RightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).rotation;
            initialRotations.LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
            initialRotations.RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
            initialRotations.LeftThumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal).rotation;
            initialRotations.LeftThumbIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate).rotation;
            initialRotations.LeftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal).rotation;
            initialRotations.LeftIndexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).rotation;
            initialRotations.LeftIndexIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate).rotation;
            initialRotations.LeftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal).rotation;
            initialRotations.LeftMiddleProximal =
                animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).rotation;
            initialRotations.LeftMiddleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate).rotation;
            initialRotations.LeftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal).rotation;
            initialRotations.LeftRingProximal = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal).rotation;
            initialRotations.LeftRingIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate).rotation;
            initialRotations.LeftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal).rotation;
            initialRotations.LeftLittleProximal =
                animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal).rotation;
            initialRotations.LeftLittleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate).rotation;
            initialRotations.LeftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal).rotation;
            initialRotations.RightThumbProximal =
                animator.GetBoneTransform(HumanBodyBones.RightThumbProximal).rotation;
            initialRotations.RightThumbIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate).rotation;
            initialRotations.RightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal).rotation;
            initialRotations.RightIndexProximal =
                animator.GetBoneTransform(HumanBodyBones.RightIndexProximal).rotation;
            initialRotations.RightIndexIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate).rotation;
            initialRotations.RightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal).rotation;
            initialRotations.RightMiddleProximal =
                animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).rotation;
            initialRotations.RightMiddleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate).rotation;
            initialRotations.RightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal).rotation;
            initialRotations.RightRingProximal = animator.GetBoneTransform(HumanBodyBones.RightRingProximal).rotation;
            initialRotations.RightRingIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate).rotation;
            initialRotations.RightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal).rotation;
            initialRotations.RightLittleProximal =
                animator.GetBoneTransform(HumanBodyBones.RightLittleProximal).rotation;
            initialRotations.RightLittleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate).rotation;
            initialRotations.RightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal).rotation;

            return initialRotations;
        }

        public static void UpdateGlobalRotations(ref BoneRotations _boneRotations, Animator animator) {
            _boneRotations.Hips = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
            _boneRotations.Spine = animator.GetBoneTransform(HumanBodyBones.Spine).rotation;
            _boneRotations.Chest = animator.GetBoneTransform(HumanBodyBones.Chest).rotation;
            _boneRotations.LeftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation;
            _boneRotations.RightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).rotation;
            _boneRotations.LeftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
            _boneRotations.RightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
            _boneRotations.LeftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).rotation;
            _boneRotations.RightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).rotation;
            _boneRotations.LeftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).rotation;
            _boneRotations.RightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).rotation;
            _boneRotations.LeftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).rotation;
            _boneRotations.RightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).rotation;
            _boneRotations.LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
            _boneRotations.RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
            _boneRotations.LeftThumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal).rotation;
            _boneRotations.LeftThumbIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate).rotation;
            _boneRotations.LeftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal).rotation;
            _boneRotations.LeftIndexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).rotation;
            _boneRotations.LeftIndexIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate).rotation;
            _boneRotations.LeftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal).rotation;
            _boneRotations.LeftMiddleProximal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).rotation;
            _boneRotations.LeftMiddleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate).rotation;
            _boneRotations.LeftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal).rotation;
            _boneRotations.LeftRingProximal = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal).rotation;
            _boneRotations.LeftRingIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate).rotation;
            _boneRotations.LeftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal).rotation;
            _boneRotations.LeftLittleProximal = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal).rotation;
            _boneRotations.LeftLittleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate).rotation;
            _boneRotations.LeftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal).rotation;
            _boneRotations.RightThumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal).rotation;
            _boneRotations.RightThumbIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate).rotation;
            _boneRotations.RightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal).rotation;
            _boneRotations.RightIndexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal).rotation;
            _boneRotations.RightIndexIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate).rotation;
            _boneRotations.RightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal).rotation;
            _boneRotations.RightMiddleProximal =
                animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).rotation;
            _boneRotations.RightMiddleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate).rotation;
            _boneRotations.RightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal).rotation;
            _boneRotations.RightRingProximal = animator.GetBoneTransform(HumanBodyBones.RightRingProximal).rotation;
            _boneRotations.RightRingIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate).rotation;
            _boneRotations.RightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal).rotation;
            _boneRotations.RightLittleProximal =
                animator.GetBoneTransform(HumanBodyBones.RightLittleProximal).rotation;
            _boneRotations.RightLittleIntermediate =
                animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate).rotation;
            _boneRotations.RightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal).rotation;
        }

        public static Quaternion GetRotationFromAvatar(Avatar _avatar, string _jointname) {
            Quaternion rot = Quaternion.Euler(0, 0, 0);
            HumanDescription humanDescription = _avatar.humanDescription;
            HumanBone[] humanBones = humanDescription.human;
            HumanBone foundHumanBone = System.Array.Find(humanBones, x => x.humanName == _jointname);

            if (foundHumanBone.humanName != _jointname) {
                return rot;
            }

            SkeletonBone[] skeletonBones = humanDescription.skeleton;

            SkeletonBone foundSkeletonBone = System.Array.Find(skeletonBones, x => x.name == foundHumanBone.boneName);

            return foundSkeletonBone.rotation;
        }

        public static Vector3 GetLocalPositionFromAvatar(Avatar _avatar, string _jointname) {
            Vector3 pos = Vector3.zero;
            HumanDescription humanDescription = _avatar.humanDescription;
            HumanBone[] humanBones = humanDescription.human;
            HumanBone foundHumanBone = System.Array.Find(humanBones, x => x.humanName == _jointname);

            if (foundHumanBone.humanName != _jointname) {
                return pos;
            }

            SkeletonBone[] skeletonBones = humanDescription.skeleton;

            SkeletonBone foundSkeletonBone = System.Array.Find(skeletonBones, x => x.name == foundHumanBone.boneName);

            return foundSkeletonBone.position;
        }

        public static string AvatarToString(Avatar _avatar) {
            string s = "";
            HumanDescription humanDescription = _avatar.humanDescription;

            List<string> names = new List<string>();
            foreach (HumanBone humanBone in humanDescription.human) {
                s = s + humanBone.boneName + " " + humanBone.humanName + "\n";
                names.Add(humanBone.boneName);
            }

            s = s + "\n";

            foreach (SkeletonBone bone in humanDescription.skeleton) {
                if (!string.IsNullOrEmpty(names.Find(x => x == bone.name))) {
                    s = s + " " + bone.name + "\n\tposition:\t" + bone.position + "\n\trotation:\t" + bone.rotation +
                        "\n\tscale:\t" + bone.scale + "\n";
                }
            }

            s = s + "\n";

            s = s + "upperArmTwist: " + humanDescription.upperArmTwist + "\n";
            s = s + "lowerArmTwist: " + humanDescription.lowerArmTwist + "\n";
            s = s + "upperLegTwist: " + humanDescription.upperLegTwist + "\n";
            s = s + "lowerLegTwist: " + humanDescription.lowerLegTwist + "\n";
            s = s + "armStretch: " + humanDescription.armStretch + "\n";
            s = s + "legStretch: " + humanDescription.legStretch + "\n";
            s = s + "feetSpacing: " + humanDescription.feetSpacing + "\n";
            s = s + "hasTranslationDoF: " + humanDescription.hasTranslationDoF + "\n";
            return s;
        }
    }

}
