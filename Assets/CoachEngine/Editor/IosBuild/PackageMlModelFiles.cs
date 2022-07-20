// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

#if UNITY_IOS

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace CoachAiEngine.Editor {
    public static class PackageMlModelFiles {

        [PostProcessBuildAttribute(10)]
        public static void OnPostprocessBuild(BuildTarget target, string buildPath) {
            var modelsDir = $"{Application.dataPath}/CoachEngine/MLModels";
            if (!Directory.Exists(modelsDir)) return;

            var projectFilePath = PBXProject.GetPBXProjectPath(buildPath);
            var project = OpenProject(projectFilePath);
            var unityTarget = project.GetUnityMainTargetGuid();
            var unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
            var models = Directory.EnumerateFiles(modelsDir)
                .Where(f => Path.GetExtension(f) == ".tflite" || Path.GetExtension(f) == ".mlmodel")
                .ToArray();

            foreach (var modelPath in models) {
                AddModelFileToTarget(project, unityTarget, modelPath);
                AddModelFileToTarget(project, unityFrameworkTarget, modelPath);
            }

            if (models.Any(model => Path.GetExtension(model) == ".mlmodel")) {
                // required to link compiled CoreML models
                project.AddFrameworkToProject(unityTarget, "CoreML.framework", true);
                project.AddFrameworkToProject(unityTarget, "CoreVideo.framework", true);
            }

            SaveProject(project, projectFilePath);
        }

        private static void AddModelFileToTarget(PBXProject project, string targetGuid, string modelPath) {
            var buildPhase = Path.GetExtension(modelPath) switch {
                ".tflite" => project.GetResourcesBuildPhaseByTarget(targetGuid),
                ".mlmodel" => project.GetSourcesBuildPhaseByTarget(targetGuid),
                _ => throw new ArgumentException($"Unsupported ml model found at: {modelPath}.")
            };
            var fileGuid = project.AddFile(modelPath, Path.GetFileName(modelPath));
            project.AddFileToBuildSection(targetGuid, buildPhase, fileGuid);
        }

        private static PBXProject OpenProject(string projectFilePath) {
            var project = new PBXProject();
            project.ReadFromFile(projectFilePath);
            return project;
        }

        private static void SaveProject(PBXProject project, string projectFilePath) =>
            File.WriteAllText(projectFilePath, project.WriteToString());
    }
}

#endif

