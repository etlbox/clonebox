using System;

namespace CloneBox {
    public static class CloneXExtensions {


        public static T CloneX<T>(this T sourceObject)
            => CloneXTo<T, T>(sourceObject, new CloneSettings());

        public static T CloneX<T>(this T sourceObject, CloneSettings settings)
           => CloneXTo<T, T>(sourceObject, settings);

        public static TOut CloneXTo<TIn, TOut>(this TIn sourceObject)
            => CloneXTo<TIn, TOut>(sourceObject, new CloneSettings());

        public static TOut CloneXTo<TIn, TOut>(this TIn sourceObject, TOut destinationObject)
            => CloneXTo<TIn, TOut>(sourceObject, destinationObject, new CloneSettings());

        public static TOut CloneXTo<TIn, TOut>(this TIn sourceObject, TOut destinationObject, CloneSettings settings) {
            CheckInputParameter(sourceObject);
            var cloneProvider = new CloneProvider(settings);
            return (TOut)cloneProvider.CloneToInternal(sourceObject, destinationObject);
        }

        public static TOut CloneXTo<TIn, TOut>(this TIn sourceObject, CloneSettings settings) {
            var cloneProvider = new CloneProvider(settings);
            return (TOut)cloneProvider.CloneInternal(sourceObject);
        }

        private static void CheckInputParameter(object sourceObject) {
            if (sourceObject == null)
                throw new InvalidOperationException("Cannot clone a null object.");
        }

        public static T CreateInstance<T>() {
            var instanceCreator = new InstanceCreator();
            return (T)instanceCreator.CreateInstance(typeof(T));
        }

        public static T CreateInstance<T>(T templateObject) {
            var instanceCreator = new InstanceCreator();
            return (T)instanceCreator.CreateInstance(typeof(T), templateObject);
        }

    }
}
