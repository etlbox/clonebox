using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloneBox.Helper {
    internal static class ReflectionExtensions {
        //See also https://stackoverflow.com/questions/1827425/how-to-check-programmatically-if-a-type-is-a-struct-or-a-class
        public static bool IsRealPrimitive(this Type type) {            
            if (type == null) return false;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(DBNull);
        }

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
    }
}
