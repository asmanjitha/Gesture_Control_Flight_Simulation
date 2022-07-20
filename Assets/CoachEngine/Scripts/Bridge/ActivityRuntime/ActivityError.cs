// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

namespace CoachAiEngine {

    public readonly struct ActivityError {
        public readonly string Message;
        public readonly int Code;
        public ActivityError(string message, int code = 10_000) {
            Message = message;
            Code = code;
        }
    }
}
