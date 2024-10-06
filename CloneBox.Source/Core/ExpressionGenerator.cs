using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CloneBox.Core {
    internal static class ExpressionGenerator {

        public static object CopyFieldsExpression(object sourceObject, object targetObject) {
            var type = targetObject.GetType();
            var methodType = typeof(object);
            var expressionList = new List<Expression>();

            ParameterExpression from = Expression.Parameter(methodType);            
            var to = Expression.Parameter(methodType);
            var fromLocal = Expression.Variable(type);
            var toLocal = Expression.Variable(type);
            var state = Expression.Parameter(typeof(CloneState));

            expressionList.Add(Expression.Assign(fromLocal, Expression.Convert(from, type)));
            expressionList.Add(Expression.Assign(toLocal, Expression.Convert(to, type)));
            expressionList.Add(Expression.Call(state, typeof(CloneState).GetMethod("AddExistingClone"), from, to));

            List<FieldInfo> fi = new List<FieldInfo>();
            var tp = type;
            do {
                if (tp == typeof(ContextBoundObject)) break;
                fi.AddRange(tp.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly));
                tp = tp.BaseType;
            }
            while (tp != null);

            foreach (var fieldInfo in fi) {
                var methodInfo = fieldInfo.FieldType.IsRealPrimitive()
                         ? typeof(ExpressionGenerator).GetMethod("CloneStructInternal", BindingFlags.NonPublic | BindingFlags.Static)
                             .MakeGenericMethod(fieldInfo.FieldType)
                         : typeof(ExpressionGenerator).GetMethod("CloneClassInternal", BindingFlags.NonPublic | BindingFlags.Static);

                var get = Expression.Field(fromLocal, fieldInfo);
                var call = (Expression)Expression.Call(methodInfo, get, state);
                call = Expression.Convert(call, fieldInfo.FieldType);
                expressionList.Add(Expression.Assign(Expression.Field(toLocal, fieldInfo), call));
            }

            expressionList.Add(Expression.Convert(toLocal, methodType));

            var funcType = typeof(Func<,,,>).MakeGenericType(methodType, methodType, typeof(CloneState), methodType);

            var blockParams = new List<ParameterExpression>();
            blockParams.Add(fromLocal);
            blockParams.Add(toLocal);

            return Expression.Lambda(funcType, Expression.Block(blockParams, expressionList), from, to, state).Compile();
            //var state = Expression.Parameter(typeof(DeepCloneState));
        }

        private static T CloneStructInternal<T>(T obj, CloneState cloneSettings) // where T : struct
        {
            return obj;
            // no loops, no nulls, no inheritance
            //var cloner = GetClonerForValueType<T>();

            // safe ojbect
            //if (cloner == null)
            //    return obj;

            //return cloner(obj, state);
        }

        private static object CloneClassInternal(object obj, CloneState cloneSettings) {
            if (obj == null)
                return null;
            return obj;         
        }

        //private static void CopyPropertiesAndFields(object sourceObject, object targetObject, CloneSettings cloneSettings) {
        //    var allProperties = PropFieldInfo.GetAllProperties(targetObject.GetType(), cloneSettings);
        //    HashSet<string> ignoreRelatedBackingField = new HashSet<string>();
        //    foreach (var prop in allProperties) {
        //        if (prop.DoNotClone) ignoreRelatedBackingField.TryAdd(ReflectionExtensions.GetBackingFieldName(prop));
        //        if (prop.CanBeCloned && prop.CanRead && prop.CanWrite)
        //            TryClonePropField(sourceObject, targetObject, prop, cloneSettings);
        //    }
        //    foreach (var field in PropFieldInfo.GetAllFields(targetObject.GetType(), cloneSettings)) {
        //        if (field.CanBeCloned && field.CanRead && field.CanWrite && !ignoreRelatedBackingField.Contains(field.Name))
        //            TryClonePropField(sourceObject, targetObject, field, cloneSettings);
        //    }
        //}


        //private static void TryClonePropField(object sourceObject, object targetObject, PropFieldInfo targetPropField, CloneSettings cloneSettings) {
        //    PropFieldInfo sourcePropField = PropFieldInfo.MatchingPropField(targetPropField.MemberType, sourceObject, targetPropField.Name, cloneSettings);
        //    if (sourcePropField?.Type == null) return;

        //    var paramInfo = sourcePropField.TryGetIndexedParameters();
        //    if (sourcePropField.MemberType == MemberType.Property && (paramInfo?.Length ?? 0) > 0) {
        //        for (int i = 0; i < paramInfo.Length; i++) {
        //            var index = new object[] { i };
        //            object propFieldClone = CloneInternal(sourcePropField.PropInfo.GetValue(sourceObject, index), cloneSettings);
        //            targetPropField.PropInfo.SetValue(targetObject, propFieldClone, index);
        //        }
        //    } else {
        //        object propFieldClone = CloneInternal(sourcePropField.GetValue(sourceObject), cloneSettings);
        //        targetPropField.SetValue(targetObject, propFieldClone);
        //    }

        //}
    }
}
