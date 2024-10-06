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
            return (TOut)CloneProvider.CloneToInternal(sourceObject, destinationObject, CloneState.GetInstance(settings));
        }

        public static TOut CloneXTo<TIn, TOut>(this TIn sourceObject, CloneSettings settings) {            
            return (TOut)CloneProvider.CloneInternal(sourceObject, CloneState.GetInstance(settings));
        }

        private static void CheckInputParameter(object sourceObject) {
            if (sourceObject == null)
                throw new InvalidOperationException("Cannot clone a null object.");
        }

        public static T CreateInstance<T>() {            
            return (T)InstanceCreator.CreateInstance(typeof(T), new CloneSettings());
        }

        public static T CreateInstance<T>(T templateObject) {            
            return (T)InstanceCreator.CreateInstance(typeof(T), new CloneSettings(),  templateObject);
        }

    }
}
