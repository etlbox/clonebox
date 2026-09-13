using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloneBox {
    internal static class CloneRuntime {
        private static readonly MethodInfo CloneItemMethod = typeof(CloneRuntime).GetMethod(nameof(CloneItem));
        private static readonly MethodInfo RegisterMethod = typeof(CloneRuntime).GetMethod(nameof(Register));
        private static readonly MethodInfo CreateInstanceMethod = typeof(CloneRuntime).GetMethod(nameof(CreateInstance));
        private static readonly MethodInfo TrySetPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(TrySetProperty));
        private static readonly MethodInfo TrySetFieldMethod = typeof(CloneRuntime).GetMethod(nameof(TrySetField));
        private static readonly MethodInfo ShouldSkipPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipProperty));
        private static readonly MethodInfo ShouldSkipFieldMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipField));
        private static readonly MethodInfo ShouldSkipClassMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipClass));
        private static readonly MethodInfo GetDynamicValueMethod = typeof(CloneRuntime).GetMethod(nameof(GetDynamicValue));
        private static readonly MethodInfo FillEnumerableMethod = typeof(CloneRuntime).GetMethod(nameof(FillEnumerable));
        private static readonly MethodInfo FillDictionaryMethod = typeof(CloneRuntime).GetMethod(nameof(FillDictionary));
        private static readonly MethodInfo FillDynamicDictionaryMethod = typeof(CloneRuntime).GetMethod(nameof(FillDynamicDictionary));
        private static readonly MethodInfo FillArrayMethod = typeof(CloneRuntime).GetMethod(nameof(FillArray));
        private static readonly MethodInfo CloneIndexedPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(CloneIndexedProperty));

        public static MethodInfo CloneItemInfo => CloneItemMethod;
        public static MethodInfo RegisterInfo => RegisterMethod;
        public static MethodInfo CreateInstanceInfo => CreateInstanceMethod;
        public static MethodInfo TrySetPropertyInfo => TrySetPropertyMethod;
        public static MethodInfo TrySetFieldInfo => TrySetFieldMethod;
        public static MethodInfo ShouldSkipPropertyInfo => ShouldSkipPropertyMethod;
        public static MethodInfo ShouldSkipFieldInfo => ShouldSkipFieldMethod;
        public static MethodInfo ShouldSkipClassInfo => ShouldSkipClassMethod;
        public static MethodInfo GetDynamicValueInfo => GetDynamicValueMethod;
        public static MethodInfo FillEnumerableInfo => FillEnumerableMethod;
        public static MethodInfo FillDictionaryInfo => FillDictionaryMethod;
        public static MethodInfo FillDynamicDictionaryInfo => FillDynamicDictionaryMethod;
        public static MethodInfo FillArrayInfo => FillArrayMethod;
        public static MethodInfo CloneIndexedPropertyInfo => CloneIndexedPropertyMethod;

        public static object CloneItem(object item, CloneProvider provider) => provider.CloneInternal(item);

        public static void Register(object source, object clone, CloneProvider provider) {
            provider.ExistingClones.Add(source, clone);
        }

        public static object CreateInstance(Type type, object source, CloneProvider provider) {
            return provider.InstanceCreator.CreateInstance(type, source);
        }

        public static bool ShouldSkipClass(Type type, CloneProvider provider) {
            return provider.CloneSettings.DoNotCloneClassInternal(type);
        }

        public static bool ShouldSkipProperty(PropertyInfo property, CloneProvider provider) {
            return provider.CloneSettings.DoNotClonePropertyInternal(property);
        }

        public static bool ShouldSkipField(FieldInfo field, CloneProvider provider) {
            return provider.CloneSettings.DoNotCloneFieldInternal(field);
        }

        public static void TrySetProperty(PropertyInfo property, object target, object value) {
            try {
                property.SetValue(target, value);
            } catch {
            }
        }

        public static void TrySetField(FieldInfo field, object target, object value) {
            try {
                field.SetValue(target, value);
            } catch {
            }
        }

        public static object GetDynamicValue(object source, string name) {
            return (source as IDictionary<string, object>).GetValueOrNull(name);
        }

        public static void CloneIndexedProperty(object source, object target, PropertyInfo sourceProperty, PropertyInfo targetProperty, CloneProvider provider) {
            var parameters = sourceProperty.GetIndexParameters();
            for (int i = 0; i < parameters.Length; i++) {
                var index = new object[] { i };
                object cloned = provider.CloneInternal(sourceProperty.GetValue(source, index));
                targetProperty.SetValue(target, cloned, index);
            }
        }

        public static object FillEnumerable(object source, object target, Type targetType, CloneProvider provider) {
            var enumerable = (IEnumerable)source;
            if (!HasEntries(source, targetType, enumerable))
                return target;
            try {
                MethodInfo addMethod = targetType.DetermineAddMethod();
                foreach (var item in enumerable) {
                    var element = provider.CloneInternal(item);
                    addMethod.Invoke(target, new[] { element });
                }
            } catch {
                provider.CloneSettings.Logger?.LogDebug("Could not copy data into enumerable of type '{typeName}' - is this enumerable readonly?", targetType.Name);
                return source;
            }
            return target;
        }

        public static object FillDictionary(object source, object target, CloneProvider provider) {
            var targetDict = target as IDictionary;
            var sourceDict = source as IDictionary;
            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, provider.CloneInternal(sourceDict[key]));
            return targetDict;
        }

        public static object FillDynamicDictionary(object source, object target, CloneProvider provider) {
            var targetDict = target as IDictionary<string, object>;
            var sourceDict = source.ToDictionary();
            foreach (var key in sourceDict.Keys)
                targetDict?.Add(key, provider.CloneInternal(sourceDict[key]));
            return targetDict;
        }

        public static T[] CopyPrimitiveArray<T>(T[] source, T[] target) {
            var length = Math.Min(source.Length, target.Length);
            Array.Copy(source, target, length);
            return target;
        }

        public static T[] CopyReferenceArray<T>(T[] source, T[] target, CloneProvider provider) {
            var length = Math.Min(source.Length, target.Length);
            for (int i = 0; i < length; i++)
                target[i] = (T)provider.CloneInternal(source[i]);
            return target;
        }

        public static List<T> CopyList<T>(List<T> source, List<T> target, CloneProvider provider) {
            if (source.Count == 0)
                return target;
            if (target.Capacity < source.Count)
                target.Capacity = source.Count;
            for (int i = 0; i < source.Count; i++)
                target.Add((T)provider.CloneInternal(source[i]));
            return target;
        }

        public static Dictionary<TKey, TValue> CopyGenericDictionary<TKey, TValue>(Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> target, CloneProvider provider) {
            foreach (var pair in source)
                target.Add(pair.Key, (TValue)provider.CloneInternal(pair.Value));
            return target;
        }

        public static object FillArray(object source, object target, CloneProvider provider) {
            var sourceArray = source as Array;
            var targetArray = target as Array;
            int[] indices = new int[targetArray.Rank];
            SetValues(targetArray, 0);
            return targetArray;

            void SetValues(Array array, int dimension) {
                if (dimension == array.Rank) {
                    if (IsWithinSourceArrayBounds(sourceArray, indices)) {
                        var clonedValue = provider.CloneInternal(sourceArray.GetValue(indices));
                        array.SetValue(clonedValue, indices);
                    }
                    return;
                }

                for (int i = array.GetLowerBound(dimension); i <= array.GetUpperBound(dimension); i++) {
                    indices[dimension] = i;
                    SetValues(array, dimension + 1);
                }
            }

            bool IsWithinSourceArrayBounds(Array sourceArray, int[] currentIndices) {
                for (int dim = 0; dim < sourceArray.Rank; dim++) {
                    if (currentIndices[dim] < sourceArray.GetLowerBound(dim) || currentIndices[dim] > sourceArray.GetUpperBound(dim))
                        return false;
                }
                return true;
            }
        }

        private static bool HasEntries(object source, Type targetType, IEnumerable enumerable) {
            if (typeof(ICollection).IsAssignableFrom(targetType))
                return ((ICollection)source).Count > 0;
            return enumerable.Cast<object>().Any();
        }
    }
}
