using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CloneBox.Core {

    internal class PropFieldInfo {

        public string Name => PropInfo?.Name ?? FieldInfo?.Name;
        public Type Type => PropInfo?.PropertyType ?? FieldInfo?.FieldType;
        public MemberType MemberType { get; set; }
        public PropertyInfo PropInfo { get; set; }
        public FieldInfo FieldInfo { get; set; }
        public bool CanRead => PropInfo?.CanRead ?? true;
        public bool CanWrite => PropInfo?.CanWrite ?? true;

        public PropFieldInfo(MemberType memberType) {
            MemberType = memberType;
        }

        public static IEnumerable<PropFieldInfo> GetAllProperties(Type type, CloneSettings cloneSettings) {
            return type
                .GetProperties(cloneSettings.PropertyBindings)
                .Select(
                    propInfo => new PropFieldInfo(MemberType.Property) {
                        PropInfo = propInfo
                    }
                );
        }

        public static IEnumerable<PropFieldInfo> GetAllFields(Type type, CloneSettings cloneSettings) {
            return type
                .GetFields(cloneSettings.FieldBindings)
                .Select(
                    fieldInfo => new PropFieldInfo(MemberType.Field) {
                        FieldInfo = fieldInfo
                    }
                );
        }

        public static PropFieldInfo MatchingPropField(MemberType memberType, Type sourceType, string propFieldName, CloneSettings cloneSettings) {
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
            if (MemberType == MemberType.Property)
                try {
                    return PropInfo.GetValue(obj);
                }
                catch {
                    return PropInfo.GetValue(obj, new object[] {null });
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
            } catch(Exception e) {
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
