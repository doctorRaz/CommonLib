using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dRz.Settings
{
    /// <summary>
    /// Читает и записывает настройки в JSON-файл.
    /// </summary>
    public sealed class JsonSettingsStore
    {
        private readonly string _filePath;
        private readonly IDrzLogger _logger;
        private readonly IMessageService _messageService;

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        /// <summary>True, если последняя загрузка завершилась использованием значений по умолчанию.</summary>
        internal bool LoadUsedDefaults { get; private set; }

        /// <summary>
        /// Создаёт JSON-хранилище по готовому пути к файлу.
        /// </summary>
        public JsonSettingsStore(string filePath, IDrzLoggerFactory loggerFactory, IMessageService messageService)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Не указан путь к файлу настроек.", nameof(filePath));
            }

            _filePath = filePath;
            _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).GetLogger<JsonSettingsStore>();
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        /// <summary>Загружает настройки. При отсутствии файла или ошибке чтения возвращает null.</summary>
        internal AddonSettings? Load()
        {
            LoadUsedDefaults = false;

            if (!File.Exists(_filePath))
            {
                LoadUsedDefaults = true;
                return null;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                AddonSettings settings = JsonSerializer.Deserialize<AddonSettings>(json, SerializerOptions)
                    ?? throw new JsonException("Файл настроек содержит null вместо объекта настроек.");

                settings.General ??= new GeneralSettings();
                settings.Updates ??= new UpdateSettings();
                return settings;
            }
            catch (Exception ex)
            {
                LoadUsedDefaults = true;
                _logger.Error(ex, $"Не удалось прочитать настройки из '{_filePath}'. Будут использованы значения по умолчанию.");
                return null;
            }
        }

        /// <summary>Сохраняет настройки. При ошибке уведомляет пользователя и повторно выбрасывает исключение.</summary>
        internal void Save(AddonSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            try
            {
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, SerializerOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Не удалось сохранить настройки в '{_filePath}'.");
                try
                {
                    _messageService.ErrorMessage($"Не удалось сохранить настройки аддона. Изменения могут быть потеряны. Файл: '{_filePath}'.", ex);
                }
                catch (Exception notificationException)
                {
                    _logger.Error(notificationException, "Не удалось уведомить пользователя об ошибке сохранения настроек.");
                }

                throw;
            }
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}