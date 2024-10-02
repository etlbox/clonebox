using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;

namespace CloneBox {

    internal class CloneProvider {

        internal CloneSettings CloneSettings { get; set; }
        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();
        InstanceCreator InstanceCreator;

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
            if (ExistingClones.ContainsKey(sourceObject)) return ExistingClones[sourceObject];
            if (CloneSettings.UseICloneableClone && sourceObject is ICloneable && !(sourceObject is Delegate) && !(sourceObject is Array)) {
                var iclone = ((ICloneable)sourceObject).Clone();
                ExistingClones.Add(sourceObject, iclone);
                return iclone;
            } else {
                var targetInst = InstanceCreator.CreateInstance(sourceObject.GetType(), sourceObject);
                ExistingClones.Add(sourceObject, targetInst);
                return CloneToInternal(sourceObject, targetInst);
            }
        }

        internal object CloneToInternal(object sourceObject, object targetObject) {
            if (targetObject == null) return null;
            var targetType = targetObject.GetType();
            if (targetType.IsRealPrimitive()) {
                targetObject = sourceObject;
            } else if (typeof(IDictionary).IsAssignableFrom(targetType)) {
                return FillDictionary(sourceObject, targetObject);
            } else if (typeof(IDictionary<string, object>).IsAssignableFrom(targetType)) {
                return FillDynamicDictionary(sourceObject, targetObject);
            } else if (targetType.IsArray(targetObject)) {
                return FillArray(sourceObject, targetObject);
            } else if (targetType.IsIEnumerable()) {
                return FillList(sourceObject, targetObject, targetType);
            } else {
                CopyPropertiesAndFields(sourceObject, targetObject);
            }

            return targetObject;
        }

        private void CopyPropertiesAndFields(object sourceObject, object targetObject) {
            var allProperties = PropFieldInfo.GetAllProperties(targetObject.GetType(), CloneSettings);
            HashSet<string> backingFieldNames = new HashSet<string>();
            foreach (var prop in allProperties) {
                if (!prop.CanBeCloned) backingFieldNames.Add(string.Format("<{0}>k__BackingField", prop.Name));
                if (prop.CanBeCloned && prop.CanRead && prop.CanWrite)
                    TryClonePropField(sourceObject, targetObject, prop);
            }
            foreach (var field in PropFieldInfo.GetAllFields(targetObject.GetType(), CloneSettings)) {
                if (field.CanBeCloned && field.CanRead && field.CanWrite && !backingFieldNames.Contains(field.Name))
                    TryClonePropField(sourceObject, targetObject, field);
            }
        }


        private void TryClonePropField(object sourceObject, object targetObject, PropFieldInfo targetPropField) {
            PropFieldInfo sourcePropField = PropFieldInfo.MatchingPropField(targetPropField.MemberType, sourceObject, targetPropField.Name, CloneSettings);
            if (sourcePropField?.Type == null) return;

            var paramInfo = sourcePropField.TryGetIndexedParameters();
            if (sourcePropField.MemberType == MemberType.Property && (paramInfo?.Length ?? 0) > 0) {
                for (int i = 0; i < paramInfo.Length; i++) {
                    var index = new object[] { i };
                    object propFieldClone = CloneInternal(sourcePropField.PropInfo.GetValue(sourceObject, index));
                    targetPropField.PropInfo.SetValue(targetObject, propFieldClone, index);
                }
            } else {
                object propFieldClone = CloneInternal(sourcePropField.GetValue(sourceObject));
                targetPropField.SetValue(targetObject, propFieldClone);
            }

        }


        private object FillList(object sourceObject, object targetInst, Type targetType) {
            var enumerable = (IEnumerable)sourceObject;
            if (HasEntriesInEnumerable(sourceObject, targetType, enumerable)) {
                try {
                    MethodInfo addMethod = targetType.DetermineAddMethod();
                    foreach (var item in enumerable) {
                        var element = CloneInternal(item);
                        addMethod.Invoke(targetInst, new[] { element });
                    }
                } catch { return sourceObject; }
            }
            return targetInst;

            bool HasEntriesInEnumerable(object sourceObject, Type targetType, IEnumerable enumerable) {
                bool hasEntries;
                if (typeof(ICollection).IsAssignableFrom(targetType))
                    hasEntries = ((ICollection)sourceObject).Count > 0;
                else {
                    hasEntries = enumerable.Cast<object>().Any();
                }

                return hasEntries;
            }
        }

        private object FillDictionary(object sourceObject, object targetInst) {
            var targetDict = targetInst as IDictionary;
            var sourceDict = sourceObject as IDictionary;
            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, CloneInternal(sourceDict[key]));

            return targetDict;
        }

        private object FillDynamicDictionary(object sourceObject, object targetInst) {
            var targetDict = targetInst as IDictionary<string, object>;
            var sourceDict = sourceObject.ToDictionary();
            
            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, CloneInternal(sourceDict[key]));

            return targetDict;
        }


        private object FillArray(object sourceObject, object targetObject) {
            var sourceArray = sourceObject as Array;
            var targetArray = targetObject as Array;

            int[] indices = new int[targetArray.Rank];

            SetValues(targetArray, 0);

            void SetValues(Array array, int dimension) {
                if (dimension == array.Rank) {
                    if (IsWithinSourceArrayBounds(sourceArray, indices)) {
                        var curValue = sourceArray.GetValue(indices);
                        var clonedValue = CloneInternal(curValue);
                        array.SetValue(clonedValue, indices);
                    }
                    return;
                }

                for (int i = array.GetLowerBound(dimension); i <= array.GetUpperBound(dimension); i++) {
                    indices[dimension] = i;
                    SetValues(array, dimension + 1);
                }
            }

            bool IsWithinSourceArrayBounds(Array sourceArray, int[] indices) {
                for (int dim = 0; dim < sourceArray.Rank; dim++) {
                    if (indices[dim] < sourceArray.GetLowerBound(dim) || indices[dim] > sourceArray.GetUpperBound(dim)) {
                        return false;
                    }
                }
                return true;
            }

            return targetArray;

        }

    }
}
