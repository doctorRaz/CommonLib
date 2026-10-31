using NLog;
using NLog.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dRz.LogBootstrap.Diagnostics
{
    /// <summary>
    /// Вспомогательные методы для настройки и обслуживания внутреннего логгера NLog.
    /// </summary>
    public static class InternalLoggerHelpers
    {
        // 10 MB
        /// <summary>
        /// Однократно настраивает Internal Logger: вывод в Debug Output, Console и файл.
        /// Запрос уровня <see cref="LogLevel.Off"/> не инициализирует логгер.
        /// После инициализации повторные запросы с уровнем, отличным от <see cref="LogLevel.Off"/>,
        /// записываются в Internal Logger как отклонённая повторная настройка; конфигурация не меняется.
        /// Инициализация определяется по текущему уровню Internal Logger, отдельный флаг не используется.
        /// </summary>
        /// <param name="typeCaller">Имя вызывающего компонента для диагностических сообщений.</param>
        /// <param name="requestedLevel">Запрашиваемый уровень Internal Logger.</param>
        /// <param name="logName">Семейство продукта, используемое в имени файла внутреннего лога.</param>
        /// <param name="logDir">Общий каталог внутренних логов компании; если <see langword="null"/>, используется каталог <c>%TEMP%\dRzTools\logs</c>.</param>
        public static void ConfigureInternalLogger(string typeCaller, LogLevel requestedLevel, string logName, string? logDir = null)
        {
            lock (_lock)
            {
                // Уровень Off не должен фиксировать инициализацию: другой аддон ещё может включить диагностику.
                if (requestedLevel == LogLevel.Off)
                {
                    return;
                }

                // Уровень InternalLogger — единственный признак того, что логгер уже инициализирован.
                if (InternalLogger.LogLevel != LogLevel.Off)
                {
                    // Повторную настройку не применяем, но фиксируем попытку в текущем InternalLogger.
                    // Error проходит фильтр при текущих уровнях Trace, Debug, Info, Warn и Error.
                    InternalLogger.Log(
                        LogLevel.Error,
                        $"{typeCaller} InternalLogger is already initialized. " +
                        $"RequestedLevel={requestedLevel}, CurrentLevel={InternalLogger.LogLevel}. " +
                        "Configuration remains unchanged.");

                    return;
                }

                logDir ??= Path.Combine(Path.GetTempPath(), "dRzTools", "logs");

                _logDir = logDir;
                Directory.CreateDirectory(LogDir);

                string currentLogPath = GetInternalLogPath();

                // Удаляем старые файлы внутренних логов, сохраняя последние.
                RotateOldLogs();

                // Если текущий файл больше 10 МБ, переименовываем его в архивный.
                CheckCurrentFileSize(currentLogPath);

                // Пишем в файл.
                InternalLogger.LogFile = currentLogPath;

                // Пишем в Debug Output.
                InternalLogger.LogWriter = new OutputDebugTextWriter();

                InternalLogger.LogToConsole = true;

                // Устанавливаем уровень первого аддона, запросившего диагностику.
                InternalLogger.LogLevel = requestedLevel;

                // Все исключения.
                LogManager.ThrowExceptions = false;

                // Ошибки конфигурации.
                LogManager.ThrowConfigExceptions = true;

                InternalLogger.Info($"{typeCaller} InternalLogger Initialized, Level={requestedLevel}");
            }
        }

        /// <summary>Gets the log dir.</summary>
        /// <value>The log dir.</value>
        internal static string LogDir => _logDir;

        /// <summary>
        /// Gets a value indicating whether Internal Logger is initialized.
        /// </summary>
        /// <value><see langword="true"/> if its level is not <see cref="LogLevel.Off"/>; otherwise, <see langword="false"/>.</value>
        internal static bool Initialized => InternalLogger.LogLevel != LogLevel.Off;

        //private const string logName = "nlog-drzTools-internal";
        private const int MaxArchiveFiles = 10;

        private const int MaxFileSizeBytes = 10 * 1024 * 1024;
        private static readonly object _lock = new();
        private static string _logDir = string.Empty;

        /// <summary>
        /// Checks the size of the current file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private static void CheckCurrentFileSize(string filePath)
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                if (file.Exists && file.Length > MaxFileSizeBytes)
                {
                    string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
                    string newName = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:HHmmss}.log";
                    file.MoveTo(Path.Combine(dir, newName));
                }
            }
            catch { }
        }

        private static string GetInternalLogPath() =>
                   Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}_{Guid.NewGuid():N}_internal.log");

        /// <summary>Удаляет старые файлы внутренних логов, оставляя не более заданного количества.</summary>
        private static void RotateOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogDir))
                {
                    return;
                }

                // Все аддоны одного процесса используют общий каталог внутренних логов.
                // Сортируем файлы от новых к старым.
                List<FileInfo> files = new DirectoryInfo(LogDir)
                                         .GetFiles("*_internal.log")
                                         .OrderByDescending(f => f.LastWriteTime)
                                         .ToList();

                // Если файлов больше 5, удаляем лишние.
                foreach (FileInfo file in files.Skip(MaxArchiveFiles))
                {
                    file.Delete();
                }
            }
            catch { }
        }
    }
}