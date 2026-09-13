using System;

namespace CloneBox {
    internal readonly struct ClonerKey : IEquatable<ClonerKey> {
        public readonly Type Type;
        public readonly int Flags;

        public ClonerKey(Type type, CloneSettings settings) {
            Type = type;
            Flags = Pack(settings);
        }

        public bool Equals(ClonerKey other) => Type == other.Type && Flags == other.Flags;

        public override bool Equals(object obj) => obj is ClonerKey other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ Flags;
            }
        }

        internal static int Pack(CloneSettings settings) {
            int flags = 0;
            if (settings.IncludePublicProperties) flags |= 1;
            if (settings.IncludePublicFields) flags |= 2;
            if (settings.IncludeNonPublicProperties) flags |= 4;
            if (settings.IncludeNonPublicFields) flags |= 8;
            if (settings.IncludePublicConstructors) flags |= 16;
            if (settings.IncludeNonPublicConstructors) flags |= 32;
            if (settings.UseICloneableClone) flags |= 64;
            if (settings.DoNotCloneClass != null) flags |= 128;
            if (settings.DoNotCloneProperty != null) flags |= 256;
            if (settings.DoNotCloneField != null) flags |= 512;
            return flags;
        }
    }

    internal readonly struct CopierKey : IEquatable<CopierKey> {
        public readonly Type SourceType;
        public readonly Type TargetType;
        public readonly int Flags;

        public CopierKey(Type sourceType, Type targetType, CloneSettings settings) {
            SourceType = sourceType;
            TargetType = targetType;
            Flags = ClonerKey.Pack(settings);
        }

        public bool Equals(CopierKey other) =>
            SourceType == other.SourceType && TargetType == other.TargetType && Flags == other.Flags;

        public override bool Equals(object obj) => obj is CopierKey other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                int hash = SourceType != null ? SourceType.GetHashCode() : 0;
                hash = (hash * 397) ^ (TargetType != null ? TargetType.GetHashCode() : 0);
                return (hash * 397) ^ Flags;
            }
        }
    }
}
