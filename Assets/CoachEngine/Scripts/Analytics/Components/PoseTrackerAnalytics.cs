/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by a tool.
// CoachAI Core Version: 13.2.3
// Generated On: 2022-06-13T13:29:09Z
//
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CoachAiEngine;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CoachAiEngine.Analytics {

    [System.CodeDom.Compiler.GeneratedCode(tool: "Coach-Ai engine", version: "13.2.3")]
    [CreateAssetMenu(fileName = nameof(PoseTrackerAnalytics), menuName = "Coach-AI Engine/Components/HumanBody/PoseTrackerAnalytics")]
    public class PoseTrackerAnalytics : CoachComponent {

        public override string ComponentId => "com.coachai.engine.analytics.pose.components.PoseTrackerAnalytics";
        private static readonly string[] eventIds =  {
            "com.coachai.engine.analytics.pose.Pose2dResult",
            "com.coachai.engine.analytics.pose.JointLocation",
            "com.coachai.engine.analytics.pose.RegionsOfInterest"
        };

        private static readonly PoseModelForFullFrameType PoseModelForFullFrame = PoseModelForFullFrameType.PoseMulti17lu1fGeneralLightV7;
        private static readonly bool UsePoseSmoothing = false;
        private static readonly float PoseSmoothingFactor = 0.1f;
        private static readonly float PoseSmoothingMaxDisplacement = 20.0f;
        private static readonly bool EnablePose2DLocalizationOnGroundWithAR = false;
        private static readonly bool EnablePersonSegmentation = false;
        private static readonly bool UsePersonDetectorForAdaptiveRegionOfInterest = true;
        private static readonly SortPoseByType SortPoseBy = SortPoseByType.AREA;
        private static readonly bool UseAdaptiveRegionOfInterest = true;
        private static readonly PoseModelRoiType PoseModelRoi = PoseModelRoiType.PoseMulti17lu1fGeneralLightV7;
        private static readonly int TargetFPS = 30;
        private static readonly int MaximumPipelineThreads = 2;
        private static readonly int MaximumInboundQueueSize = 2;

        [Tooltip("Choose a pose model to use for full frame detection, or initialization if adaptive region of interest is enabled.")]
        public PoseModelForFullFrameType poseModelForFullFrame = PoseModelForFullFrame;
        [Tooltip("All detected poses will be smoothed over time as long as movements are not too quick.")]
        public bool usePoseSmoothing = UsePoseSmoothing;
        [Tooltip("A value of 1.0 means that the previous pose is not used for smoothing.")]
        public float poseSmoothingFactor = PoseSmoothingFactor;
        [Tooltip("No smoothing is applied if the current pose exceeds this threshold of movement (pixels). This will be reworked in a future release, to be based on person size not on pixels!")]
        public float poseSmoothingMaxDisplacement = PoseSmoothingMaxDisplacement;
        [Tooltip("Use AR planes to determine the location of Pose2D on the ground.")]
        public bool enablePose2DLocalizationOnGroundWithAR = EnablePose2DLocalizationOnGroundWithAR;
        [Tooltip("Enable person segmentation output, if the model supports it.")]
        public bool enablePersonSegmentation = EnablePersonSegmentation;
        [Tooltip("Use the [PersonDetectorResult] to (re-)initialize the region of interest in case the pose was lost. Enable this option if you expect the Pose to be far away from the phone and leaving and re-entering the camera view. If disabled, Pose2dResults are used to (re-)initialize the region of interest. Make sure to run the [PersonDetector] Analytics in addition to the [PoseTrackerAnalytics] to receive [PersonDetectorResult]s")]
        public bool usePersonDetectorForAdaptiveRegionOfInterest = UsePersonDetectorForAdaptiveRegionOfInterest;
        [Tooltip("Sort the poses on the basis of proximity, pose confidence or bounding box area")]
        public SortPoseByType sortPoseBy = SortPoseBy;
        [Tooltip("Use an adaptive region of interest to improve pose estimation, specifically of persons further away. As soon as a pose is found, pose estimation is applied only on a sub-region in the image where the person is predicted to be. This yields better accuracy and precision of pose estimation and enables much higher distances from the camera. Current limitations: * currently only single pose * If multiple persons are in view might track the wrong person. Also see parameter [SortPoseBy]")]
        public bool useAdaptiveRegionOfInterest = UseAdaptiveRegionOfInterest;
        [Tooltip("Choose a pose model to be used within an adaptive region of interest.")]
        public PoseModelRoiType poseModelRoi = PoseModelRoi;
        [Tooltip("Set maximum allowed processed frames per second")]
        public int targetFPS = TargetFPS;
        [Tooltip("Defines how many threads are used to concurrently process inbound events.")]
        public int maximumPipelineThreads = MaximumPipelineThreads;
        [Tooltip("Set maximum allowed inbound events to put into processing queue")]
        public int maximumInboundQueueSize = MaximumInboundQueueSize;

        public override Dictionary<string, object> Parameters {
            get {
                var parameters = new Dictionary<string, object>();

                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseModelForFullFrame",
                    PoseModelForFullFrameTypeLookup[poseModelForFullFrame], PoseModelForFullFrameTypeLookup[PoseModelForFullFrame], parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.UsePoseSmoothing",
                    usePoseSmoothing, UsePoseSmoothing, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseSmoothingFactor",
                    poseSmoothingFactor, PoseSmoothingFactor, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseSmoothingMaxDisplacement",
                    poseSmoothingMaxDisplacement, PoseSmoothingMaxDisplacement, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.EnablePose2DLocalizationOnGroundWithAR",
                    enablePose2DLocalizationOnGroundWithAR, EnablePose2DLocalizationOnGroundWithAR, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.EnablePersonSegmentation",
                    enablePersonSegmentation, EnablePersonSegmentation, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.UsePersonDetectorForAdaptiveRegionOfInterest",
                    usePersonDetectorForAdaptiveRegionOfInterest, UsePersonDetectorForAdaptiveRegionOfInterest, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.SortPoseBy",
                    SortPoseByTypeLookup[sortPoseBy], SortPoseByTypeLookup[SortPoseBy], parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.UseAdaptiveRegionOfInterest",
                    useAdaptiveRegionOfInterest, UseAdaptiveRegionOfInterest, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.components.PoseModelRoi",
                    PoseModelRoiTypeLookup[poseModelRoi], PoseModelRoiTypeLookup[PoseModelRoi], parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.parameters.TargetFPS",
                    targetFPS, TargetFPS, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.parameters.MaximumPipelineThreads",
                    maximumPipelineThreads, MaximumPipelineThreads, parameters
                );
                AddUnlessDefault("com.coachai.engine.analytics.pose.parameters.MaximumInboundQueueSize",
                    maximumInboundQueueSize, MaximumInboundQueueSize, parameters
                );
                return parameters;
            }
        }

        private void AddUnlessDefault<T>(string key, T value, T @default, Dictionary<string, object> parameters) {
            if (value.Equals(@default)) return;
            parameters.Add(key, value);
        }

        public override List<string> PublishedEventIds => eventIds.ToList();

        public enum PoseModelForFullFrameType {
            PoseMulti17lu1fGeneralLightV7,
            PoseMulti17lu1fGeneralLightV2,
            PoseMulti17lu1fGeneralMediumV4,
            PoseMulti17lu1fDancingLightV12,
            PoseMulti17lu1fDancingLightV4,
            PoseMulti21flu1fDancingLightV1,
            PoseMulti25fhlu1fDancingLightV4,
            PoseSegMulti17lu1fDancingLightV1,
            PoseSegMulti25fhlu1fDancingLightV1,
            PoseSegMulti17lu1fGeneralLightV6,
            PoseSingle17lu1fFitnessMediumV2,
            PoseSingle17lu1fFitnessLightV2
        }

        public Dictionary<PoseModelForFullFrameType, string> PoseModelForFullFrameTypeLookup = new Dictionary<PoseModelForFullFrameType, string> {
            { PoseModelForFullFrameType.PoseMulti17lu1fGeneralLightV7, "pose.multi-17lu.1f.general.light v7" },
            { PoseModelForFullFrameType.PoseMulti17lu1fGeneralLightV2, "pose.multi-17lu.1f.general.light v2 (Deprecated)" },
            { PoseModelForFullFrameType.PoseMulti17lu1fGeneralMediumV4, "pose.multi-17lu.1f.general.medium v4" },
            { PoseModelForFullFrameType.PoseMulti17lu1fDancingLightV12, "pose.multi-17lu.1f.dancing.light v12" },
            { PoseModelForFullFrameType.PoseMulti17lu1fDancingLightV4, "pose.multi-17lu.1f.dancing.light v4" },
            { PoseModelForFullFrameType.PoseMulti21flu1fDancingLightV1, "pose.multi-21flu.1f.dancing.light v1" },
            { PoseModelForFullFrameType.PoseMulti25fhlu1fDancingLightV4, "pose.multi-25fhlu.1f.dancing.light v4" },
            { PoseModelForFullFrameType.PoseSegMulti17lu1fDancingLightV1, "pose.seg-multi-17lu.1f.dancing.light v1" },
            { PoseModelForFullFrameType.PoseSegMulti25fhlu1fDancingLightV1, "pose.seg-multi-25fhlu.1f.dancing.light v1" },
            { PoseModelForFullFrameType.PoseSegMulti17lu1fGeneralLightV6, "pose.seg-multi-17lu.1f.general.light v6" },
            { PoseModelForFullFrameType.PoseSingle17lu1fFitnessMediumV2, "pose.single-17lu.1f.fitness.medium v2" },
            { PoseModelForFullFrameType.PoseSingle17lu1fFitnessLightV2, "pose.single-17lu.1f.fitness.light v2" }
        };

        public enum SortPoseByType {
            ROI_CENTER_PROXIMITY,
            SCORE,
            AREA,
            EXTENDED_AREA
        }

        public Dictionary<SortPoseByType, string> SortPoseByTypeLookup = new Dictionary<SortPoseByType, string> {
            { SortPoseByType.ROI_CENTER_PROXIMITY, "ROI_CENTER_PROXIMITY" },
            { SortPoseByType.SCORE, "SCORE" },
            { SortPoseByType.AREA, "AREA" },
            { SortPoseByType.EXTENDED_AREA, "EXTENDED_AREA" }
        };

        public enum PoseModelRoiType {
            PoseMulti17lu1fGeneralLightV7,
            PoseMulti17lu1fGeneralLightV2,
            PoseMulti17lu1fGeneralMediumV4,
            PoseMulti17lu1fDancingLightV12,
            PoseMulti17lu1fDancingLightV4,
            PoseMulti21flu1fDancingLightV1,
            PoseMulti25fhlu1fDancingLightV4,
            PoseSegMulti17lu1fDancingLightV1,
            PoseSegMulti25fhlu1fDancingLightV1,
            PoseSegMulti17lu1fGeneralLightV6,
            PoseSingle17lu1fFitnessMediumV2,
            PoseSingle17lu1fFitnessLightV2
        }

        public Dictionary<PoseModelRoiType, string> PoseModelRoiTypeLookup = new Dictionary<PoseModelRoiType, string> {
            { PoseModelRoiType.PoseMulti17lu1fGeneralLightV7, "pose.multi-17lu.1f.general.light v7" },
            { PoseModelRoiType.PoseMulti17lu1fGeneralLightV2, "pose.multi-17lu.1f.general.light v2 (Deprecated)" },
            { PoseModelRoiType.PoseMulti17lu1fGeneralMediumV4, "pose.multi-17lu.1f.general.medium v4" },
            { PoseModelRoiType.PoseMulti17lu1fDancingLightV12, "pose.multi-17lu.1f.dancing.light v12" },
            { PoseModelRoiType.PoseMulti17lu1fDancingLightV4, "pose.multi-17lu.1f.dancing.light v4" },
            { PoseModelRoiType.PoseMulti21flu1fDancingLightV1, "pose.multi-21flu.1f.dancing.light v1" },
            { PoseModelRoiType.PoseMulti25fhlu1fDancingLightV4, "pose.multi-25fhlu.1f.dancing.light v4" },
            { PoseModelRoiType.PoseSegMulti17lu1fDancingLightV1, "pose.seg-multi-17lu.1f.dancing.light v1" },
            { PoseModelRoiType.PoseSegMulti25fhlu1fDancingLightV1, "pose.seg-multi-25fhlu.1f.dancing.light v1" },
            { PoseModelRoiType.PoseSegMulti17lu1fGeneralLightV6, "pose.seg-multi-17lu.1f.general.light v6" },
            { PoseModelRoiType.PoseSingle17lu1fFitnessMediumV2, "pose.single-17lu.1f.fitness.medium v2" },
            { PoseModelRoiType.PoseSingle17lu1fFitnessLightV2, "pose.single-17lu.1f.fitness.light v2" }
        };

    }
}