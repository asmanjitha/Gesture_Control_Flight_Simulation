// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using UnityEngine;

namespace CoachAiEngine {

    public class ScreenUtils {

        public static int AllowSleep(bool allow) {
            var oldSetting = Screen.sleepTimeout;
            Screen.sleepTimeout = allow ? SleepTimeout.SystemSetting : SleepTimeout.NeverSleep;
            return oldSetting;
        }

        public static int AllowSleep(int allow) {
            var oldSetting = Screen.sleepTimeout;
            Screen.sleepTimeout = allow;
            return oldSetting;
        }

    }

}
