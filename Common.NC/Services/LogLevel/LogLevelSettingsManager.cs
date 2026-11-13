using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using System;
using System.ComponentModel;
using System.IO;

namespace dRz.Common.NC.Services.LogLevel
{
    /// <summary>
    /// Действие пользователя при настройке уровня логирования.
    /// </summary>
    public enum LogLevelAction
    {
        [Description("Задать")]
        Apply,

        [Description("Сбросить")]
        Delete,

        [Description("Показать")]
        Status
    }

    /// <summary>
    /// Допустимые значения уровня логирования.
    /// </summary>
    public enum LogLevelValue
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
        Off
    }

    /// <summary>
    /// Предоставляет ввод действия и уровня, не зависящий от CAD-хоста.
    /// </summary>
    public interface ILogLevelPrompt
    {
        LogLevelAction? ChooseAction(string settingName);

        LogLevelValue? ChooseLevel(string settingName);
    }

    /// <summary>
    /// Управляет файлами переопределения уровней логирования.
    /// </summary>
    public sealed class LogLevelSettingsManager
    {
        private readonly IMessageService _message;
        private readonly ILogLevelPrompt _prompt;

        public LogLevelSettingsManager(IMessageService message, ILogLevelPrompt prompt)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        /// <summary>
        /// Запускает выбранное действие для файла переопределения уровня.
        /// </summary>
        public void ManageLevel(string assemblyDirectory, string fileName, string settingName, string actualLevel)
        {
            LogLevelAction? action = _prompt.ChooseAction(settingName);
            if (action == null)
            {
                return;
            }

            string path = Path.Combine(assemblyDirectory, fileName);
            switch (action.Value)
            {
                case LogLevelAction.Apply:
                    ApplyLevel(path, fileName, settingName);
                    break;

                case LogLevelAction.Delete:
                    DeleteLevel(path, fileName, settingName);
                    break;

                case LogLevelAction.Status:
                    ShowStatus(path, fileName, settingName, actualLevel);
                    break;
            }
        }

        /// <summary>
        /// Возвращает минимальный фактически включённый уровень логгера.
        /// </summary>
        public static string GetEffectiveLogLevel(IDrzLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (logger.IsTraceEnabled)
            {
                return "Trace";
            }

            if (logger.IsDebugEnabled)
            {
                return "Debug";
            }

            if (logger.IsInfoEnabled)
            {
                return "Info";
            }

            if (logger.IsWarnEnabled)
            {
                return "Warn";
            }

            if (logger.IsErrorEnabled)
            {
                return "Error";
            }

            if (logger.IsFatalEnabled)
            {
                return "Fatal";
            }

            return "Off";
        }

        private void ApplyLevel(string path, string fileName, string settingName)
        {
            LogLevelValue? answer = _prompt.ChooseLevel(settingName);
            if (answer == null)
            {
                return;
            }

            string level = answer.Value.ToString();
            try
            {
                File.WriteAllText(path, level + Environment.NewLine);
                _message.InfoMessage(
                    $"Для {settingName} установлен уровень {level} в файле {fileName}. Для применения изменения необходимо перезапустить nanoCAD.");
            }
            catch (Exception ex)
            {
                _message.ErrorMessage($"Не удалось записать файл {fileName}.", ex);
            }
        }

        private void DeleteLevel(string path, string fileName, string settingName)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _message.InfoMessage(
                        $"Переопределение {settingName} удалено (файл {fileName}). Для применения изменения необходимо перезапустить nanoCAD.");
                }
                else
                {
                    _message.InfoMessage(
                        $"Файл {fileName} отсутствует. Используется программное значение {settingName}.");
                }
            }
            catch (Exception ex)
            {
                _message.ErrorMessage($"Не удалось удалить файл {fileName}.", ex);
            }
        }

        private void ShowStatus(string path, string fileName, string settingName, string actualLevel)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _message.InfoMessage($"Файл {fileName} отсутствует. Фактический уровень {settingName}: {actualLevel}.");
                    return;
                }

                string[] lines = File.ReadAllLines(path);
                string level = lines.Length > 0 ? lines[0].Trim() : string.Empty;
                _message.InfoMessage(
                    string.IsNullOrWhiteSpace(level)
                        ? $"Файл {fileName} существует, но первая строка пуста. Фактический уровень {settingName}: {actualLevel}."
                        : $"В файле {fileName} задан уровень: {level}. Фактический уровень {settingName}: {actualLevel}.");
            }
            catch (Exception ex)
            {
                _message.ErrorMessage($"Не удалось прочитать файл {fileName}.", ex);
            }
        }
    }
}