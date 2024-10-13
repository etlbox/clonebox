using System;
using System.Collections;
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


        internal static Action<Array, Array, CloneState> Create1DimArrayFillExpression(Type elementType) {
            // Parameter für die Methode (sourceArray, targetArray, state)
            var sourceArrayParam = Expression.Parameter(typeof(Array), "sourceArray");
            var targetArrayParam = Expression.Parameter(typeof(Array), "targetArray");
            var stateParam = Expression.Parameter(typeof(CloneState), "state");

            // Variable für den Index
            var indexVar = Expression.Variable(typeof(int), "i");

            // Methode GetLength für das Array
            var getArrayLengthMethod = typeof(Array).GetMethod(nameof(Array.GetLength));

            // Array.Length für die Schleifenbedingung
            var sourceArrayLength = Expression.Call(sourceArrayParam, getArrayLengthMethod, Expression.Constant(0));
            var targetArrayLength = Expression.Call(targetArrayParam, getArrayLengthMethod, Expression.Constant(0));

            // Label für die Schleife
            var loopLabel = Expression.Label("loopLabel");

            // Abbruchbedingung: i >= targetArray.Length
            var breakCondition = Expression.GreaterThanOrEqual(indexVar, targetArrayLength);

            // sourceArray.GetValue(i)
            var getSourceValue = Expression.Call(sourceArrayParam, typeof(Array).GetMethod(nameof(Array.GetValue), new[] { typeof(int) }), indexVar);

            // Aufruf von CloneInternal
            var cloneInternalMethod = typeof(CloneProvider).GetMethod(nameof(CloneProvider.CloneInternal), BindingFlags.Static | BindingFlags.NonPublic);
            var clonedValue = Expression.Call(cloneInternalMethod, Expression.Convert(getSourceValue, typeof(object)), stateParam);

            // targetArray.SetValue(CloneInternal(sourceArray.GetValue(i)), i)
            var setTargetValue = Expression.Call(targetArrayParam, typeof(Array).GetMethod(nameof(Array.SetValue), new[] { typeof(object), typeof(int) }), Expression.Convert(clonedValue, typeof(object)), indexVar);

            // i++
            var incrementIndex = Expression.PostIncrementAssign(indexVar);

            // Schleifen-Body
            var loopBody = Expression.Block(
                // Abbruchbedingung: i >= targetArray.Length
                Expression.IfThen(breakCondition, Expression.Break(loopLabel)), // Abbruchbedingung
                setTargetValue,  // SetValue-Aufruf
                incrementIndex    // Index erhöhen
            );

            // Schleife (for i = 0; i < sourceArray.Length; i++)
            var loop = Expression.Loop(
                Expression.IfThenElse(Expression.LessThan(indexVar, sourceArrayLength), loopBody, Expression.Break(loopLabel)), // Bedingung für die Schleife
                loopLabel  // Break-Label
            );

            // Block, der die Variable "i" initialisiert und die Schleife ausführt
            var block = Expression.Block(new[] { indexVar },
                Expression.Assign(indexVar, Expression.Constant(0)),  // Initialisiere i = 0
                loop
            );

            // Kompiliere die Expression
            return Expression.Lambda<Action<Array, Array, CloneState>>(block, sourceArrayParam, targetArrayParam, stateParam).Compile();
        }


        internal static Action<object, object, CloneState> CreateCloneToInternalExpression(Type sourceType, Type targetType) {
            var sourceParam = Expression.Parameter(typeof(object), "sourceObject");
            var targetParam = Expression.Parameter(typeof(object), "targetObject");
            var stateParam = Expression.Parameter(typeof(CloneState), "state");

            // Erstellen der Expressions für die verschiedenen Szenarien
            var expressions = new List<Expression>();

            // Erster Fall: Ziel-Objekt ist eine Referenz
            var checkReference = Expression.IfThen(
                Expression.Call(
                    typeof(ReflectionExtensions),
                    nameof(ReflectionExtensions.DoReturnReference), 
                    null,
                    Expression.TypeAs(targetParam, targetType)),
                
                Expression.Assign(targetParam, sourceParam)
            );

            // Hinzufügen des Falles zur Liste
            expressions.Add(checkReference);

            // Fall: Ziel ist ein IDictionary
            var fillDictionaryCondition = Expression.IfThen(
                Expression.TypeIs(targetParam, typeof(IDictionary)),
                Expression.Call(typeof(CloneProvider), nameof(CloneProvider.FillDictionary), null, sourceParam, targetParam, stateParam)
            );

            expressions.Add(fillDictionaryCondition);

            // Fall: Ziel ist ein IDictionary<string, object>
            var fillDynamicDictionaryCondition = Expression.IfThen(
                Expression.TypeIs(targetParam, typeof(IDictionary<string, object>)),
                Expression.Call(typeof(CloneProvider), nameof(CloneProvider.FillDynamicDictionary), null, sourceParam, targetParam, stateParam)
            );

            expressions.Add(fillDynamicDictionaryCondition);

            // Fall: Ziel ist ein Array
            var fillArrayCondition = Expression.IfThen(
                Expression.TypeIs(targetParam, targetType.MakeByRefType()),  // Ziel ist ein Array
                Expression.Call(typeof(CloneProvider), nameof(CloneProvider.FillArray), null, sourceParam, targetParam, stateParam)
            );

            expressions.Add(fillArrayCondition);

            // Fall: Ziel ist IEnumerable
            var fillListCondition = Expression.IfThen(
                Expression.Call(typeof(CloneProvider), nameof(IsIEnumerable), null, targetParam),
                Expression.Call(typeof(CloneProvider), nameof(CloneProvider.FillList), null, sourceParam, targetParam, Expression.Constant(targetType), stateParam)
            );

            expressions.Add(fillListCondition);

            // Fall: Erstellen des Kloners
            var createCloner = Expression.Block(
                new[] { Expression.Variable(typeof(Action<object, object, CloneState>), "clonerExpression") },
                Expression.Assign(
                    Expression.Variable(typeof(Action<object, object, CloneState>), "clonerExpression"),
                    Expression.Convert(
                        Expression.Call(typeof(ExpressionGenerator), nameof(ExpressionGenerator.GenerateClassClonerExpression), null, Expression.TypeAs(sourceParam, sourceType), Expression.TypeAs(targetParam, targetType)),
                        typeof(Action<object, object, CloneState>)
                    )
                ),
                Expression.Invoke(Expression.Variable(typeof(Action<object, object, CloneState>), "clonerExpression"), sourceParam, targetParam, stateParam)
            );

            // Ausdruck hinzufügen
            expressions.Add(createCloner);

            // Block erstellen und kompilieren
            var block = Expression.Block(expressions);
            return Expression.Lambda<Action<object, object, CloneState>>(block, sourceParam, targetParam, stateParam).Compile();
        }

        private static bool IsIEnumerable(object obj) {
            return obj is IEnumerable;
        }
    }
}
