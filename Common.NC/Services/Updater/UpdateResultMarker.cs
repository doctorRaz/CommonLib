using dRz.Abstractions.Updater;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using System;
using System.IO;
using System.Text.Json;

namespace dRz.Common.NC.Services.Updater
{
    /// <summary>Передаёт результат установки обновления в следующий запуск nanoCAD.</summary>
    public static class UpdateResultMarker
    {
        private const int CurrentSchemaVersion = 1;
        private const string FileName = "UpdateResult.json";

        private sealed class MarkerData
        {
            public int SchemaVersion { get; set; }
            public string Result { get; set; } = string.Empty;
            public string PreviousVersion { get; set; } = string.Empty;
            public string NewVersion { get; set; } = string.Empty;
            public DateTime CreatedUtc { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>Атомарно сохраняет итог установки вне каталога заменяемых файлов.</summary>
        public static void Write(
            string markerDirectory,
            UpdateResult result,
            string previousVersion,
            string newVersion,
            string message,
            IDrzLogger logger)
        {
            string path = Path.Combine(markerDirectory, FileName);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                Directory.CreateDirectory(markerDirectory);

                MarkerData data = new MarkerData
                {
                    SchemaVersion = CurrentSchemaVersion,
                    Result = result.ToString(),
                    PreviousVersion = previousVersion,
                    NewVersion = newVersion,
                    CreatedUtc = DateTime.UtcNow,
                    Message = message
                };

                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data));
                File.Move(temporaryPath, path, true);
            }
            catch (Exception ex)
            {
                TryLogError(logger, ex, "Не удалось сохранить результат установки обновления.");
                TryDelete(temporaryPath, logger);
            }
        }

        /// <summary>Показывает результат предыдущей установки и удаляет обработанный маркер.</summary>
        public static void ReportAndDelete(string markerDirectory, IMessageService message, IDrzLogger logger)
        {
            string path = Path.Combine(markerDirectory, FileName);
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                MarkerData? data = JsonSerializer.Deserialize<MarkerData>(File.ReadAllText(path));
                if (data is null || data.SchemaVersion != CurrentSchemaVersion ||
                    !Enum.TryParse(data.Result, out UpdateResult result))
                {
                    try { logger.Warn("Маркер результата обновления имеет неизвестный формат."); } catch { }
                    return;
                }

                string text = string.IsNullOrWhiteSpace(data.Message)
                    ? $"Результат установки обновления: {result}."
                    : data.Message;

                if (result == UpdateResult.Installed)
                {
                    message.InfoMessage(text, title: "Updater");
                }
                else
                {
                    message.WarningMessage(text, title: "Updater");
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                TryLogError(logger, ex, "Не удалось прочитать или показать результат предыдущего обновления.");
            }
        }

        private static void TryLogError(IDrzLogger logger, Exception exception, string message)
        {
            try { logger.Error(exception, message); } catch { }
        }

        private static void TryDelete(string path, IDrzLogger logger)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                TryLogError(logger, ex, "Не удалось удалить временный файл маркера обновления.");
            }
        }
    }
}