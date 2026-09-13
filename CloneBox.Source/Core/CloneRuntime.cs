using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace CloneBox {
    internal static class CloneRuntime {
        private static readonly MethodInfo CloneItemMethod = typeof(CloneRuntime).GetMethod(nameof(CloneItem));
        private static readonly MethodInfo RegisterMethod = typeof(CloneRuntime).GetMethod(nameof(Register));
        private static readonly MethodInfo CreateInstanceMethod = typeof(CloneRuntime).GetMethod(nameof(CreateInstance));
        private static readonly MethodInfo TrySetPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(TrySetProperty));
        private static readonly MethodInfo TrySetFieldMethod = typeof(CloneRuntime).GetMethod(nameof(TrySetField));
        private static readonly MethodInfo CopyPointerFieldMethod = typeof(CloneRuntime).GetMethod(nameof(CopyPointerField));
        private static readonly MethodInfo ShouldSkipPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipProperty));
        private static readonly MethodInfo ShouldSkipFieldMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipField));
        private static readonly MethodInfo ShouldSkipClassMethod = typeof(CloneRuntime).GetMethod(nameof(ShouldSkipClass));
        private static readonly MethodInfo GetDynamicValueMethod = typeof(CloneRuntime).GetMethod(nameof(GetDynamicValue));
        private static readonly MethodInfo FillEnumerableMethod = typeof(CloneRuntime).GetMethod(nameof(FillEnumerable));
        private static readonly MethodInfo FillDictionaryMethod = typeof(CloneRuntime).GetMethod(nameof(FillDictionary));
        private static readonly MethodInfo FillDynamicDictionaryMethod = typeof(CloneRuntime).GetMethod(nameof(FillDynamicDictionary));
        private static readonly MethodInfo FillArrayMethod = typeof(CloneRuntime).GetMethod(nameof(FillArray));
        private static readonly MethodInfo CloneIndexedPropertyMethod = typeof(CloneRuntime).GetMethod(nameof(CloneIndexedProperty));
        private static readonly MethodInfo CopyCollectionPropertiesMethod = typeof(CloneRuntime).GetMethod(nameof(CopyCollectionProperties));
        private static readonly MethodInfo CreateExceptionMethod = typeof(CloneRuntime).GetMethod(nameof(CreateException));
        private static readonly MethodInfo CopyReadOnlyCollectionMethod = typeof(CloneRuntime).GetMethod(nameof(CopyReadOnlyCollection));
        private static readonly MethodInfo CopyReadOnlyDictionaryMethod = typeof(CloneRuntime).GetMethod(nameof(CopyReadOnlyDictionary));
        private static readonly MethodInfo CopyBitArrayMethod = typeof(CloneRuntime).GetMethod(nameof(CopyBitArray));
        private static readonly MethodInfo CopyNameValueCollectionMethod = typeof(CloneRuntime).GetMethod(nameof(CopyNameValueCollection));

        public static MethodInfo CloneItemInfo => CloneItemMethod;
        public static MethodInfo RegisterInfo => RegisterMethod;
        public static MethodInfo CreateInstanceInfo => CreateInstanceMethod;
        public static MethodInfo TrySetPropertyInfo => TrySetPropertyMethod;
        public static MethodInfo TrySetFieldInfo => TrySetFieldMethod;
        public static MethodInfo CopyPointerFieldInfo => CopyPointerFieldMethod;
        public static MethodInfo ShouldSkipPropertyInfo => ShouldSkipPropertyMethod;
        public static MethodInfo ShouldSkipFieldInfo => ShouldSkipFieldMethod;
        public static MethodInfo ShouldSkipClassInfo => ShouldSkipClassMethod;
        public static MethodInfo GetDynamicValueInfo => GetDynamicValueMethod;
        public static MethodInfo FillEnumerableInfo => FillEnumerableMethod;
        public static MethodInfo FillDictionaryInfo => FillDictionaryMethod;
        public static MethodInfo FillDynamicDictionaryInfo => FillDynamicDictionaryMethod;
        public static MethodInfo FillArrayInfo => FillArrayMethod;
        public static MethodInfo CloneIndexedPropertyInfo => CloneIndexedPropertyMethod;
        public static MethodInfo CopyCollectionPropertiesInfo => CopyCollectionPropertiesMethod;
        public static MethodInfo CreateExceptionInfo => CreateExceptionMethod;
        public static MethodInfo CopyReadOnlyCollectionInfo => CopyReadOnlyCollectionMethod;
        public static MethodInfo CopyReadOnlyDictionaryInfo => CopyReadOnlyDictionaryMethod;
        public static MethodInfo CopyBitArrayInfo => CopyBitArrayMethod;
        public static MethodInfo CopyNameValueCollectionInfo => CopyNameValueCollectionMethod;

        public static object CloneItem(object item, CloneProvider provider) => provider.CloneInternal(item);

        public static void Register(object source, object clone, CloneProvider provider) {
            provider.ExistingClones.Add(source, clone);
        }

        public static object CreateInstance(Type type, object source, CloneProvider provider) {
            return provider.InstanceCreator.CreateInstance(type, source);
        }

        public static object CreateException(object source, CloneProvider provider) {
            var exception = (Exception)source;
            var inner = exception.InnerException == null
                ? null
                : (Exception)provider.CloneInternal(exception.InnerException);
            try {
                return Activator.CreateInstance(exception.GetType(), exception.Message, inner);
            } catch {
                try {
                    return Activator.CreateInstance(exception.GetType(), exception.Message);
                } catch {
                    return provider.InstanceCreator.CreateInstance(exception.GetType(), source);
                }
            }
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

        public static void CopyPointerField(FieldInfo field, object source, object target) {
            try {
                field.SetValue(target, field.GetValue(source));
            } catch {
            }
        }

        public static object GetDynamicValue(object source, string name) {
            return (source as IDictionary<string, object>).GetValueOrNull(name);
        }

        public static void CloneIndexedProperty(object source, object target, PropertyInfo sourceProperty, PropertyInfo targetProperty, CloneProvider provider) {
            try {
                var parameters = sourceProperty.GetIndexParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(int))
                    return;
                var index = new object[] { 0 };
                object cloned = provider.CloneInternal(sourceProperty.GetValue(source, index));
                targetProperty.SetValue(target, cloned, index);
            } catch {
            }
        }

        public static object FillEnumerable(object source, object target, Type targetType, CloneProvider provider) {
            var enumerable = (IEnumerable)source;
            if (!HasEntries(source, targetType, enumerable))
                return target;
            try {
                MethodInfo addMethod = targetType.DetermineAddMethod();
                if (addMethod.Name == "Push") {
                    var items = enumerable.Cast<object>().Select(item => provider.CloneInternal(item)).ToList();
                    for (int i = items.Count - 1; i >= 0; i--)
                        addMethod.Invoke(target, new[] { items[i] });
                } else {
                    foreach (var item in enumerable) {
                        var element = provider.CloneInternal(item);
                        addMethod.Invoke(target, new[] { element });
                    }
                }
            } catch {
                provider.CloneSettings.Logger?.LogDebug("Could not copy data into enumerable of type '{typeName}' - is this enumerable readonly?", targetType.Name);
                return source;
            }
            return target;
        }

        public static object CopyCollectionProperties(object source, object target, CloneProvider provider) {
            if (target == null || ReferenceEquals(source, target))
                return target;
            foreach (var property in target.GetType().GetProperties(provider.CloneSettings.PropertyBindings)) {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                    continue;
                if (property.Name == "Capacity" || property.Name == "Comparer")
                    continue;
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
                    continue;
                try {
                    property.SetValue(target, provider.CloneInternal(property.GetValue(source)));
                } catch {
                }
            }
            return target;
        }

        public static object CopyReadOnlyCollection(object source, object target, CloneProvider provider) {
            var type = source.GetType();
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
            foreach (var item in (IEnumerable)source)
                list.Add(provider.CloneInternal(item));
            var clone = Activator.CreateInstance(type, list);
            provider.ExistingClones[source] = clone;
            return clone;
        }

        public static object CopyReadOnlyDictionary(object source, object target, CloneProvider provider) {
            var type = source.GetType();
            var args = type.GetGenericArguments();
            var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
            var sourceDictionary = (IDictionary)source;
            foreach (var key in sourceDictionary.Keys)
                dictionary.Add(key, provider.CloneInternal(sourceDictionary[key]));
            var clone = Activator.CreateInstance(type, dictionary);
            provider.ExistingClones[source] = clone;
            return clone;
        }

        public static object CopyBitArray(object source, object target, CloneProvider provider) {
            var clone = new BitArray((BitArray)source);
            provider.ExistingClones[source] = clone;
            return clone;
        }

        public static object CopyNameValueCollection(object source, object target, CloneProvider provider) {
            var sourceCollection = (NameValueCollection)source;
            var clone = new NameValueCollection();
            foreach (var key in sourceCollection.AllKeys) {
                var values = sourceCollection.GetValues(key);
                if (values == null)
                    clone.Add(key, null);
                else
                    foreach (var value in values)
                        clone.Add(key, value);
            }
            provider.ExistingClones[source] = clone;
            return clone;
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
            var targetArray = target as Array;
            var sourceArray = source as Array;
            if (sourceArray == null)
                return FillArrayFromEnumerable(source, targetArray, provider);
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

        private static object FillArrayFromEnumerable(object source, Array targetArray, CloneProvider provider) {
            if (targetArray.Rank != 1)
                return targetArray;
            var lower = targetArray.GetLowerBound(0);
            var upper = targetArray.GetUpperBound(0);
            if (source is IList list) {
                var length = Math.Min(list.Count, targetArray.Length);
                for (int i = 0; i < length; i++)
                    targetArray.SetValue(provider.CloneInternal(list[i]), lower + i);
                return targetArray;
            }
            var index = lower;
            foreach (var item in (IEnumerable)source) {
                if (index > upper)
                    break;
                targetArray.SetValue(provider.CloneInternal(item), index);
                index++;
            }
            return targetArray;
        }

        private static bool HasEntries(object source, Type targetType, IEnumerable enumerable) {
            if (typeof(ICollection).IsAssignableFrom(targetType))
                return ((ICollection)source).Count > 0;
            return enumerable.Cast<object>().Any();
        }
    }
}
