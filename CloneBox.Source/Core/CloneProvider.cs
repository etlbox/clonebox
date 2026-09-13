using System;
using System.Collections.Generic;

namespace CloneBox {

    internal class CloneProvider {

        internal CloneSettings CloneSettings { get; set; }
        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();
        internal InstanceCreator InstanceCreator;

        public CloneProvider(CloneSettings cloneSettings) {
            if (cloneSettings == null) cloneSettings = new CloneSettings();
            Init(cloneSettings);
        }

        public void Init(CloneSettings cloneSettings) {
            CloneSettings = cloneSettings;
            InstanceCreator = new InstanceCreator() {
                CloneSettings = CloneSettings
            };
        }

        internal object CloneInternal(object sourceObject) {
            if (sourceObject == null) return null;
            if (sourceObject.GetType().IsRealPrimitive()) return sourceObject;
            if (ExistingClones.TryGetValue(sourceObject, out var existing)) return existing;
            if (CloneSettings.UseICloneableClone && sourceObject is ICloneable && !(sourceObject is Delegate) && !(sourceObject is Array)) {
                var iclone = ((ICloneable)sourceObject).Clone();
                ExistingClones.Add(sourceObject, iclone);
                return iclone;
            }
            var cloner = ExpressionCloner.GetCloner(sourceObject.GetType(), CloneSettings);
            return cloner(sourceObject, this);
        }

        internal object CloneToInternal(object sourceObject, object targetObject) {
            if (targetObject == null) return null;
            var targetType = targetObject.GetType();
            if (CloneSettings.DoNotCloneClassInternal(targetType)) return null;
            if (targetType.IsRealPrimitive())
                return sourceObject;
            var copier = ExpressionCloner.GetCopier(sourceObject.GetType(), targetType, CloneSettings);
            return copier(sourceObject, targetObject, this);
        }
    }
}
