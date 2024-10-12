﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloneBox {

    internal static class CloneProvider {
        internal static object CloneInternal(object sourceObject, CloneState state) {
            if (sourceObject == null) return null;
            if (sourceObject.GetType().IsRealPrimitive()) return sourceObject;
            if (state.ExistingClones.ContainsKey(sourceObject)) return state.ExistingClones[sourceObject];
            if (state.Settings.UseICloneableClone && sourceObject is ICloneable && !(sourceObject is Delegate) && !(sourceObject is Array)) {
                var iclone = ((ICloneable)sourceObject).Clone();
                state.ExistingClones.Add(sourceObject, iclone);
                return iclone;
            } else {
                var targetInst = InstanceCreator.CreateInstance(sourceObject.GetType(), state.Settings, sourceObject);
                state.ExistingClones.Add(sourceObject, targetInst);
                return CloneToInternal(sourceObject, targetInst, state);
            }
        }

        internal static object CloneToInternal(object sourceObject, object targetObject, CloneState state) {
            if (targetObject == null) return null;
            var targetType = targetObject.GetType();
            if (state.Settings.DoNotCloneClassInternal(targetType)) return null;
            if (targetType.IsRealPrimitive()) {
                targetObject = sourceObject;
            } else if (typeof(IDictionary).IsAssignableFrom(targetType)) {
                return FillDictionary(sourceObject, targetObject, state);
            } else if (typeof(IDictionary<string, object>).IsAssignableFrom(targetType)) {
                return FillDynamicDictionary(sourceObject, targetObject, state);
            } else if (targetType.IsArray(targetObject)) {
                return FillArray(sourceObject, targetObject, state);
            } else if (targetType.IsIEnumerable()) {
                return FillList(sourceObject, targetObject, targetType, state);
            } else {
                CopyPropertiesAndFields(sourceObject, targetObject, state);
            }

            return targetObject;
        }

        private static void CopyPropertiesAndFields(object sourceObject, object targetObject, CloneState state) {
            var allProperties = PropFieldInfo.GetAllProperties(targetObject.GetType(), state.Settings);
            HashSet<string> ignoreRelatedBackingField = new HashSet<string>();
            foreach (var prop in allProperties) {
                if (prop.DoNotClone) ignoreRelatedBackingField.TryAdd(ReflectionExtensions.GetBackingFieldName(prop));
                if (prop.CanBeCloned && prop.CanRead && prop.CanWrite)
                    TryClonePropField(sourceObject, targetObject, prop, state);
            }
            foreach (var field in PropFieldInfo.GetAllFields(targetObject.GetType(), state.Settings)) {
                if (field.CanBeCloned && field.CanRead && field.CanWrite && !ignoreRelatedBackingField.Contains(field.Name))
                    TryClonePropField(sourceObject, targetObject, field, state);
            }
        }


        private static void TryClonePropField(object sourceObject, object targetObject, PropFieldInfo targetPropField, CloneState state) {
            PropFieldInfo sourcePropField = PropFieldInfo.MatchingPropField(targetPropField.MemberType, sourceObject, targetPropField.Name, state.Settings);
            if (sourcePropField?.Type == null) return;

            var paramInfo = sourcePropField.TryGetIndexedParameters();
            if (sourcePropField.MemberType == MemberType.Property && (paramInfo?.Length ?? 0) > 0) {
                for (int i = 0; i < paramInfo.Length; i++) {
                    var index = new object[] { i };
                    object propFieldClone = CloneInternal(sourcePropField.PropInfo.GetValue(sourceObject, index), state);
                    targetPropField.PropInfo.SetValue(targetObject, propFieldClone, index);
                }
            } else {
                object propFieldClone = CloneInternal(sourcePropField.GetValue(sourceObject), state);
                targetPropField.SetValue(targetObject, propFieldClone);
            }

        }


        private static object FillList(object sourceObject, object targetInst, Type targetType, CloneState state) {
            var enumerable = (IEnumerable)sourceObject;
            if (HasEntriesInEnumerable(sourceObject, targetType, enumerable)) {
                try {
                    MethodInfo addMethod = targetType.DetermineAddMethod();
                    foreach (var item in enumerable) {
                        var element = CloneInternal(item,state);
                        addMethod.Invoke(targetInst, new[] { element });
                    }
                } catch {
                    state.Settings.Logger?.LogDebug("Could not copy data into enumerable of type '{typeName}' - is this enumerable readonly?", targetType.Name);
                    return sourceObject;
                }
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

        private static object FillDictionary(object sourceObject, object targetInst, CloneState state) {
            var targetDict = targetInst as IDictionary;
            var sourceDict = sourceObject as IDictionary;
            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, CloneInternal(sourceDict[key], state));

            return targetDict;
        }

        private static object FillDynamicDictionary(object sourceObject, object targetInst, CloneState state) {
            var targetDict = targetInst as IDictionary<string, object>;
            var sourceDict = sourceObject.ToDictionary();

            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, CloneInternal(sourceDict[key], state));

            return targetDict;
        }


        private static object FillArray(object sourceObject, object targetObject, CloneState state) {
            var sourceArray = sourceObject as Array;
            var targetArray = targetObject as Array;

            int[] indices = new int[targetArray.Rank];

            SetValues(targetArray, 0);

            void SetValues(Array array, int dimension) {
                if (dimension == array.Rank) {
                    if (IsWithinSourceArrayBounds(sourceArray, indices)) {
                        var curValue = sourceArray.GetValue(indices);
                        var clonedValue = CloneInternal(curValue, state);
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
