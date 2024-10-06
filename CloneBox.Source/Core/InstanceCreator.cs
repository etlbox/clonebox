using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CloneBox {
    internal static class InstanceCreator {

        public static object CreateInstance(Type type, CloneSettings cloneSettings, object sourceObject = null) {
            object newInstance = null;
            if (type == null) return null;
            if (type.IsArray(sourceObject)) {
                newInstance = CreateArrayInstance(type, sourceObject);
            } else if (sourceObject != null && sourceObject is Delegate) {
                return (sourceObject as Delegate).Clone();
            } else {
                newInstance = CreateObjectInstance(type, cloneSettings);
            }
            return newInstance;
        }


        private static object CreateObjectInstance(Type type, CloneSettings cloneSettings) {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            bool hasDefaultConstructor = type.GetConstructor(Type.EmptyTypes) != null;
            try {
                if (hasDefaultConstructor)
                    return Activator.CreateInstance(type);

            } catch {
                cloneSettings.Logger?.LogDebug("No default constructor found for '{typeName}' - trying to use other constructors using default values.", type.Name);
            }

            var bindingFlags = cloneSettings.ConstructorBindings;
            var constructors = type.GetConstructors(bindingFlags);
            foreach (var constructor in constructors) {
                try {
                    var parameters = constructor.GetParameters();
                    var param = new List<object>();
                    foreach (var p in parameters)
                        param.Add(CreateInstance(p.ParameterType, cloneSettings));
                    var newList = Activator.CreateInstance(type, bindingAttr: bindingFlags, binder: null, args: param.ToArray(), culture: null);
                    return newList;
                } catch {
                    cloneSettings.Logger?.LogDebug("Constructor {rank} for type '{typeName}' failed using default values as parameter", constructors.Rank, type.Name);

                }
            }
            return null;

        }

        class ArrayDimensions {
            public List<int> Lengths { get; set; } = new List<int>();
            public List<int> LowerBounds { get; set; } = new List<int>();

        }

        private static object CreateArrayInstance(Type type, object sourceObject = null) {
            Array sourceArray = sourceObject as Array;
            if (sourceArray == null || sourceArray.Rank == 0) return Create0Array(type);
            ArrayDimensions dimensions = DetermineArrayDimensions(sourceArray);
            return Array.CreateInstance(type.GetElementType(), dimensions.Lengths.ToArray(), dimensions.LowerBounds.ToArray());

            object Create0Array(Type type) {
                return Activator.CreateInstance(type, new object[] { 0 });
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

