using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace CloneBox {
    internal static class ReflectionExtensions {
        //See also https://stackoverflow.com/questions/1827425/how-to-check-programmatically-if-a-type-is-a-struct-or-a-class

        static HashSet<Type> PrimitiveTypes = new HashSet<Type> {
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(DBNull),
            typeof(IntPtr),
            typeof(UIntPtr),
            Type.GetType("System.RuntimeType"),
            Type.GetType("System.RuntimeTypeHandle"),
            StringComparer.Ordinal.GetType(),
            StringComparer.CurrentCulture.GetType(),
        };
        public static bool DoReturnReference(this Type type) {
            if (type == null) return false;
            return type.IsPrimitive ||
                type.IsEnum ||
                PrimitiveTypes.Contains(type) ||
                type.IsPointer ||
                type.IsCOMObject ||
                type.IsSubclassOf(typeof(System.Runtime.ConstrainedExecution.CriticalFinalizerObject)) ||
                type.FullName.StartsWith("System.Runtime.Remoting.") ||
                type.FullName.StartsWith("System.Reflection.") ||
                type.FullName.StartsWith("System.Collections.Generic.GenericEqualityComparer") ||
                type.FullName.StartsWith("System.Collections.Generic.ObjectEqualityComparer") ||
                type.FullName.StartsWith("System.Collections.Generic.EnumEqualityComparer") ||
                type.FullName.StartsWith("System.Collections.Generic.NullableEqualityComparer") ||
                type.FullName.StartsWith("System.Collections.Generic.ByteEqualityComparer");
        }

        public static bool IsDynamic(this Type type, object obj)
            => (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type) && (obj == null || obj is IDictionary<string, object>));

        public static bool IsArray(this Type type, object obj)
          => (type.IsArray && (obj == null || obj is Array));

        public static bool IsIEnumerable(this Type type)
            => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

        public static MethodInfo DetermineAddMethod(this Type targetType) {
            MethodInfo addMethod = targetType.GetMethod("Add");
            if (addMethod == null) {
                addMethod = targetType.GetMethod("Enqueue");
                if (addMethod == null) {
                    addMethod = targetType.GetMethod("Push");
                    if (addMethod == null) {
                        addMethod = targetType.GetMethods().FirstOrDefault(x => x.Name.StartsWith("Add"));
                    }
                }
            }
            return addMethod;
        }

        //See also: https://stackoverflow.com/questions/8817070/is-it-possible-to-access-backing-fields-behind-auto-implemented-properties
        //Will likely work only in c#
        internal static string GetBackingFieldName(string propName) {
            return string.Format("<{0}>k__BackingField", propName);
        }

        internal static FieldInfo[] GetDeclaredFields(this Type t) {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

    }
}
