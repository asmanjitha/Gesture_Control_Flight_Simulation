// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

namespace CoachAiEngine {
    public interface IActivityCallbacks {
        void OnDecisionRequest(int requestId, string json);
        void OnFinish(int finishFlag);
        void OnInit();
        void OnError(string json);
        void OnRequire(int requirementId, string json);
    }
}

public enum FinishFlags {
    Finish = 0x00,
    Abort = 0x01
}
