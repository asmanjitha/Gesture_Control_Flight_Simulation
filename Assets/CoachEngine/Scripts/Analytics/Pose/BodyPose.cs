/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


namespace CoachAiEngine {
    public enum CoordinateSpace {
        World,
        Camera
    }
}

namespace CoachAiEngine.Analytics.Pose {
    /**
     * <summary>
     * Wraps a prediction for a human body from Coach SDK, providing access to all
     * recognized joints.
     * </summary>
     */
    public class BodyPose {
        public const string Pose3dEventId = "com.coachai.engine.analytics.pose3d.Pose3dResult";
        public const string PoseEventId = "com.coachai.engine.analytics.pose.Pose2dResult";

        public enum JointType {
            Nose,
            LeftEye,
            RightEye,
            LeftEar,
            RightEar,
            LeftShoulder,
            RightShoulder,
            LeftElbow,
            RightElbow,
            LeftWrist,
            RightWrist,
            LeftHip,
            RightHip,
            LeftKnee,
            RightKnee,
            LeftAnkle,
            RightAnkle
        }

        public class Joint {

            public JointType Type;

            /**
             * Joint location are in sensor space. To transform these into screen space use
             * <see cref="DeviceCamera.TransformToScreenCoordinates"/>
             */
            public Vector3 Location;

            public float Score;

            public static List<Joint> CreateFromJointMapJson(JArray jsonJointList) {
                var joints = new List<Joint>();

                foreach (var joint in jsonJointList) {
                    AddJoint(joint, ref joints);
                }

                return joints;
            }

            private static void AddJoint([CanBeNull] JToken jsonJoint, ref List<Joint> joints) {
                if (!(jsonJoint is JObject @object)) return;

                try {
                    var keyPoint = @object["keyPoint"]?.Value<string>();
                    if (!Enum.TryParse<JointType>(keyPoint, true, out var jointType)) return;
                    var loc = @object["loc3"] as JArray;
                    var score = @object["score"]?.Value<float>() ?? 0f;
                    var location = loc.Count == 2
                        ? new Vector3(loc[0].Value<float>(), loc[1].Value<float>())
                        : new Vector3(loc[0].Value<float>(), loc[1].Value<float>(), loc[2].Value<float>());

                    joints.Add(new Joint {
                        Type = jointType,
                        Location = location,
                        Score = score,
                    });
                } catch (Exception e) {
                    Debug.LogWarning($"{nameof(BodyPose)}: Couldn't parse joint {jsonJoint}");
                    Debug.LogException(e);
                }

            }
        }

        /**
         * Predictions for the individual joint positions
         */
        public List<Joint> Joints;

        /**
         * An overall score [0,1] denoting the confidence of the prediction.
         */
        public float Score;

        /**
         * The camera with which this prediction was obtained.
         */
        public DeviceCamera Camera;

        public Joint Nose => Joints.FirstOrDefault(j => j.Type == JointType.Nose);
        public Joint LeftEye => Joints.FirstOrDefault(j => j.Type == JointType.LeftEye);
        public Joint RightEye => Joints.FirstOrDefault(j => j.Type == JointType.RightEye);
        public Joint LeftEar => Joints.FirstOrDefault(j => j.Type == JointType.LeftEar);
        public Joint RightEar => Joints.FirstOrDefault(j => j.Type == JointType.RightEar);

        public Joint LeftShoulder => Joints.FirstOrDefault(j => j.Type == JointType.LeftShoulder);
        public Joint LeftElbow => Joints.FirstOrDefault(j => j.Type == JointType.LeftElbow);
        public Joint LeftWrist => Joints.FirstOrDefault(j => j.Type == JointType.LeftWrist);

        public Joint RightShoulder => Joints.FirstOrDefault(j => j.Type == JointType.RightShoulder);
        public Joint RightElbow => Joints.FirstOrDefault(j => j.Type == JointType.RightElbow);
        public Joint RightWrist => Joints.FirstOrDefault(j => j.Type == JointType.RightWrist);

        public Joint LeftHip => Joints.FirstOrDefault(j => j.Type == JointType.LeftHip);
        public Joint LeftKnee => Joints.FirstOrDefault(j => j.Type == JointType.LeftKnee);
        public Joint LeftAnkle => Joints.FirstOrDefault(j => j.Type == JointType.LeftAnkle);

        public Joint RightHip => Joints.FirstOrDefault(j => j.Type == JointType.RightHip);
        public Joint RightKnee => Joints.FirstOrDefault(j => j.Type == JointType.RightKnee);
        public Joint RightAnkle => Joints.FirstOrDefault(j => j.Type == JointType.RightAnkle);

        /**
         * Transforms a joint (which is given in camera pixels) to screen coordinates.
         */
        public Vector2 ToScreenCoordinates(Joint joint) {
            return Camera.TransformToScreenCoordinates(joint.Location);
        }

        /**
         * Transforms a joint from Coach engine coordinate system to Unity world coordinates.
         */
        public Vector3 ToWorldCoordinates(Joint joint) {
            return new Vector3(joint.Location.x, joint.Location.y, joint.Location.z * -1f);
        }

        public static List<BodyPose> CreateFrom(PublicEvent @event, CoordinateSpace coordinateSpace = CoordinateSpace.Camera) {
            switch (@event) {
                case {Type: PoseEventId} when @event.Properties.TryGetValue("poses", out var poses):
                    return CreateFromPublicPoseResult(@event, poses as JArray);
                case {Type: Pose3dEventId} when coordinateSpace == CoordinateSpace.Camera &&
                                                @event.Properties.TryGetValue("posesInCameraCoordinates", out var poses):
                    return CreateFromPose3dResult(@event, poses as JArray, coordinateSpace);
                case {Type: Pose3dEventId} when coordinateSpace == CoordinateSpace.World &&
                                                @event.Properties.TryGetValue("posesInWorldCoordinates", out var poses):
                    return CreateFromPose3dResult(@event, poses as JArray, coordinateSpace);
                default:
                    Debug.LogWarning($"{nameof(BodyPose)}: provided event not a known pose event: {@event.Type}");
                    return new List<BodyPose>();
            }
        }

        private static List<BodyPose> CreateFromPose3dResult(
            PublicEvent @event, JArray poses, CoordinateSpace coordinateSpace
        ) {
            var jointsInWorldCoordinates = coordinateSpace switch {
                CoordinateSpace.World => poses.FirstOrDefault(token => token["type"].Value<string>() == "WORLD"),
                CoordinateSpace.Camera => poses.FirstOrDefault(token => token["type"].Value<string>() == "CAMERA"),
                _ => null
            };

            if (jointsInWorldCoordinates == null) return new List<BodyPose>();

            var camera = DeviceCamera.CreateFrom(@event.Properties["camera"] as JObject);
            if (null == camera) {
                Debug.LogWarning("Pose3dResult: Could not extract camera");
                camera = new DeviceCamera();
            }

            var bodyPoses = new List<BodyPose>();
            ParseJointMap(jointsInWorldCoordinates, camera, ref bodyPoses);
            return bodyPoses;
        }

        private static List<BodyPose> CreateFromPublicPoseResult(PublicEvent @event, JArray objects) {
            if (objects == null) return new List<BodyPose>();

            var camera = DeviceCamera.CreateFrom(@event.Properties["camera"] as JObject);
            if (null == camera) {
                Debug.LogWarning("PoseResult: Could not extract camera");
                camera = new DeviceCamera();
            }

            var poses = new List<BodyPose>();
            foreach (var token in objects) {
                ParseJointMap(token, camera, ref poses);
            }

            return poses;
        }

        private static void ParseJointMap(JToken token, DeviceCamera camera, ref List<BodyPose> poses) {
            if (!(token is JObject)) return;
            if (token["joints"] is JArray joints) {
                var jointsList = Joint.CreateFromJointMapJson(joints);
                var score = token["score"].Value<float>();

                poses.Add(new BodyPose {
                    Joints = jointsList,
                    Score = score,
                    Camera = camera
                });
            }
        }
    }
}
