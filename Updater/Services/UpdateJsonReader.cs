using dRz.Updater.Models;
using System.IO;
using System.Text.Json;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Читает описание обновления из update.json.
    /// </summary>
    public static class UpdateJsonReader
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Читает и десериализует update.json.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу manifest.</param>
        /// <returns>Десериализованные метаданные обновления.</returns>
        /// <exception cref="FileNotFoundException">Файл manifest не найден.</exception>
        /// <exception cref="JsonException">Содержимое файла не является корректным JSON или не соответствует модели.</exception>
        /// <exception cref="InvalidDataException">JSON содержит <see langword="null"/> вместо объекта обновления.</exception>
        /// <exception cref="IOException">Не удалось прочитать файл.</exception>
        public static UpdateInfo Read(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл update.json не найден.", filePath);
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<UpdateInfo>(json, _options)
                ?? throw new InvalidDataException("Файл update.json не содержит данных обновления.");
        }
    }
}