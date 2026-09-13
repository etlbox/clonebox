using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CloneBox {
    internal class InstanceCreator {

        public CloneSettings CloneSettings { get; set; } = new CloneSettings();

        public object CreateInstance(Type type, object sourceObject = null) {
            if (type == null)
                return null;
            if (type.IsArray)
                return CreateArrayInstance(type, sourceObject);
            if (sourceObject is Delegate del)
                return del.Clone();
            return CreateObjectInstance(type, sourceObject);
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

            try {
                if (type.GetConstructor(Type.EmptyTypes) != null)
                    return Activator.CreateInstance(type);
            } catch {
                CloneSettings.Logger?.LogDebug("No default constructor found for '{typeName}' - trying to use other constructors using default values.", type.Name);
            }

            var bindingFlags = CloneSettings.ConstructorBindings;
            foreach (var constructor in type.GetConstructors(bindingFlags)) {
                try {
                    var args = new List<object>();
                    foreach (var parameter in constructor.GetParameters())
                        args.Add(CreateInstance(parameter.ParameterType));
                    return Activator.CreateInstance(type, bindingAttr: bindingFlags, binder: null, args: args.ToArray(), culture: null);
                } catch {
                    CloneSettings.Logger?.LogDebug("Constructor for type '{typeName}' failed using default values as parameter", type.Name);
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

        private static object CreateArrayInstance(Type type, object sourceObject = null) {
            var sourceArray = sourceObject as Array;
            if (sourceArray == null || sourceArray.Rank == 0)
                return Array.CreateInstance(type.GetElementType(), new int[type.GetArrayRank()]);

            var lengths = new int[sourceArray.Rank];
            var lowerBounds = new int[sourceArray.Rank];
            for (var dimension = 0; dimension < sourceArray.Rank; dimension++) {
                lengths[dimension] = sourceArray.GetLength(dimension);
                lowerBounds[dimension] = sourceArray.GetLowerBound(dimension);
            }
            return Array.CreateInstance(type.GetElementType(), lengths, lowerBounds);
        }
    }
}
