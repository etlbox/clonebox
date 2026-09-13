using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CloneBox {
    internal static class ReflectionExtensions {
        public static bool IsRealPrimitive(this Type type) {
            if (type == null) return false;
            if (type.IsPrimitive || type.IsEnum) return true;
            if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
                || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid)
                || type == typeof(DBNull))
                return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return type.GetGenericArguments()[0].IsRealPrimitive();
            return false;
        }

        public static bool IsIdentityClone(this Type type) {
            var ns = type?.Namespace;
            if (ns == null) return false;
            if (ns == "System.Collections.Frozen") return true;
            if (ns != "System" && !ns.StartsWith("System.Reflection", StringComparison.Ordinal)) return false;
            return typeof(MemberInfo).IsAssignableFrom(type)
                || typeof(Assembly).IsAssignableFrom(type)
                || typeof(Module).IsAssignableFrom(type);
        }

        public static bool IsReadOnlyCollection(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>);

        public static bool IsReadOnlyDictionary(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>);

        public static bool IsBitArray(this Type type) => type == typeof(BitArray);

        public static bool IsNameValueCollection(this Type type) => type == typeof(NameValueCollection);

        public static bool IsDynamicDictionary(this Type type)
            => typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type)
               && typeof(IDictionary<string, object>).IsAssignableFrom(type);

        public static bool IsIEnumerable(this Type type)
            => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

        public static MethodInfo DetermineAddMethod(this Type targetType) {
            var add = GetSingleParameterInstanceMethod(targetType, "Add")
                ?? GetSingleParameterInstanceMethod(targetType, "AddLast")
                ?? GetSingleParameterInstanceMethod(targetType, "Enqueue")
                ?? GetSingleParameterInstanceMethod(targetType, "Push");
            if (add != null)
                return add;
            foreach (var iface in targetType.GetInterfaces()) {
                if (!iface.IsGenericType)
                    continue;
                var definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(ICollection<>) || definition == typeof(IList<>)) {
                    var ifaceAdd = iface.GetMethod("Add");
                    if (ifaceAdd != null)
                        return ifaceAdd;
                }
            }
            return null;
        }

        private static MethodInfo GetSingleParameterInstanceMethod(Type type, string name) {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == 1);
        }

        internal static string GetBackingFieldName(string propertyName)
            => "<" + propertyName + ">k__BackingField";
    }

    internal sealed class ReferenceComparer : IEqualityComparer<object> {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
