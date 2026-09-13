using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace CloneBox {
    internal static class ExpressionCloner {
        private static readonly ConcurrentDictionary<ClonerKey, Func<object, CloneProvider, object>> Cloners =
            new ConcurrentDictionary<ClonerKey, Func<object, CloneProvider, object>>();

        private static readonly ConcurrentDictionary<CopierKey, Func<object, object, CloneProvider, object>> Copiers =
            new ConcurrentDictionary<CopierKey, Func<object, object, CloneProvider, object>>();

        public static Func<object, CloneProvider, object> GetCloner(Type type, CloneSettings settings) {
            return Cloners.GetOrAdd(new ClonerKey(type, settings), key => CompileCloner(key.Type, settings));
        }

        public static Func<object, object, CloneProvider, object> GetCopier(Type sourceType, Type targetType, CloneSettings settings) {
            return Copiers.GetOrAdd(new CopierKey(sourceType, targetType, settings), key => CompileCopier(key.SourceType, key.TargetType, settings));
        }

        private static Func<object, CloneProvider, object> CompileCloner(Type type, CloneSettings settings) {
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var providerParam = Expression.Parameter(typeof(CloneProvider), "provider");
            var targetVar = Expression.Variable(typeof(object), "target");

            Expression afterCreate = Expression.Block(
                typeof(object),
                Expression.Call(CloneRuntime.RegisterInfo, sourceParam, targetVar, providerParam),
                BuildCopyBody(type, type, sourceParam, targetVar, providerParam, settings)
            );
            if (settings.DoNotCloneClass != null || type.GetCustomAttribute<DoNotClone>() != null) {
                afterCreate = Expression.Block(
                    typeof(object),
                    Expression.Call(CloneRuntime.RegisterInfo, sourceParam, targetVar, providerParam),
                    Expression.Condition(
                        Expression.Call(CloneRuntime.ShouldSkipClassInfo, Expression.Constant(type), providerParam),
                        Expression.Constant(null, typeof(object)),
                        BuildCopyBody(type, type, sourceParam, targetVar, providerParam, settings)
                    )
                );
            }

            var block = Expression.Block(
                typeof(object),
                new[] { targetVar },
                Expression.Assign(targetVar, BuildCreateInstance(type, sourceParam, providerParam, settings)),
                Expression.Condition(
                    Expression.Equal(targetVar, Expression.Constant(null)),
                    Expression.Constant(null, typeof(object)),
                    afterCreate
                )
            );

            return Expression.Lambda<Func<object, CloneProvider, object>>(block, sourceParam, providerParam).Compile();
        }

        private static Expression BuildCreateInstance(Type type, Expression source, Expression provider, CloneSettings settings) {
            if (type.IsArray)
                return Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider);

            if (typeof(Delegate).IsAssignableFrom(type))
                return Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider);

            if (type.IsValueType)
                return Expression.Convert(Expression.New(type), typeof(object));

            if (typeof(Exception).IsAssignableFrom(type))
                return Expression.Call(CloneRuntime.CreateExceptionInfo, source, provider);

            if (type.GetProperty("Comparer") != null)
                return Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider);

            var constructor = type.GetConstructor(settings.ConstructorBindings, null, Type.EmptyTypes, null);
            if (constructor != null) {
                var created = Expression.Variable(typeof(object), "created");
                return Expression.Block(
                    typeof(object),
                    new[] { created },
                    Expression.TryCatch(
                        Expression.Assign(created, Expression.Convert(Expression.New(constructor), typeof(object))),
                        Expression.Catch(
                            typeof(Exception),
                            Expression.Assign(created, Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider))
                        )
                    ),
                    created
                );
            }

            return Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider);
        }

        private static Func<object, object, CloneProvider, object> CompileCopier(Type sourceType, Type targetType, CloneSettings settings) {
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var targetParam = Expression.Parameter(typeof(object), "target");
            var providerParam = Expression.Parameter(typeof(CloneProvider), "provider");

            var body = BuildCopyBody(sourceType, targetType, sourceParam, targetParam, providerParam, settings);
            return Expression.Lambda<Func<object, object, CloneProvider, object>>(body, sourceParam, targetParam, providerParam).Compile();
        }

        private static Expression BuildCopyBody(Type sourceType, Type targetType, Expression source, Expression target, Expression provider, CloneSettings settings) {
            if (targetType.IsReadOnlyDictionary())
                return Expression.Call(CloneRuntime.CopyReadOnlyDictionaryInfo, source, target, provider);
            if (targetType.IsReadOnlyCollection())
                return Expression.Call(CloneRuntime.CopyReadOnlyCollectionInfo, source, target, provider);
            if (targetType.IsBitArray())
                return Expression.Call(CloneRuntime.CopyBitArrayInfo, source, target, provider);
            if (targetType.IsNameValueCollection())
                return Expression.Call(CloneRuntime.CopyNameValueCollectionInfo, source, target, provider);
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                var args = targetType.GetGenericArguments();
                var fill = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyGenericDictionary)).MakeGenericMethod(args[0], args[1]);
                return Expression.Call(fill, Expression.Convert(source, targetType), Expression.Convert(target, targetType), provider);
            }
            if (typeof(IDictionary).IsAssignableFrom(targetType))
                return Expression.Call(CloneRuntime.FillDictionaryInfo, source, target, provider);
            if (typeof(IDictionary<string, object>).IsAssignableFrom(targetType))
                return Expression.Call(CloneRuntime.FillDynamicDictionaryInfo, source, target, provider);
            if (targetType.IsArray)
                return BuildArrayCopy(sourceType, targetType, source, target, provider);
            if (targetType.IsIEnumerable())
                return BuildEnumerableCopy(sourceType, targetType, source, target, provider);
            return BuildMemberCopy(sourceType, targetType, source, target, provider, settings);
        }

        private static Expression BuildArrayCopy(Type sourceType, Type targetType, Expression source, Expression target, Expression provider) {
            var elementType = targetType.GetElementType();
            if (sourceType == targetType && targetType.GetArrayRank() == 1 && targetType == elementType.MakeArrayType()) {
                if (elementType.IsRealPrimitive()) {
                    var copy = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyPrimitiveArray)).MakeGenericMethod(elementType);
                    return Expression.Call(copy, Expression.Convert(source, targetType), Expression.Convert(target, targetType));
                }
                var fill = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyReferenceArray)).MakeGenericMethod(elementType);
                return Expression.Call(fill, Expression.Convert(source, targetType), Expression.Convert(target, targetType), provider);
            }
            return Expression.Call(CloneRuntime.FillArrayInfo, source, target, provider);
        }

        private static Expression BuildEnumerableCopy(Type sourceType, Type targetType, Expression source, Expression target, Expression provider) {
            if (targetType.IsGenericType) {
                var definition = targetType.GetGenericTypeDefinition();
                if (definition == typeof(List<>)) {
                    var itemType = targetType.GetGenericArguments()[0];
                    var fill = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyList)).MakeGenericMethod(itemType);
                    return Expression.Call(fill, Expression.Convert(source, targetType), Expression.Convert(target, targetType), provider);
                }
                if (definition == typeof(Dictionary<,>)) {
                    var args = targetType.GetGenericArguments();
                    var fill = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyGenericDictionary)).MakeGenericMethod(args[0], args[1]);
                    return Expression.Call(fill, Expression.Convert(source, targetType), Expression.Convert(target, targetType), provider);
                }
            }
            var filled = Expression.Variable(typeof(object), "filled");
            return Expression.Block(
                typeof(object),
                new[] { filled },
                Expression.Assign(filled, Expression.Call(CloneRuntime.FillEnumerableInfo, source, target, Expression.Constant(targetType), provider)),
                Expression.Call(CloneRuntime.CopyCollectionPropertiesInfo, source, filled, provider)
            );
        }

        private static Expression BuildMemberCopy(Type sourceType, Type targetType, Expression source, Expression target, Expression provider, CloneSettings settings) {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            Expression sourceTyped;
            Expression targetTyped;

            if (sourceType.IsValueType) {
                sourceTyped = Expression.Unbox(source, sourceType);
            } else {
                var sourceVar = Expression.Variable(sourceType, "src");
                variables.Add(sourceVar);
                expressions.Add(Expression.Assign(sourceVar, Expression.Convert(source, sourceType)));
                sourceTyped = sourceVar;
            }

            if (targetType.IsValueType) {
                targetTyped = Expression.Unbox(target, targetType);
            } else {
                var targetVar = Expression.Variable(targetType, "tgt");
                variables.Add(targetVar);
                expressions.Add(Expression.Assign(targetVar, Expression.Convert(target, targetType)));
                targetTyped = targetVar;
            }

            var ignoreBackingFields = new HashSet<string>();
            var sourceIsDynamic = typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(sourceType)
                && typeof(IDictionary<string, object>).IsAssignableFrom(sourceType);
            var hasPropertyPredicate = settings.DoNotCloneProperty != null;
            var hasFieldPredicate = settings.DoNotCloneField != null;

            foreach (var prop in PropFieldInfo.GetAllProperties(targetType, settings)) {
                if (prop.DoNotClone)
                    ignoreBackingFields.Add(ReflectionExtensions.GetBackingFieldName(prop));
                if (!prop.CanBeCloned || !prop.CanRead || !prop.CanWrite)
                    continue;

                var copy = BuildPropertyCopy(prop, sourceType, sourceTyped, targetTyped, source, target, provider, sourceIsDynamic, hasPropertyPredicate, settings);
                if (copy != null)
                    expressions.Add(copy);
            }

            foreach (var field in PropFieldInfo.GetAllFields(targetType, settings)) {
                if (!field.CanBeCloned || !field.CanRead || !field.CanWrite || ignoreBackingFields.Contains(field.Name))
                    continue;

                var copy = BuildFieldCopy(field, sourceType, sourceTyped, targetTyped, source, target, provider, sourceIsDynamic, hasFieldPredicate, settings);
                if (copy != null)
                    expressions.Add(copy);
            }

            expressions.Add(target);
            return Expression.Block(typeof(object), variables, expressions);
        }

        private static Expression BuildPropertyCopy(
            PropFieldInfo targetProp,
            Type sourceType,
            Expression sourceTyped,
            Expression targetTyped,
            Expression sourceBoxed,
            Expression targetBoxed,
            Expression provider,
            bool sourceIsDynamic,
            bool hasPropertyPredicate,
            CloneSettings settings) {

            var indexParameters = targetProp.TryGetIndexedParameters();
            if (indexParameters != null && indexParameters.Length > 0) {
                var indexedSource = sourceType.GetProperty(targetProp.Name, settings.PropertyBindings);
                if (indexedSource == null)
                    return null;
                Expression copy = Expression.Call(
                    CloneRuntime.CloneIndexedPropertyInfo,
                    sourceBoxed,
                    targetBoxed,
                    Expression.Constant(indexedSource),
                    Expression.Constant(targetProp.PropInfo),
                    provider);
                return WrapPropertyPredicate(copy, targetProp.PropInfo, provider, hasPropertyPredicate);
            }

            if (sourceIsDynamic) {
                var dynamicValue = Expression.Variable(typeof(object), "dynVal");
                var clonedDynamic = Expression.Call(CloneRuntime.CloneItemInfo, dynamicValue, provider);
                Expression setDynamic = Expression.Call(
                    CloneRuntime.TrySetPropertyInfo,
                    Expression.Constant(targetProp.PropInfo),
                    targetBoxed,
                    clonedDynamic);
                var dynamicBlock = Expression.Block(
                    typeof(void),
                    new[] { dynamicValue },
                    Expression.Assign(dynamicValue, Expression.Call(CloneRuntime.GetDynamicValueInfo, sourceBoxed, Expression.Constant(targetProp.Name))),
                    Expression.IfThen(Expression.NotEqual(dynamicValue, Expression.Constant(null)), setDynamic)
                );
                return WrapPropertyPredicate(dynamicBlock, targetProp.PropInfo, provider, hasPropertyPredicate);
            }

            var sourceProp = sourceType.GetProperty(targetProp.Name, settings.PropertyBindings);
            if (sourceProp == null)
                return null;
            var cloned = BuildClonedValue(Expression.Property(sourceTyped, sourceProp), sourceProp.PropertyType, targetProp.Type, provider);
            Expression assign;
            if (CanAssignProperty(targetProp.PropInfo)) {
                assign = Expression.TryCatch(
                    Expression.Block(typeof(void), Expression.Assign(Expression.Property(targetTyped, targetProp.PropInfo), cloned)),
                    Expression.Catch(typeof(Exception), Expression.Empty())
                );
            } else {
                assign = Expression.Call(
                    CloneRuntime.TrySetPropertyInfo,
                    Expression.Constant(targetProp.PropInfo),
                    targetBoxed,
                    Expression.Convert(cloned, typeof(object)));
            }

            return WrapPropertyPredicate(assign, targetProp.PropInfo, provider, hasPropertyPredicate);
        }

        private static Expression BuildFieldCopy(
            PropFieldInfo targetField,
            Type sourceType,
            Expression sourceTyped,
            Expression targetTyped,
            Expression sourceBoxed,
            Expression targetBoxed,
            Expression provider,
            bool sourceIsDynamic,
            bool hasFieldPredicate,
            CloneSettings settings) {

            if (sourceIsDynamic) {
                var dynamicValue = Expression.Variable(typeof(object), "dynFieldVal");
                var clonedDynamic = Expression.Call(CloneRuntime.CloneItemInfo, dynamicValue, provider);
                var setDynamic = Expression.Call(
                    CloneRuntime.TrySetFieldInfo,
                    Expression.Constant(targetField.FieldInfo),
                    targetBoxed,
                    clonedDynamic);
                var dynamicBlock = Expression.Block(
                    typeof(void),
                    new[] { dynamicValue },
                    Expression.Assign(dynamicValue, Expression.Call(CloneRuntime.GetDynamicValueInfo, sourceBoxed, Expression.Constant(targetField.Name))),
                    Expression.IfThen(Expression.NotEqual(dynamicValue, Expression.Constant(null)), setDynamic)
                );
                return WrapFieldPredicate(dynamicBlock, targetField.FieldInfo, provider, hasFieldPredicate);
            }

            var sourceField = sourceType.GetField(targetField.Name, settings.FieldBindings);
            if (sourceField == null)
                return null;
            if (sourceField.FieldType.IsPointer || targetField.Type.IsPointer) {
                return WrapFieldPredicate(
                    Expression.Call(CloneRuntime.CopyPointerFieldInfo, Expression.Constant(targetField.FieldInfo), sourceBoxed, targetBoxed),
                    targetField.FieldInfo,
                    provider,
                    hasFieldPredicate);
            }
            var cloned = BuildClonedValue(Expression.Field(sourceTyped, sourceField), sourceField.FieldType, targetField.Type, provider);
            Expression assign;
            if (targetField.FieldInfo.IsInitOnly || !targetField.FieldInfo.IsPublic) {
                assign = Expression.Call(
                    CloneRuntime.TrySetFieldInfo,
                    Expression.Constant(targetField.FieldInfo),
                    targetBoxed,
                    Expression.Convert(cloned, typeof(object)));
            } else {
                assign = Expression.TryCatch(
                    Expression.Block(typeof(void), Expression.Assign(Expression.Field(targetTyped, targetField.FieldInfo), cloned)),
                    Expression.Catch(typeof(Exception), Expression.Empty())
                );
            }

            return WrapFieldPredicate(assign, targetField.FieldInfo, provider, hasFieldPredicate);
        }

        private static Expression BuildClonedValue(Expression getValue, Type sourceValueType, Type targetValueType, Expression provider) {
            if (sourceValueType.IsPointer || targetValueType.IsPointer)
                return getValue;
            if (sourceValueType.IsRealPrimitive() && targetValueType.IsRealPrimitive()) {
                if (sourceValueType == targetValueType)
                    return getValue;
                return Expression.Convert(getValue, targetValueType);
            }

            Expression boxed = getValue.Type.IsValueType ? Expression.Convert(getValue, typeof(object)) : Expression.Convert(getValue, typeof(object));
            var cloned = Expression.Call(CloneRuntime.CloneItemInfo, boxed, provider);
            if (targetValueType == typeof(object))
                return cloned;
            if (targetValueType.IsValueType)
                return Expression.Convert(cloned, targetValueType);
            return Expression.Convert(cloned, targetValueType);
        }

        private static bool CanAssignProperty(PropertyInfo property) {
            return property.CanWrite && property.GetIndexParameters().Length == 0 && property.SetMethod != null;
        }

        private static Expression WrapPropertyPredicate(Expression body, PropertyInfo property, Expression provider, bool hasPredicate) {
            if (!hasPredicate)
                return body;
            return Expression.IfThen(
                Expression.Not(Expression.Call(CloneRuntime.ShouldSkipPropertyInfo, Expression.Constant(property), provider)),
                body);
        }

        private static Expression WrapFieldPredicate(Expression body, FieldInfo field, Expression provider, bool hasPredicate) {
            if (!hasPredicate)
                return body;
            return Expression.IfThen(
                Expression.Not(Expression.Call(CloneRuntime.ShouldSkipFieldInfo, Expression.Constant(field), provider)),
                body);
        }
    }
}
