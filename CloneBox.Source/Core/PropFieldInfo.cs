using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloneBox {

    internal class PropFieldInfo {

        private string _name;
        public string Name {
            get {
                return _name ?? PropInfo?.Name ?? FieldInfo?.Name;
            }
            set {
                _name = value;
            }
        }

        private Type _type;
        public Type Type {
            get {
                return _type ?? PropInfo?.PropertyType ?? FieldInfo?.FieldType;
            }
            set {
                _type = value;
            }
        }

        public MemberType MemberType { get; private set; }
        public PropertyInfo PropInfo { get; private set; }
        public FieldInfo FieldInfo { get; private set; }
        public bool CanRead => PropInfo?.CanRead ?? true;
        public bool CanWrite => PropInfo?.CanWrite ?? true;
        public bool DoNotClone { get; private set; }
        public bool CanBeCloned => !DoNotClone;

        public PropFieldInfo(MemberType memberType) {
            MemberType = memberType;
        }

        public static IEnumerable<PropFieldInfo> GetAllProperties(Type type, CloneSettings cloneSettings) {
            return type
                .GetProperties(cloneSettings.PropertyBindings)
                .Select(
                    propInfo => new PropFieldInfo(MemberType.Property) {
                        PropInfo = propInfo,
                        DoNotClone = cloneSettings.DoNotClonePropertyInternal(propInfo)
                    }
                );
        }

        public static IEnumerable<PropFieldInfo> GetAllFields(Type type, CloneSettings cloneSettings) {
            var fields = new List<PropFieldInfo>();
            var names = new HashSet<string>();
            var flags = cloneSettings.FieldBindings | BindingFlags.DeclaredOnly;
            for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
                foreach (var fieldInfo in current.GetFields(flags)) {
                    if (!names.Add(fieldInfo.Name))
                        continue;
                    fields.Add(new PropFieldInfo(MemberType.Field) {
                        FieldInfo = fieldInfo,
                        DoNotClone = cloneSettings.DoNotCloneFieldInternal(fieldInfo)
                    });
                }
            }
            return fields;
        }

        public static PropFieldInfo MatchingPropField(MemberType memberType, object sourceObject, string propFieldName, CloneSettings cloneSettings) {
            var sourceType = sourceObject.GetType();
            if (sourceType.IsDynamic(sourceObject)) {
                var dict = sourceObject as IDictionary<string, object>;
                return new PropFieldInfo(MemberType.Dynamic) {
                    Type = dict.GetValueOrNull(propFieldName)?.GetType(),
                    Name = propFieldName
                };
            }
            if (memberType == MemberType.Property) {
                var prop = sourceType.GetProperty(propFieldName, cloneSettings.PropertyBindings);
                return new PropFieldInfo(MemberType.Property) {
                    PropInfo = prop
                };
            } else {
                var field = sourceType.GetField(propFieldName, cloneSettings.FieldBindings);
                return new PropFieldInfo(MemberType.Field) {
                    FieldInfo = field
                };
            }
        }

        public object GetValue(object obj) {
            if (MemberType == MemberType.Dynamic)
                return (obj as IDictionary<string, object>).GetValueOrNull(Name);
            else if (MemberType == MemberType.Property)
                try {
                    return PropInfo.GetValue(obj);
                } catch {
                    return PropInfo.GetValue(obj, new object[] { null });
                }
            else
                return FieldInfo.GetValue(obj);
        }



        public void SetValue(object obj, object value) {
            try {
                if (MemberType == MemberType.Property)
                    PropInfo.SetValue(obj, value);
                else
                    FieldInfo.SetValue(obj, value);
            } catch (Exception) {
            }
        }

        public ParameterInfo[] TryGetIndexedParameters() {
            if (MemberType == MemberType.Property)
                return PropInfo.GetIndexParameters();
            else
                return null;
        }


    }
}
