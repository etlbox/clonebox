using System;
using System.Collections.Generic;

namespace CloneBox {

    internal class CloneProvider {

        internal CloneSettings CloneSettings { get; }
        internal readonly Dictionary<object, object> ExistingClones = new Dictionary<object, object>(ReferenceComparer.Instance);
        internal readonly InstanceCreator InstanceCreator;

        public CloneProvider(CloneSettings cloneSettings) {
            CloneSettings = cloneSettings ?? new CloneSettings();
            InstanceCreator = new InstanceCreator { CloneSettings = CloneSettings };
        }

        internal object CloneInternal(object sourceObject) {
            if (sourceObject == null) return null;
            var type = sourceObject.GetType();
            if (type.IsRealPrimitive() || type.IsIdentityClone()) return sourceObject;
            if (ExistingClones.TryGetValue(sourceObject, out var existing)) return existing;
            if (CloneSettings.UseICloneableClone && sourceObject is ICloneable && !(sourceObject is Delegate) && !(sourceObject is Array)) {
                var iclone = ((ICloneable)sourceObject).Clone();
                ExistingClones.Add(sourceObject, iclone);
                return iclone;
            }
            var cloner = ExpressionCloner.GetCloner(type, CloneSettings);
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
