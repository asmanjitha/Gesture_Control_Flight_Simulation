// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

#if UNITY_ANDROID

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace CoachAiEngine.Editor {
    public class PackageMlModelFiles : IPostGenerateGradleAndroidProject {
        public int callbackOrder => 0;

        private string _projectDir;
        void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path) {
            // path will be inside unityLibrary Gradle module folder
            _projectDir = Path.Combine(path, "..");
            if (EditorUserBuildSettings.buildAppBundle) {
                const string assetPackName = "coach_ai_models";
                CreateAssetPackGradleModule(assetPackName);
                CopyModelFilesToAssets(assetPackName);
            } else {
                // we are building a plain apk. we can simply copy the model files
                // into the assets of the existing unityLibrary module
                CopyModelFilesToAssets("unityLibrary");
            }
        }

        private void CreateAssetPackGradleModule(string assetPackName) {
            var assetPackDir = Path.Combine(_projectDir, assetPackName);
            Directory.CreateDirectory(assetPackDir);
            // create asset pack Gradle module
            var gradleFile = Path.Combine(assetPackDir, "build.gradle");
            var gradleFileContent = $@"apply plugin: 'com.android.asset-pack'
assetPack {{
    packName = ""{assetPackName}""
    dynamicDelivery {{
        deliveryType = ""install-time""
    }}
}}
";
            File.WriteAllText(gradleFile, gradleFileContent);

            // include asset pack in launcher Gradle module
            var launcherGradleFile = Path.Combine(_projectDir, "launcher/build.gradle");
            File.AppendAllText(launcherGradleFile,$@"
android {{
    assetPacks = ["":{assetPackName}""]
}}
");
            // add asset pack module to settings.gradle
            var settingsFile = Path.Combine(_projectDir, "settings.gradle");
            File.AppendAllLines(settingsFile, new[] {"", $"include ':{assetPackName}'"});
        }

        private void CopyModelFilesToAssets(string targetModuleName) {
            var modelSrcDir = Path.Combine(Application.dataPath, "CoachEngine/MLModels");
            if (!Directory.Exists(modelSrcDir)) return;
            var assetDir = Path.Combine(_projectDir, targetModuleName, "src/main/assets");
            Directory.CreateDirectory(assetDir);
            foreach (var file in Directory.GetFiles(modelSrcDir)
                .Select(Path.GetFileName)
                .Where(filename => Path.GetExtension(filename) == ".tflite")
            ) {
                File.Copy($"{modelSrcDir}/{file}", $"{assetDir}/{file}", true);
            }
        }
    }
}

#endif
