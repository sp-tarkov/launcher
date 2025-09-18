/* DictionaryExtension.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using System.Collections.Generic;
using System.Linq;

namespace SPT.Launcher.Extensions
{
    public static class DictionaryExtensions
    {
        public static TKey GetKeyByValue<TKey, TValue>(this Dictionary<TKey, TValue> dic, TValue value)
        {
            List<TKey> keys = dic.Keys.ToList();

            for (var x = 0; x < keys.Count; x++)
            {
                if (dic.TryGetValue(keys[x], out var tempValue))
                {
                    if (tempValue != null && tempValue.Equals(value))
                    {
                        return keys[x];
                    }
                }
            }

            return default;
        }

        public static TKey GetKeyByInput<TKey, TValue>(this Dictionary<TKey, TValue> dic,
            string input)
        {
            var keys = dic.Keys.ToList();
            var values = dic.Values.ToList();
            
            for (var x = 0; x < dic.Count; x++)
            {
                if (values[x] is string s && (input.ToLower() == s.ToLower() || input.ToLower().StartsWith(s.ToLower())))
                {
                    return keys[x];
                }
            }

            return default;
        }
    }
}
