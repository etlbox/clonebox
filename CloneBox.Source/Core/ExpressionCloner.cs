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
            var key = new ClonerKey(type, settings);
            if (Cloners.TryGetValue(key, out var cloner))
                return cloner;
            return Cloners.GetOrAdd(key, CompileCloner(type, settings));
        }

        public static Func<object, object, CloneProvider, object> GetCopier(Type sourceType, Type targetType, CloneSettings settings) {
            var key = new CopierKey(sourceType, targetType, settings);
            if (Copiers.TryGetValue(key, out var copier))
                return copier;
            return Copiers.GetOrAdd(key, CompileCopier(sourceType, targetType, settings));
        }

        private static Func<object, CloneProvider, object> CompileCloner(Type type, CloneSettings settings) {
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var providerParam = Expression.Parameter(typeof(CloneProvider), "provider");
            var targetVar = Expression.Variable(typeof(object), "target");

            Expression copy = BuildCopyBody(type, type, sourceParam, targetVar, providerParam, settings);
            if (settings.DoNotCloneClass != null || type.GetCustomAttribute<DoNotClone>() != null) {
                copy = Expression.Condition(
                    Expression.Call(CloneRuntime.ShouldSkipClassInfo, Expression.Constant(type), providerParam),
                    Expression.Constant(null, typeof(object)),
                    copy);
            }

            var block = Expression.Block(
                typeof(object),
                new[] { targetVar },
                Expression.Assign(targetVar, BuildCreateInstance(type, sourceParam, providerParam, settings)),
                Expression.Condition(
                    Expression.Equal(targetVar, Expression.Constant(null)),
                    Expression.Constant(null, typeof(object)),
                    Expression.Block(
                        typeof(object),
                        Expression.Call(CloneRuntime.RegisterInfo, sourceParam, targetVar, providerParam),
                        copy)
                )
            );

            return Expression.Lambda<Func<object, CloneProvider, object>>(block, sourceParam, providerParam).Compile();
        }

        private static Expression BuildCreateInstance(Type type, Expression source, Expression provider, CloneSettings settings) {
            if (type.IsArray)
                return BuildCreateArray(type, source, provider);

            if (typeof(Delegate).IsAssignableFrom(type))
                return CallCreateInstance(type, source, provider);

            if (type.IsValueType)
                return Expression.Convert(Expression.New(type), typeof(object));

            if (typeof(Exception).IsAssignableFrom(type))
                return Expression.Call(CloneRuntime.CreateExceptionInfo, source, provider);

            var listCreate = TryCreateList(type, source);
            if (listCreate != null)
                return listCreate;

            var comparerCreate = TryCreateWithComparer(type, source, provider);
            if (comparerCreate != null)
                return comparerCreate;

            var constructor = type.GetConstructor(settings.ConstructorBindings, null, Type.EmptyTypes, null);
            if (constructor != null)
                return TryOrFallback(
                    Expression.Convert(Expression.New(constructor), typeof(object)),
                    CallCreateInstance(type, source, provider));

            return CallCreateInstance(type, source, provider);
        }

        private static Expression BuildCreateArray(Type type, Expression source, Expression provider) {
            var elementType = type.GetElementType();
            if (elementType != null && type.GetArrayRank() == 1 && type == elementType.MakeArrayType()) {
                var sourceArray = Expression.Convert(source, type);
                return Expression.Convert(
                    Expression.NewArrayBounds(elementType, Expression.ArrayLength(sourceArray)),
                    typeof(object));
            }
            return CallCreateInstance(type, source, provider);
        }

        private static Expression TryCreateList(Type type, Expression source) {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
                return null;
            var ctor = type.GetConstructor(new[] { typeof(int) });
            var count = type.GetProperty("Count");
            if (ctor == null || count == null)
                return null;
            var sourceList = Expression.Convert(source, type);
            return Expression.Convert(
                Expression.New(ctor, Expression.Property(sourceList, count)),
                typeof(object));
        }

        private static Expression TryCreateWithComparer(Type type, Expression source, Expression provider) {
            var comparerProp = type.GetProperty("Comparer");
            if (comparerProp == null)
                return null;
            foreach (var ctor in type.GetConstructors()) {
                var parameters = ctor.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(comparerProp.PropertyType)) {
                    var created = Expression.Convert(
                        Expression.New(ctor, Expression.Property(Expression.Convert(source, type), comparerProp)),
                        typeof(object));
                    return TryOrFallback(created, CallCreateInstance(type, source, provider));
                }
            }
            return CallCreateInstance(type, source, provider);
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
                return Expression.Call(CloneRuntime.CopyReadOnlyDictionaryInfo, source, provider);
            if (targetType.IsReadOnlyCollection())
                return Expression.Call(CloneRuntime.CopyReadOnlyCollectionInfo, source, provider);
            if (targetType.IsBitArray())
                return Expression.Call(CloneRuntime.CopyBitArrayInfo, source, provider);
            if (targetType.IsNameValueCollection())
                return Expression.Call(CloneRuntime.CopyNameValueCollectionInfo, source, provider);
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
                return BuildEnumerableCopy(targetType, source, target, provider);
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

        private static Expression BuildEnumerableCopy(Type targetType, Expression source, Expression target, Expression provider) {
            if (targetType.IsGenericType) {
                var definition = targetType.GetGenericTypeDefinition();
                if (definition == typeof(List<>)) {
                    var itemType = targetType.GetGenericArguments()[0];
                    var fill = typeof(CloneRuntime).GetMethod(nameof(CloneRuntime.CopyList)).MakeGenericMethod(itemType);
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

            var sourceTyped = Localize(sourceType, source, "src", variables, expressions);
            var targetTyped = Localize(targetType, target, "tgt", variables, expressions);
            var context = new MemberCopyContext(sourceType, sourceTyped, targetTyped, source, target, provider, settings);

            var ignoreBackingFields = new HashSet<string>();
            foreach (var prop in PropFieldInfo.GetAllProperties(targetType, settings)) {
                if (ShouldIgnoreBackingField(prop, settings))
                    ignoreBackingFields.Add(ReflectionExtensions.GetBackingFieldName(prop.Name));
                if (prop.DoNotClone || !prop.CanRead || !prop.CanWrite)
                    continue;

                var copy = BuildPropertyCopy(prop, context);
                if (copy != null)
                    expressions.Add(copy);
            }

            foreach (var field in PropFieldInfo.GetAllFields(targetType, settings)) {
                if (field.DoNotClone || ignoreBackingFields.Contains(field.Name))
                    continue;

                var copy = BuildFieldCopy(field, context);
                if (copy != null)
                    expressions.Add(copy);
            }

            expressions.Add(target);
            return Expression.Block(typeof(object), variables, expressions);
        }

        private static Expression Localize(Type type, Expression boxed, string name, List<ParameterExpression> variables, List<Expression> expressions) {
            if (type.IsValueType)
                return Expression.Unbox(boxed, type);
            var local = Expression.Variable(type, name);
            variables.Add(local);
            expressions.Add(Expression.Assign(local, Expression.Convert(boxed, type)));
            return local;
        }

        private static Expression BuildPropertyCopy(PropFieldInfo targetProp, MemberCopyContext context) {
            var property = targetProp.PropInfo;
            if (targetProp.IsIndexer) {
                var indexedSource = context.SourceType.GetProperty(targetProp.Name, context.Settings.PropertyBindings);
                if (indexedSource == null)
                    return null;
                return context.UnlessPropertySkipped(
                    Expression.Call(
                        CloneRuntime.CloneIndexedPropertyInfo,
                        context.SourceBoxed,
                        context.TargetBoxed,
                        Expression.Constant(indexedSource),
                        Expression.Constant(property),
                        context.Provider),
                    property);
            }

            if (context.SourceIsDynamic)
                return context.UnlessPropertySkipped(
                    context.BuildDynamicCopy(targetProp.Name, CloneRuntime.TrySetPropertyInfo, property),
                    property);

            var sourceProp = context.SourceType.GetProperty(targetProp.Name, context.Settings.PropertyBindings);
            if (sourceProp == null)
                return null;

            var cloned = BuildClonedValue(Expression.Property(context.SourceTyped, sourceProp), sourceProp.PropertyType, targetProp.Type, context.Provider);
            Expression assign = CanAssignProperty(property)
                ? AssignMember(Expression.Property(context.TargetTyped, property), cloned)
                : Expression.Call(CloneRuntime.TrySetPropertyInfo, Expression.Constant(property), context.TargetBoxed, Expression.Convert(cloned, typeof(object)));

            return context.UnlessPropertySkipped(assign, property);
        }

        private static Expression BuildFieldCopy(PropFieldInfo targetField, MemberCopyContext context) {
            var field = targetField.FieldInfo;
            if (context.SourceIsDynamic)
                return context.UnlessFieldSkipped(
                    context.BuildDynamicCopy(targetField.Name, CloneRuntime.TrySetFieldInfo, field),
                    field);

            var sourceField = context.SourceType.GetField(targetField.Name, context.Settings.FieldBindings);
            if (sourceField == null)
                return null;

            if (sourceField.FieldType.IsPointer || targetField.Type.IsPointer)
                return context.UnlessFieldSkipped(
                    Expression.Call(CloneRuntime.CopyPointerFieldInfo, Expression.Constant(field), context.SourceBoxed, context.TargetBoxed),
                    field);

            var cloned = BuildClonedValue(Expression.Field(context.SourceTyped, sourceField), sourceField.FieldType, targetField.Type, context.Provider);
            Expression assign = field.IsInitOnly || !field.IsPublic
                ? Expression.Call(CloneRuntime.TrySetFieldInfo, Expression.Constant(field), context.TargetBoxed, Expression.Convert(cloned, typeof(object)))
                : (Expression)AssignMember(Expression.Field(context.TargetTyped, field), cloned);

            return context.UnlessFieldSkipped(assign, field);
        }

        private static Expression BuildClonedValue(Expression getValue, Type sourceValueType, Type targetValueType, Expression provider) {
            if (sourceValueType.IsPointer || targetValueType.IsPointer)
                return getValue;
            if (sourceValueType.IsRealPrimitive() && targetValueType.IsRealPrimitive()) {
                if (sourceValueType == targetValueType)
                    return getValue;
                return Expression.Convert(getValue, targetValueType);
            }

            var boxed = getValue.Type == typeof(object) ? getValue : Expression.Convert(getValue, typeof(object));
            var cloned = Expression.Call(CloneRuntime.CloneItemInfo, boxed, provider);
            if (targetValueType == typeof(object))
                return cloned;
            return Expression.Convert(cloned, targetValueType);
        }

        private static bool ShouldIgnoreBackingField(PropFieldInfo prop, CloneSettings settings) {
            if (prop.DoNotClone)
                return true;
            if (!prop.CanRead || !prop.CanWrite)
                return false;
            if (settings.DoNotCloneClass != null)
                return false;
            return prop.Type?.GetCustomAttribute<DoNotClone>() == null;
        }

        private static Expression AssignMember(Expression member, Expression value) {
            return Expression.TryCatch(
                Expression.Block(typeof(void), Expression.Assign(member, value)),
                Expression.Catch(typeof(Exception), Expression.Empty())
            );
        }

        private static bool CanAssignProperty(PropertyInfo property) {
            return property.CanWrite && property.GetIndexParameters().Length == 0 && property.SetMethod != null;
        }

        private static Expression CallCreateInstance(Type type, Expression source, Expression provider)
            => Expression.Call(CloneRuntime.CreateInstanceInfo, Expression.Constant(type), source, provider);

        private static Expression TryOrFallback(Expression create, Expression fallback) {
            var created = Expression.Variable(typeof(object), "created");
            return Expression.Block(
                typeof(object),
                new[] { created },
                Expression.TryCatch(
                    Expression.Assign(created, create),
                    Expression.Catch(typeof(Exception), Expression.Assign(created, fallback))
                ),
                created);
        }

        private sealed class MemberCopyContext {
            public readonly Type SourceType;
            public readonly Expression SourceTyped;
            public readonly Expression TargetTyped;
            public readonly Expression SourceBoxed;
            public readonly Expression TargetBoxed;
            public readonly Expression Provider;
            public readonly CloneSettings Settings;
            public readonly bool SourceIsDynamic;

            public MemberCopyContext(Type sourceType, Expression sourceTyped, Expression targetTyped,
                Expression sourceBoxed, Expression targetBoxed, Expression provider, CloneSettings settings) {
                SourceType = sourceType;
                SourceTyped = sourceTyped;
                TargetTyped = targetTyped;
                SourceBoxed = sourceBoxed;
                TargetBoxed = targetBoxed;
                Provider = provider;
                Settings = settings;
                SourceIsDynamic = sourceType.IsDynamicDictionary();
            }

            public Expression UnlessPropertySkipped(Expression body, PropertyInfo property) {
                if (Settings.DoNotCloneProperty == null)
                    return body;
                return Expression.IfThen(
                    Expression.Not(Expression.Call(CloneRuntime.ShouldSkipPropertyInfo, Expression.Constant(property), Provider)),
                    body);
            }

            public Expression UnlessFieldSkipped(Expression body, FieldInfo field) {
                if (Settings.DoNotCloneField == null)
                    return body;
                return Expression.IfThen(
                    Expression.Not(Expression.Call(CloneRuntime.ShouldSkipFieldInfo, Expression.Constant(field), Provider)),
                    body);
            }

            public Expression BuildDynamicCopy(string name, MethodInfo setter, object member) {
                var value = Expression.Variable(typeof(object), "dynVal");
                var set = Expression.Call(
                    setter,
                    Expression.Constant(member),
                    TargetBoxed,
                    Expression.Call(CloneRuntime.CloneItemInfo, value, Provider));
                return Expression.Block(
                    typeof(void),
                    new[] { value },
                    Expression.Assign(value, Expression.Call(CloneRuntime.GetDynamicValueInfo, SourceBoxed, Expression.Constant(name))),
                    Expression.IfThen(Expression.NotEqual(value, Expression.Constant(null)), set));
            }
        }
    }
}
