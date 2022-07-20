// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */


using CoachAiEngine;
using UnityEngine;
using UnityEngine.UI;

namespace CoachAi.Samples {

    /// <summary>
    /// A simple helper component to update Text fields with updated <see cref="MetricsEvent"/>.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class MetricDisplayUpdater : MonoBehaviour {
        private Text _text;

        public void Awake() {
            _text = GetComponent<Text>();
        }

        /// <summary>
        /// Update the text field to reflect the metric's value
        /// </summary>
        /// <param name="event"></param>
        public void UpdateValue(MetricsEvent @event) {
            _text.text = $"{@event.Value}";
        }
    }

}
