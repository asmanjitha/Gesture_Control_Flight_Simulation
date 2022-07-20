/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using UnityEngine;
using CoachAiEngine.Analytics.Pose;

namespace CoachAiEngine {

    public abstract class PoseAnimator : MonoBehaviour {

        public void UpdatePose(PublicEvent @event) {
            var poses = BodyPose.CreateFrom(@event, CoordinateSpace.World);
            if (poses.Count == 0) {
                return;
            }
            UpdatePose(poses[0]);
        }

        public virtual void UpdatePose(BodyPose bodyPose){}
        public virtual void UpdatePose(float[] jointArray){}
    }
}
