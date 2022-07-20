// /*
//  * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System.Collections.Generic;
using UnityEngine;

namespace CoachAiEngine {

    public abstract class CoachComponent : ScriptableObject {
        public abstract string ComponentId { get; }
        public abstract List<string> PublishedEventIds { get; }
        public abstract Dictionary<string, object> Parameters { get; }
    }
}
