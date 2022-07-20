// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */
#if UNITY_ANDROID

using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoachAiEngine.Editor {
    public class ValidateBuildProcessor : IPreprocessBuildWithReport {
        public int callbackOrder => 99;

        public void OnPreprocessBuild(BuildReport report) {
            var errorCount = 0;
            var errorMsg = new StringBuilder();
            errorMsg.AppendLine("The following problems have been detected with your project configuration:");

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64) {
                errorCount++;
                errorMsg.AppendLine("* Unsupported target architectures. Only ARM64 is supported");
            }

            var vulkanEnabled = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)
                    .Contains(GraphicsDeviceType.Vulkan);

            if (vulkanEnabled) {
                errorCount++;
                errorMsg.AppendLine("* Unsupported graphics apis. Only OpenGL ES3 is supported.");
            }

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP) {
                errorCount++;
                errorMsg.AppendLine("* Unsupported scripting backend. Only IL2CPP is supported.");
            }

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24) {
                errorCount++;
                errorMsg.AppendLine("* Unsupported Android API level. Minimum api level of 24 is required.");
            }

            if (errorCount == 0) return;
#if !COACH_AI_NO_VERIFY_BUILD
            errorMsg.AppendLine("You can disable this check by defining the scripting symbol " +
                                "COACH_AI_NO_VERIFY_BUILD in your project settings.");
            throw new BuildFailedException(errorMsg.ToString());
#endif
#pragma warning disable 162
            Debug.LogWarning(errorMsg.ToString());
#pragma warning restore 162
        }
    }
}

#endif
