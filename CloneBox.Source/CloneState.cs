using System;
using System.Collections.Generic;
using System.Text;

namespace CloneBox {
    internal class CloneState {
        public CloneSettings Settings { get; set; }

        public CloneState() {
            CloneSettings settings = new CloneSettings();
        }

        public CloneState(CloneSettings settings) {
            Settings = settings;
        }


        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();

        public void AddExistingClone(object from, object to) {
            if (!from.GetType().IsRealPrimitive() && !ExistingClones.ContainsKey(from))
                ExistingClones.Add(from, to);
        }

        public object GetExistingClone(object from) {
            return ExistingClones[from];
        }
    }
}
