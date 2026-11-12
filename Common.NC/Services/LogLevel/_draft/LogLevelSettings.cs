//using dRz.Abstractions.Updater;
//using dRz.Abstractions.Logger;
//using dRz.Abstractions.Services.Message;
//using System;
//using System.IO;
//using System.Text.Json;

//namespace dRz.Common.NC.Services.LogLevel
//{
//    /// <summary>Передаёт результат установки обновления в следующий запуск nanoCAD.</summary>
//    public static class LogLevelSettings
//    {

//    /// <summary>Путь к файлу настроек.</summary>
//        private static readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogLevelSettings.json");
//        /// <summary>Сохраняет настройки уровня логирования в файл.</summary>
//        /// <param name="logLevel">Уровень логирования.</param>
//        public static void SaveSettings(LogLevel logLevel)
//        {
//            var settings = new LogLevelSettingsData { LogLevel = logLevel };
//            var json = JsonSerializer.Serialize(settings);
//            File.WriteAllText(_settingsFilePath, json);
//        }
//        /// <summary>Загружает настройки уровня логирования из файла.</summary>
//        /// <returns>Настройки уровня логирования.</returns>
//        public static LogLevel LoadSettings()
//        {
//            if (!File.Exists(_settingsFilePath))
//            {
//                return LogLevel.Info; // Возвращаем уровень по умолчанию, если файл не найден
//            }
//            var json = File.ReadAllText(_settingsFilePath);
//            var settings = JsonSerializer.Deserialize<LogLevelSettingsData>(json);
//            return settings?.LogLevel ?? LogLevel.Info; // Возвращаем уровень по умолчанию, если десериализация не удалась
      
//    }
//}