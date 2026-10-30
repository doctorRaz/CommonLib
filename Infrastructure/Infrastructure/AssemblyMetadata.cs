using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace dRz.Infrastructure.Infrastructure
{
    /// <summary>
    /// Получает метаданные из атрибутов сборки и хранит их в словаре.
    /// Поиск ключей выполняется без учёта регистра.
    /// </summary>
    public class AssemblyMetadata
    {
        private readonly Dictionary<string, string> _metadata;

        //public IReadOnlyDictionary<string, string> Items => _metadata;

        /// <summary>Создаёт коллекцию метаданных из атрибутов указанной сборки.</summary>
        /// <param name="assembly">Сборка, атрибуты которой требуется прочитать.</param>
        /// <exception cref="ArgumentException">В сборке присутствуют повторяющиеся ключи метаданных без учёта регистра.</exception>
        public AssemblyMetadata(Assembly assembly)
        {
            _metadata = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .ToDictionary(
                    x => x.Key,
                    x => x.Value ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Пытается получить значение метаданных по ключу.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <param name="value">При успехе содержит значение; иначе <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, если ключ найден; иначе <see langword="false"/>.</returns>
        public bool TryGet(string key, out string? value)
        {
            return _metadata.TryGetValue(key, out value);
        }

        /// <summary>Получает значение метаданных по ключу.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <returns>Значение метаданных или <see langword="null"/>, если ключ отсутствует.</returns>
        public string? Get(string key)
        {
            return _metadata.TryGetValue(key, out string? value)
                ? value
                : null;
        }

        /// <summary>Получает значение метаданных по ключу либо значение по умолчанию.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <param name="defaultValue">Значение, возвращаемое, если ключ отсутствует.</param>
        /// <returns>Найденное значение или <paramref name="defaultValue"/>.</returns>
        public string Get(string key, string defaultValue)
        {
            return _metadata.TryGetValue(key, out string? value)
                ? value
                : defaultValue;
        }

        /// <summary>Возвращает ключи всех сохранённых метаданных.</summary>
        public IEnumerable<string> Keys => _metadata.Keys;

        /// <summary>Возвращает все пары «ключ — значение» метаданных.</summary>
        public IEnumerable<KeyValuePair<string, string>> Items => _metadata;
    }
}