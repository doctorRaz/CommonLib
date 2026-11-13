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
        /// <summary>Задать переопределение уровня логирования.</summary>
        [Description("Задать")]
        Apply,

        /// <summary>Удалить переопределение уровня логирования.</summary>
        [Description("Сбросить")]
        Delete,

        /// <summary>Показать заданный и фактически действующий уровни логирования.</summary>
        [Description("Показать")]
        Status
    }

    /// <summary>
    /// Допустимые значения уровня логирования.
    /// </summary>
    public enum LogLevelValue
    {
        /// <summary>Максимально подробный уровень.</summary>
        Trace,

        /// <summary>Отладочные сообщения.</summary>
        Debug,

        /// <summary>Информационные сообщения.</summary>
        Info,

        /// <summary>Предупреждения.</summary>
        Warn,

        /// <summary>Ошибки.</summary>
        Error,

        /// <summary>Критические ошибки.</summary>
        Fatal,

        /// <summary>Логирование отключено.</summary>
        Off
    }

    /// <summary>
    /// Предоставляет ввод действия и уровня логирования, не зависящий от CAD-хоста.
    /// </summary>
    public interface ILogLevelPrompt
    {
        /// <summary>
        /// Запрашивает действие для указанной настройки.
        /// </summary>
        /// <param name="settingName">Название настраиваемого параметра.</param>
        /// <returns>Выбранное действие или <see langword="null"/>, если ввод отменён.</returns>
        LogLevelAction? ChooseAction(string settingName);

        /// <summary>
        /// Запрашивает уровень логирования для указанной настройки.
        /// </summary>
        /// <param name="settingName">Название настраиваемого параметра.</param>
        /// <returns>Выбранный уровень или <see langword="null"/>, если ввод отменён.</returns>
        LogLevelValue? ChooseLevel(string settingName);
    }

    /// <summary>
    /// Управляет файлами переопределения уровней логирования.
    /// </summary>
    public sealed class LogLevelSettingsManager
    {
        private readonly IMessageService _message;
        private readonly ILogLevelPrompt _prompt;

        /// <summary>
        /// Инициализирует менеджер настройки уровней логирования.
        /// </summary>
        /// <param name="message">Сервис вывода сообщений пользователю.</param>
        /// <param name="prompt">Поставщик пользовательского ввода.</param>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если <paramref name="message"/> или <paramref name="prompt"/> равен <see langword="null"/>.
        /// </exception>
        public LogLevelSettingsManager(IMessageService message, ILogLevelPrompt prompt)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        /// <summary>
        /// Выполняет выбранное пользователем действие с файлом переопределения уровня логирования.
        /// </summary>
        /// <param name="settingsDirectory">Каталог, в котором хранятся файлы настроек.</param>
        /// <param name="fileName">Имя файла переопределения уровня логирования.</param>
        /// <param name="settingName">Название настраиваемого параметра для отображения пользователю.</param>
        /// <param name="actualLevel">Фактически действующий уровень логирования; используется при просмотре статуса.</param>
        /// <remarks>
        /// Если пользователь отменяет выбор действия или уровня, операция не выполняется.
        /// После задания или удаления переопределения требуется перезапустить nanoCAD,
        /// чтобы изменение вступило в силу.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Возникает, если каталог настроек, имя файла или название настройки пусты либо содержат только пробелы.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если обязательный строковый параметр равен <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Возникает, если поставщик ввода вернул неизвестное значение действия.
        /// </exception>
        public void ManageLevel(string settingsDirectory, string fileName, string settingName, string actualLevel)
        {
            if (string.IsNullOrWhiteSpace(settingsDirectory))
            {
                throw new ArgumentException("Каталог настроек не задан.", nameof(settingsDirectory));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Имя файла не задано.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(settingName))
            {
                throw new ArgumentException("Название настройки не задано.", nameof(settingName));
            }

            LogLevelAction? action = _prompt.ChooseAction(settingName);
            if (action == null)
            {
                return;
            }

            if (!Enum.IsDefined(typeof(LogLevelAction), action.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Выбрано неизвестное действие.");
            }

            string path = Path.Combine(settingsDirectory, fileName);
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
        /// Возвращает наиболее подробный уровень логирования, включённый для указанного логгера.
        /// </summary>
        /// <param name="logger">Логгер, для которого определяется фактически включённый уровень.</param>
        /// <returns>Название наиболее подробного включённого уровня или <c>Off</c>, если логирование отключено.</returns>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если <paramref name="logger"/> равен <see langword="null"/>.
        /// </exception>
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

            if (!Enum.IsDefined(typeof(LogLevelValue), answer.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(answer), answer, "Выбран неизвестный уровень логирования.");
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