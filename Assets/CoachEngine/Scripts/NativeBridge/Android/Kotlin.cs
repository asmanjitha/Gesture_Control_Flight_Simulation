// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine.Android {

    public static class Kotlin {
        /// <summary>
        ///
        /// </summary>
        /// <param name="kClassName">the Kotlin class name of the object</param>
        /// <returns>An <see cref="AndroidJavaObject"/> representing the Kotlin object instance for this class</returns>
        public static AndroidJavaObject GetObject(string kClassName) {
            var kClass = new AndroidJavaClass(kClassName);
            return kClass.GetStatic<AndroidJavaObject>("INSTANCE");
        }
    }
}
