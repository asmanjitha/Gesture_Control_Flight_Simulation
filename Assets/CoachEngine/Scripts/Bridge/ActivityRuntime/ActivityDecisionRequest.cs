// /*
//  * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System.Collections.Generic;

namespace CoachAiEngine {

    /**
     * <summary>Encapsulates the data associated with a decision request
     * issued by a native Coach-Ai activity.</summary>
     */
    public class ActivityDecisionRequest {
        /**
         * <summary>The id of the request as assigned by the native activity.</summary>
         */
        public int Id;

        /**
         * <summary>The type of the request as defined by the native activity.</summary>
         */
        public string Type;

        /**
         * <summary>The dictionary of the data associated with this request.</summary>
         */
        public Dictionary<string, object> Properties;

        /**
         * <summary>Replies to this request with the decision made.</summary>
         * <remarks>The decision data must be deserializable into an ActivityDecision
         * object that is understood by the activity that requested the decision.
         * </remarks>
         * <param name="decisionData">The data that represents the decision made.
         * Coach-Ai activities typically list a number of decisions they will accept.
         *
         * Passing <c>null</c> as the parameter value will result in the default
         * defined by the ActivityDecision class for this type of request being chosen.
         * </param>
         */
        public void TakeDecision(object decisionData) {
            CoachActivityRuntime.TakeDecision(Id, decisionData);
        }

        /**
         * <summary>Replies to this request by choosing the default decision.</summary>
         */
        public void ChooseDefault() {
            CoachActivityRuntime.TakeDecision(Id, null);
        }
    }
}
