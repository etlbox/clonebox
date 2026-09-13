using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CloneBox {
    internal class InstanceCreator {

        public CloneSettings CloneSettings { get; set; } = new CloneSettings();
        public object CreateInstance(Type type, object sourceObject = null) {
            object newInstance = null;
            if (type == null) return null;
            if (type.IsArray(sourceObject)) {
                newInstance = CreateArrayInstance(type, sourceObject);
            } else if (sourceObject != null && sourceObject is Delegate) {
                return (sourceObject as Delegate).Clone();
            } else {
                newInstance = CreateObjectInstance(type, sourceObject);
            }
            return newInstance;
        }


        private object CreateObjectInstance(Type type, object sourceObject = null) {
            var withComparer = TryCreateCollectionWithComparer(type, sourceObject);
            if (withComparer != null)
                return withComparer;
            var special = TryCreateSpecialCollection(type);
            if (special != null)
                return special;
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            bool hasDefaultConstructor = type.GetConstructor(Type.EmptyTypes) != null;
            try {
                if (hasDefaultConstructor)
                    return Activator.CreateInstance(type);

            } catch {
                CloneSettings.Logger?.LogDebug("No default constructor found for '{typeName}' - trying to use other constructors using default values.", type.Name);
            }

            var bindingFlags = CloneSettings.ConstructorBindings;
            var constructors = type.GetConstructors(bindingFlags);
            foreach (var constructor in constructors) {
                try {
                    var parameters = constructor.GetParameters();
                    var param = new List<object>();
                    foreach (var p in parameters)
                        param.Add(CreateInstance(p.ParameterType));
                    var newList = Activator.CreateInstance(type, bindingAttr: bindingFlags, binder: null, args: param.ToArray(), culture: null);
                    return newList;
                } catch {
                    CloneSettings.Logger?.LogDebug("Constructor {rank} for type '{typeName}' failed using default values as parameter", constructors.Rank, type.Name);

                }
            }
            if (!CloneSettings.IncludeNonPublicConstructors)
                return null;
            try {
#if NET
                return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
#else
                return FormatterServices.GetUninitializedObject(type);
#endif
            } catch {
                return null;
            }

        }

        private static object TryCreateCollectionWithComparer(Type type, object sourceObject) {
            if (sourceObject == null || sourceObject.GetType() != type)
                return null;
            var comparerProperty = type.GetProperty("Comparer");
            if (comparerProperty == null)
                return null;
            var comparer = comparerProperty.GetValue(sourceObject);
            if (comparer == null)
                return null;
            foreach (var constructor in type.GetConstructors()) {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(comparer))
                    return constructor.Invoke(new[] { comparer });
            }
            return null;
        }

        private static object TryCreateSpecialCollection(Type type) {
            if (type.IsReadOnlyCollection()) {
                var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
                return Activator.CreateInstance(type, list);
            }
            if (type.IsReadOnlyDictionary()) {
                var args = type.GetGenericArguments();
                var dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
                return Activator.CreateInstance(type, dictionary);
            }
            return null;
        }

        class ArrayDimensions {
            public List<int> Lengths { get; set; } = new List<int>();
            public List<int> LowerBounds { get; set; } = new List<int>();

        }

        private object CreateArrayInstance(Type type, object sourceObject = null) {
            Array sourceArray = sourceObject as Array;
            if (sourceArray == null || sourceArray.Rank == 0) return Create0Array(type);
            ArrayDimensions dimensions = DetermineArrayDimensions(sourceArray);
            return Array.CreateInstance(type.GetElementType(), dimensions.Lengths.ToArray(), dimensions.LowerBounds.ToArray());

            object Create0Array(Type type) {
                var lengths = new int[type.GetArrayRank()];
                return Array.CreateInstance(type.GetElementType(), lengths);
            }

            ArrayDimensions DetermineArrayDimensions(Array sourceArray) {
                var arrayDimensions = new ArrayDimensions();
                for (var dimension = 0; dimension < sourceArray.Rank; dimension++) {
                    arrayDimensions.Lengths.Add(sourceArray.GetLength(dimension));
                    arrayDimensions.LowerBounds.Add(sourceArray.GetLowerBound(dimension));
                }
                return arrayDimensions;
            }
        }
    }
}

