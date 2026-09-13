using System;
using System.Collections.Generic;
using System.Reflection;

namespace CloneBox {

    internal class PropFieldInfo {
        public string Name => PropInfo?.Name ?? FieldInfo?.Name;
        public Type Type => PropInfo?.PropertyType ?? FieldInfo?.FieldType;
        public PropertyInfo PropInfo { get; }
        public FieldInfo FieldInfo { get; }
        public bool CanRead => PropInfo?.CanRead ?? true;
        public bool CanWrite => PropInfo?.CanWrite ?? true;
        public bool IsIndexer => PropInfo?.GetIndexParameters().Length > 0;
        public bool DoNotClone { get; }

        public PropFieldInfo(PropertyInfo propInfo, bool doNotClone) {
            PropInfo = propInfo;
            DoNotClone = doNotClone;
        }

        public PropFieldInfo(FieldInfo fieldInfo, bool doNotClone) {
            FieldInfo = fieldInfo;
            DoNotClone = doNotClone;
        }

        public static IEnumerable<PropFieldInfo> GetAllProperties(Type type, CloneSettings cloneSettings) {
            foreach (var propInfo in type.GetProperties(cloneSettings.PropertyBindings))
                yield return new PropFieldInfo(propInfo, cloneSettings.DoNotClonePropertyInternal(propInfo));
        }

        public static IEnumerable<PropFieldInfo> GetAllFields(Type type, CloneSettings cloneSettings) {
            var names = new HashSet<string>();
            var flags = cloneSettings.FieldBindings | BindingFlags.DeclaredOnly;
            for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
                foreach (var fieldInfo in current.GetFields(flags)) {
                    if (!names.Add(fieldInfo.Name))
                        continue;
                    yield return new PropFieldInfo(fieldInfo, cloneSettings.DoNotCloneFieldInternal(fieldInfo));
                }
            }
        }

    }
}
