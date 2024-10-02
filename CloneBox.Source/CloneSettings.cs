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
    }
}
