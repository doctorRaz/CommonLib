using NLog;
using System.IO;

namespace dRz.LogBootstrap.Diagnostics
{
    internal static class LogLevelReader
    {
        /// <summary>
        /// Читает LogLevel из файла.
        /// При отсутствии файла, ошибке чтения, пустом или некорректном значении
        /// возвращает заданный уровень по умолчанию.
        /// </summary>
        /// <param name="path">Путь к файлу настройки.</param>
        /// <param name="defaultLevel">Уровень по умолчанию.</param>
        /// <returns>Уровень из файла или <paramref name="defaultLevel"/>.</returns>
        internal static LogLevel GetLevelFromFile(string path, LogLevel defaultLevel)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return defaultLevel;
                }

                using FileStream fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                using StreamReader sr = new StreamReader(fs);

                string? text = sr.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return defaultLevel;
                }

                return TryFromString(text, defaultLevel);
            }
            catch
            {
                return defaultLevel;
            }
        }

        private static LogLevel TryFromString(string levelName, LogLevel defaultLevel)
        {
            try
            {
                return LogLevel.FromString(levelName);
            }
            catch
            {
                return defaultLevel;
            }
        }
    }
}