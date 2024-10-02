using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace CloneBox.Helper {
    internal static class IEnumerableExtensions {
        public static TValue GetValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            => dict.ContainsKey(key)? dict[key] : default(TValue);
        
    }
}
