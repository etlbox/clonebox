using System;
using System.Collections.Generic;
using System.Text;

namespace CloneBox {
    internal class CloneState {
        private static CloneState _instance;
        public static CloneState GetInstance(CloneSettings settings = null) {
            return _instance ?? (_instance = new CloneState(settings));
        }

        public CloneSettings Settings { get; set; }

        public CloneState() {
            CloneSettings settings = new CloneSettings();
        }

        public CloneState(CloneSettings settings) {
            Settings = settings;
        }


        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();

        internal Dictionary<Type, Func<object, object, CloneState, object>> ExistingCloners = new Dictionary<Type, Func<object, object, CloneState, object>>();

        public void AddExistingClone(object from, object to) {
            if (!from.GetType().IsRealPrimitive() && !ExistingClones.ContainsKey(from))
                ExistingClones.Add(from, to);
        }

        public object GetExistingClone(object from) {
            return ExistingClones[from];
        }
    }
}
