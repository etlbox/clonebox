using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CloneBox.Core {
    internal static class ExpressionGenerator {
        internal static object GenerateClassClonerExpression(Type sourceType, Type targetType) {
            // Parameter für sourceObject, targetObject und CloneState
            var sourceParameter = Expression.Parameter(typeof(object), "source");
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var stateParameter = Expression.Parameter(typeof(CloneState), "state");

            // Alle Felder des sourceType und targetType durchlaufen
            var sourceFields = GetAllFields(sourceType);
            var targetFields = GetAllFields(targetType);

            var expressions = new List<Expression>();

            // Die spezifische SetValue Methode für FieldInfo suchen
            var setValueMethod = typeof(FieldInfo).GetMethod(
                nameof(FieldInfo.SetValue),
                new[] { typeof(object), typeof(object) }
            );

            foreach (var targetField in targetFields) {
                if (sourceFields.TryGetValue(targetField.Key, out var sourceField)) {
                    // Zugriff auf das Feld im sourceObject (GetValue)
                    var sourceFieldAccess = Expression.Call(
                        Expression.Constant(sourceField),
                        typeof(FieldInfo).GetMethod(nameof(FieldInfo.GetValue)),
                        Expression.Convert(sourceParameter, typeof(object))
                    );

                    // Aufruf von CloneInternal für das Quellfeld
                    var cloneInternalMethod = typeof(CloneProvider).GetMethod(nameof(CloneProvider.CloneInternal), BindingFlags.Static | BindingFlags.NonPublic);
                    var clonedSourceValue = Expression.Call(cloneInternalMethod, sourceFieldAccess, stateParameter);

                    // SetValue für das targetObject aufrufen
                    var setValueCall = Expression.Call(
                        Expression.Constant(targetField.Value),
                        setValueMethod,
                        Expression.Convert(targetParameter, typeof(object)),
                        Expression.Convert(clonedSourceValue, typeof(object))
                    );

                    // Den SetValue-Aufruf zur Liste der Expressions hinzufügen
                    expressions.Add(setValueCall);
                }
            }

            // Erzeuge einen Block, der alle SetValue-Aufrufe enthält
            var body = Expression.Block(expressions);

            // Kompiliere die Expression zu einem Action Delegate
            return Expression.Lambda<Action<object, object, CloneState>>(body, sourceParameter, targetParameter, stateParameter).Compile();


            //var sourceObjectParam = Expression.Parameter(typeof(object), "sourceObject");
            //var targetObjectParam = Expression.Parameter(typeof(object), "targetObject");
            //var stateParam = Expression.Parameter(typeof(CloneState), "state");

            //var expressionList = new List<Expression>();
            //var copyFieldsMethod = typeof(CloneProvider).GetPrivateStaticMethod("CopyFields");

            //var methodCall = Expression.Call(copyFieldsMethod,
            //                            sourceObjectParam, // Parameter 1: sourceObject
            //                            targetObjectParam, // Parameter 2: targetObject
            //                            stateParam);       // Parameter 3: state


            //var lambda = Expression.Lambda<Action<object, object, CloneState>>(methodCall, sourceObjectParam, targetObjectParam, stateParam);

            //return lambda.Compile();
        }

        //private static void CopyFields(object sourceObject, object targetObject, CloneState state) {
        //    //if (sourceObject == null) return null;
        //    var targetfields = GetAllFields(targetObject);
        //    var sourceFields = GetAllFields(sourceObject);
        //    foreach (var targetFieldInfo in targetfields) {
        //        if (sourceFields.TryGetValue(targetFieldInfo.Key, out var sourceFieldInfo)) {
        //            var sourceValue = sourceFieldInfo.GetValue(sourceObject);
        //            var targetValue = CloneInternal(sourceValue, state);
        //            targetFieldInfo.Value.SetValue(targetObject, targetValue);
        //        }
        //    }
        //}

        private static Dictionary<string, FieldInfo> GetAllFields(Type targetType) {
            Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();            
            do {
                if (targetType == typeof(ContextBoundObject)) break;
                foreach (var fieldInfo in targetType.GetDeclaredFields())
                    fields.TryAddItem(fieldInfo.Name, fieldInfo);
                targetType = targetType.BaseType;
            }
            while (targetType != null);
            return fields;
        }

    }
}
