using System.Collections.Generic;
using UnityEngine;

namespace CoachAiEngine {

    public abstract class CoachActivity : ScriptableObject {
        public abstract string ActivityId { get; }
        public abstract Dictionary<string, object> Parameters { get; }
        public virtual string Variant => string.Empty;
        abstract public List<string> GetAvailableEvents();
    }
}
