using System.Collections.Generic;

namespace CloneBox {
    internal static class IDictionaryExtensions {
        public static TValue GetValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            => dict.ContainsKey(key) ? dict[key] : default(TValue);

        public static IDictionary<string,object> ToDictionary(this object obj) {
            if (obj is IDictionary<string, object>)
                return (IDictionary<string, object>)obj;
            var dict = new Dictionary<string, object>();
            if (obj == null) return dict;
            foreach (var prop in obj.GetType().GetProperties()) {
                dict[prop.Name] = prop.GetValue(obj);
            }            
            return dict;
        }

    }
}
