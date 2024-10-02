using System.Collections.Generic;

namespace CloneBox {
    internal static class IEnumerableExtensions {
        public static TValue GetValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            => dict.ContainsKey(key) ? dict[key] : default(TValue);

    }
}
