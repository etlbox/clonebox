using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace CloneBox {
    internal static class CloneRuntime {
        public static readonly MethodInfo CloneItemInfo = Method(nameof(CloneItem));
        public static readonly MethodInfo RegisterInfo = Method(nameof(Register));
        public static readonly MethodInfo CreateInstanceInfo = Method(nameof(CreateInstance));
        public static readonly MethodInfo TrySetPropertyInfo = Method(nameof(TrySetProperty));
        public static readonly MethodInfo TrySetFieldInfo = Method(nameof(TrySetField));
        public static readonly MethodInfo CopyPointerFieldInfo = Method(nameof(CopyPointerField));
        public static readonly MethodInfo ShouldSkipPropertyInfo = Method(nameof(ShouldSkipProperty));
        public static readonly MethodInfo ShouldSkipFieldInfo = Method(nameof(ShouldSkipField));
        public static readonly MethodInfo ShouldSkipClassInfo = Method(nameof(ShouldSkipClass));
        public static readonly MethodInfo GetDynamicValueInfo = Method(nameof(GetDynamicValue));
        public static readonly MethodInfo FillEnumerableInfo = Method(nameof(FillEnumerable));
        public static readonly MethodInfo FillDictionaryInfo = Method(nameof(FillDictionary));
        public static readonly MethodInfo FillDynamicDictionaryInfo = Method(nameof(FillDynamicDictionary));
        public static readonly MethodInfo FillArrayInfo = Method(nameof(FillArray));
        public static readonly MethodInfo CloneIndexedPropertyInfo = Method(nameof(CloneIndexedProperty));
        public static readonly MethodInfo CopyCollectionPropertiesInfo = Method(nameof(CopyCollectionProperties));
        public static readonly MethodInfo CreateExceptionInfo = Method(nameof(CreateException));
        public static readonly MethodInfo CopyReadOnlyCollectionInfo = Method(nameof(CopyReadOnlyCollection));
        public static readonly MethodInfo CopyReadOnlyDictionaryInfo = Method(nameof(CopyReadOnlyDictionary));
        public static readonly MethodInfo CopyBitArrayInfo = Method(nameof(CopyBitArray));
        public static readonly MethodInfo CopyNameValueCollectionInfo = Method(nameof(CopyNameValueCollection));

        private static MethodInfo Method(string name) => typeof(CloneRuntime).GetMethod(name);

        public static object CloneItem(object item, CloneProvider provider) => provider.CloneInternal(item);

        public static void Register(object source, object clone, CloneProvider provider) {
            provider.ExistingClones.Add(source, clone);
        }

        public static object CreateInstance(Type type, object source, CloneProvider provider) {
            return provider.InstanceCreator.CreateInstance(type, source);
        }

        public static object CreateException(object source, CloneProvider provider) {
            var exception = (Exception)source;
            var type = exception.GetType();
            var inner = exception.InnerException == null
                ? null
                : (Exception)provider.CloneInternal(exception.InnerException);

            return TryCreate(type, exception.Message, inner)
                ?? TryCreate(type, exception.Message)
                ?? provider.InstanceCreator.CreateInstance(type, source);
        }

        private static object TryCreate(Type type, params object[] args) {
            try {
                return Activator.CreateInstance(type, args);
            } catch {
                return null;
            }
        }

        public static bool ShouldSkipClass(Type type, CloneProvider provider)
            => provider.CloneSettings.DoNotCloneClassInternal(type);

        public static bool ShouldSkipProperty(PropertyInfo property, CloneProvider provider)
            => provider.CloneSettings.DoNotClonePropertyInternal(property);

        public static bool ShouldSkipField(FieldInfo field, CloneProvider provider)
            => provider.CloneSettings.DoNotCloneFieldInternal(field);

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
            if (!HasEntries(enumerable, targetType))
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

        public static object CopyReadOnlyCollection(object source, CloneProvider provider) {
            var type = source.GetType();
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
            foreach (var item in (IEnumerable)source)
                list.Add(provider.CloneInternal(item));
            return Registered(source, Activator.CreateInstance(type, list), provider);
        }

        public static object CopyReadOnlyDictionary(object source, CloneProvider provider) {
            var type = source.GetType();
            var args = type.GetGenericArguments();
            var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
            var sourceDictionary = (IDictionary)source;
            foreach (var key in sourceDictionary.Keys)
                dictionary.Add(key, provider.CloneInternal(sourceDictionary[key]));
            return Registered(source, Activator.CreateInstance(type, dictionary), provider);
        }

        public static object CopyBitArray(object source, CloneProvider provider) {
            return Registered(source, new BitArray((BitArray)source), provider);
        }

        public static object CopyNameValueCollection(object source, CloneProvider provider) {
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
            return Registered(source, clone, provider);
        }

        private static object Registered(object source, object clone, CloneProvider provider) {
            provider.ExistingClones[source] = clone;
            return clone;
        }

        public static object FillDictionary(object source, object target, CloneProvider provider) {
            if (!(target is IDictionary targetDict))
                return target;
            var sourceDict = (IDictionary)source;
            foreach (var key in sourceDict.Keys)
                targetDict.Add(key, provider.CloneInternal(sourceDict[key]));
            return targetDict;
        }

        public static object FillDynamicDictionary(object source, object target, CloneProvider provider) {
            if (!(target is IDictionary<string, object> targetDict))
                return target;
            var sourceDict = source.ToDictionary();
            foreach (var key in sourceDict.Keys)
                targetDict.Add(key, provider.CloneInternal(sourceDict[key]));
            return targetDict;
        }

        public static T[] CopyPrimitiveArray<T>(T[] source, T[] target) {
            var length = Math.Min(source.Length, target.Length);
            Array.Copy(source, target, length);
            return target;
        }

        public static T[] CopyReferenceArray<T>(T[] source, T[] target, CloneProvider provider) {
            var length = Math.Min(source.Length, target.Length);
            if (typeof(T).IsValueType) {
                for (int i = 0; i < length; i++)
                    target[i] = (T)provider.CloneInternal(source[i]);
                return target;
            }
            Func<object, CloneProvider, object> cloner = null;
            for (int i = 0; i < length; i++)
                target[i] = CloneKnown(source[i], provider, ref cloner);
            return target;
        }

        public static List<T> CopyList<T>(List<T> source, List<T> target, CloneProvider provider) {
            if (source.Count == 0)
                return target;
            if (target.Capacity < source.Count)
                target.Capacity = source.Count;
            if (typeof(T).IsRealPrimitive()) {
                target.AddRange(source);
                return target;
            }
            if (typeof(T).IsValueType) {
                for (int i = 0; i < source.Count; i++)
                    target.Add((T)provider.CloneInternal(source[i]));
                return target;
            }
            Func<object, CloneProvider, object> cloner = null;
            for (int i = 0; i < source.Count; i++)
                target.Add(CloneKnown(source[i], provider, ref cloner));
            return target;
        }

        public static Dictionary<TKey, TValue> CopyGenericDictionary<TKey, TValue>(Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> target, CloneProvider provider) {
            if (typeof(TValue).IsRealPrimitive()) {
                foreach (var pair in source)
                    target.Add(pair.Key, pair.Value);
                return target;
            }
            if (typeof(TValue).IsValueType) {
                foreach (var pair in source)
                    target.Add(pair.Key, (TValue)provider.CloneInternal(pair.Value));
                return target;
            }
            Func<object, CloneProvider, object> cloner = null;
            foreach (var pair in source)
                target.Add(pair.Key, CloneKnown(pair.Value, provider, ref cloner));
            return target;
        }

        private static T CloneKnown<T>(T item, CloneProvider provider, ref Func<object, CloneProvider, object> cloner) {
            if (item == null)
                return default;
            if (provider.ExistingClones.TryGetValue(item, out var existing))
                return (T)existing;
            if (provider.CloneSettings.UseICloneableClone || item.GetType() != typeof(T))
                return (T)provider.CloneInternal(item);
            if (cloner == null)
                cloner = ExpressionCloner.GetCloner(typeof(T), provider.CloneSettings);
            return (T)cloner(item, provider);
        }

        public static object FillArray(object source, object target, CloneProvider provider) {
            var targetArray = (Array)target;
            if (!(source is Array sourceArray))
                return FillArrayFromEnumerable(source, targetArray, provider);

            var indices = new int[targetArray.Rank];
            SetValues(0);
            return targetArray;

            void SetValues(int dimension) {
                if (dimension == targetArray.Rank) {
                    if (IsWithinSourceBounds())
                        targetArray.SetValue(provider.CloneInternal(sourceArray.GetValue(indices)), indices);
                    return;
                }
                for (int i = targetArray.GetLowerBound(dimension); i <= targetArray.GetUpperBound(dimension); i++) {
                    indices[dimension] = i;
                    SetValues(dimension + 1);
                }
            }

            bool IsWithinSourceBounds() {
                for (int dim = 0; dim < sourceArray.Rank; dim++) {
                    if (indices[dim] < sourceArray.GetLowerBound(dim) || indices[dim] > sourceArray.GetUpperBound(dim))
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

        private static bool HasEntries(IEnumerable source, Type targetType) {
            if (source is ICollection collection && typeof(ICollection).IsAssignableFrom(targetType))
                return collection.Count > 0;
            return source.Cast<object>().Any();
        }
    }
}
