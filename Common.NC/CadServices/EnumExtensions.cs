using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace dRz.Common.NC.CadServices
{
    /// <summary>
    /// Предоставляет методы расширения для значений перечислений.
    /// </summary>
    public static class EnumExtensions
    {
        // Note that we never need to expire these cache items, so we just use ConcurrentDictionary rather than MemoryCache
        private static readonly ConcurrentDictionary<string, string> DisplayNameCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Displays the name.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string DisplayName(this Enum value)
        {
            string key = $"{value.GetType().FullName}.{value}";

            string name = value.ToString();

            FieldInfo? field = value.GetType().GetTypeInfo().GetField(name);

            return DisplayNameCache.GetOrAdd(key, _ =>
          {
              string name = value.ToString();

              FieldInfo? field = value.GetType().GetTypeInfo().GetField(name);

              DescriptionAttribute? attribute = field?
                  .GetCustomAttribute<DescriptionAttribute>();

              return attribute?.Description ?? name;
          });
        }
    }
}