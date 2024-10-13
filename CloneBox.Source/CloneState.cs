using System;
using System.Collections.Generic;
using System.Text;

namespace CloneBox {
    internal class CloneState {
        private static CloneState _instance;
        public static CloneState GetInstance(CloneSettings settings = null) {
            if (settings?.PreCompileCloners == true)
                return _instance ?? (_instance = new CloneState(settings));
            else 
                return new CloneState(settings);
        }

        public CloneSettings Settings { get; set; }

        public CloneState() {
            CloneSettings settings = new CloneSettings();
        }

        public CloneState(CloneSettings settings) {
            Settings = settings;
        }


        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();

        static internal Dictionary<Tuple<Type,Type>, Action<object, object, CloneState>> ExistingCloners = new Dictionary<Tuple<Type,Type>, Action<object, object, CloneState>>();

        public void AddExistingClone(object from, object to) {
            if (!from.GetType().DoReturnReference() && !ExistingClones.ContainsKey(from))
                ExistingClones.Add(from, to);
        }

        public object GetExistingClone(object from) {
            return ExistingClones[from];
        }
    }
}
