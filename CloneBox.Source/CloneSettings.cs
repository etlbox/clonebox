using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CloneBox {
    public sealed class CloneSettings {
        public bool IncludePublicProperties { get; set; } = true;
        public bool IncludePublicFields { get; set; } = true;
        public bool IncludeNonPublicProperties { get; set; } = true;
        public bool IncludeNonPublicFields { get; set; } = true;
        public bool IncludePublicConstructors { get; set; } = true;
        public bool IncludeNonPublicConstructors { get; set; } = true;

        public bool UseICloneableClone { get; set; } = false;
        internal BindingFlags PropertyBindings =>
            (IncludePublicProperties ? BindingFlags.Public : 0) |
            (IncludeNonPublicProperties ? BindingFlags.NonPublic : 0) |
            BindingFlags.Instance;
        internal BindingFlags FieldBindings =>
            (IncludePublicFields ? BindingFlags.Public : 0) |
            (IncludeNonPublicFields ? BindingFlags.NonPublic : 0) |
            BindingFlags.Instance;
        internal BindingFlags ConstructorBindings =>
            (IncludePublicConstructors ? BindingFlags.Public : 0) |
            (IncludeNonPublicConstructors ? BindingFlags.NonPublic : 0) |
            BindingFlags.Instance;

        public Predicate<Type> DoNotCloneClass { get; set; }
        public Predicate<PropertyInfo> DoNotCloneProperty { get; set; }
        public Predicate<FieldInfo> DoNotCloneField { get; set; }

        public ILogger Logger { get; set; }


        internal bool DoNotCloneFieldInternal(FieldInfo fieldInfo) {
            if (fieldInfo.GetCustomAttribute<DoNotClone>() != null)
                return true;
            return DoNotCloneField?.Invoke(fieldInfo) ?? false;
        }

        internal bool DoNotCloneClassInternal(Type type) {
            if (type.GetCustomAttribute<DoNotClone>() != null)
                return true;
            return DoNotCloneClass?.Invoke(type) ?? false;
        }

        internal bool DoNotClonePropertyInternal(PropertyInfo propInfo) {
            if (propInfo.GetCustomAttribute<DoNotClone>() != null)
                return true;
            return DoNotCloneProperty?.Invoke(propInfo) ?? false;
        }

        internal Dictionary<object, object> ExistingClones = new Dictionary<object, object>();
    }
}
