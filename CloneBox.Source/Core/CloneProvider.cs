using CloneBox.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneBox {
    internal static class CloneProvider {
        internal static object CloneInternal(object sourceObject, CloneState state) {
            if (sourceObject == null) return null;
            if (sourceObject.GetType().IsRealPrimitive()) return sourceObject;
            if (state.ExistingClones.ContainsKey(sourceObject)) return state.ExistingClones[sourceObject];
            var targetInst = InstanceCreator.CreateInstance(sourceObject.GetType(), state.Settings, sourceObject);
            state.ExistingClones.Add(sourceObject, targetInst);
            return CloneToInternal(sourceObject, targetInst, state);
        }

        internal static object CloneToInternal(object sourceObject, object targetObject, CloneState state) {
            state.ExistingCloners.TryGetValue(sourceObject.GetType(), out var cloner);
            if (cloner == null) {
                cloner = (Func<object, object, CloneState, object>)ExpressionGenerator.CopyFieldsExpression(sourceObject, targetObject);
                state.ExistingCloners.Add(sourceObject.GetType(), cloner);
            }
            return cloner(sourceObject, targetObject, state);
        }
    }
}
