using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace CloneBox {
    internal static class ReflectionExtensions {
        public static bool IsRealPrimitive(this Type type) {
            if (type == null) return false;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(DBNull);
        }

        public static bool IsIdentityClone(this Type type) {
            if (type == null) return false;
            if (typeof(MemberInfo).IsAssignableFrom(type)) return true;
            if (typeof(Assembly).IsAssignableFrom(type)) return true;
            if (typeof(Module).IsAssignableFrom(type)) return true;
            return type.Namespace == "System.Collections.Frozen";
        }

        public static bool IsReadOnlyCollection(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>);

        public static bool IsReadOnlyDictionary(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>);

        public static bool IsBitArray(this Type type) => type == typeof(BitArray);

        public static bool IsNameValueCollection(this Type type) => type == typeof(NameValueCollection);

        public static bool IsDynamic(this Type type, object obj)
            => (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type) && (obj == null || obj is IDictionary<string, object>));

        public static bool IsArray(this Type type, object obj)
          => (type.IsArray && (obj == null || obj is Array));

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

        //See also: https://stackoverflow.com/questions/8817070/is-it-possible-to-access-backing-fields-behind-auto-implemented-properties
        //Will likely work only in c#
        internal static string GetBackingFieldName(PropFieldInfo prop) {
            return string.Format("<{0}>k__BackingField", prop.Name);
        }


    }
}
