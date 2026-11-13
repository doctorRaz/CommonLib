using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Common.NC.Infrastructure;
using dRz.NC;
using System;
using System.Diagnostics;
using System.IO;

using Sys = System;

namespace dRz.Test.Console.Updater
{
    /// <summary>
    /// Предоставляет консольный стенд для проверки сервисов и настроек обновления.
    /// </summary>
    public class FakeCAD
    {
        private readonly IDrzLogger _logger = null!;
        private readonly IMessageService _msg = null!;
        private readonly IAddonSettings _settings = null!;
        private readonly IAddOnInfo _addonInfo = null!;
        private readonly Stopwatch _sw;

        /// <summary>Initializes a new instance of the <see cref="FakeCAD"/> class.</summary>
        public FakeCAD()
        {
            _sw = Stopwatch.StartNew();

            EntryPoint entryPoint = new();

            try
            {
                entryPoint.Initialize();

                _logger = AddOnContext.NLogFactory.GetLogger<FakeCAD>();

                _msg = AddOnContext.Msg;

                _addonInfo = AddOnContext.AddOnInfo;

                _msg.InfoMessage($"{nameof(_addonInfo.IsNotNetworkPath)}: {_addonInfo.IsNotNetworkPath.ToString()}");
                _settings = AddOnContext.AddonSettings;
            }
            catch (Exception ex)
            {
                _logger?.Fatal(ex, "Продолжение не возможно");

                if (_msg != null)
                {
                    _msg.ErrorMessage("Продолжение не возможно", ex);
                }
                else
                {
                    Sys.Console.Error.WriteLine($"Продолжение не возможно: {ex}");
                }
            }
            finally
            {
                entryPoint.Terminate();
                _logger?.Trace($"{nameof(FakeCAD)}: Terminate");
            }
        }

        //[STAThread]
        private static void Main(string[] args)
        {
            //AddOnRuntime.AddOnCompositionRoot root = ConteinerCtx.Run();

            //IAddOnInfo addInf = AddOnContext.AddOnInfo;

            FakeCAD fakeCad = new FakeCAD();

            fakeCad.SelectShowCommandInfoOnStartup();
            fakeCad.SelectUpdateMode();
            fakeCad.SelectUpdateCheckInterval();

            // Проверяем управление файлами переопределения уровней логирования.
            fakeCad.SelectLogLevel();
            fakeCad.SelectDiagnosticMode();

            _ = ConsoleReadKey(fakeCad._msg, fakeCad._sw);
        }

        private static ConsoleKeyInfo ConsoleReadKey(IMessageService? msg, Stopwatch? sw = null)
        {
            string res = sw != null ? $"Total time {sw.Elapsed}\n" : string.Empty;
            string message = $"{res}Press any key to exit...";

            if (msg != null)
            {
                msg.InfoMessage(message);
            }
            else
            {
                Sys.Console.WriteLine(message);
            }

            return Sys.Console.ReadKey();
        }

        /// <summary>
        /// Позволяет включить или отключить вывод сведений о командах при запуске аддона.
        /// </summary>
        private void SelectShowCommandInfoOnStartup()
        {
            while (true)
            {
                bool currentValue = _settings.General.ShowCommandInfoOnStartup;
                _msg.InfoMessage($"Показывать сведения о командах при запуске: {(currentValue ? "Да" : "Нет")}");
                _msg.InfoMessage("Выберите: 1 — Да, 0 — Нет.");
                _msg.InfoMessage("Enter — оставить текущее значение.");

                string? input = Sys.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }

                bool selected;
                if (input.Trim() == "1")
                {
                    selected = true;
                }
                else if (input.Trim() == "0")
                {
                    selected = false;
                }
                else
                {
                    _msg.InfoMessage("Неверный выбор. Введите 0, 1 или Enter.");
                    continue;
                }

                _settings.General.ShowCommandInfoOnStartup = selected;

                try
                {
                    _settings.Save();
                    _msg.InfoMessage($"Вывод сведений о командах при запуске {(selected ? "включён" : "отключён")}.");
                }
                catch (Exception ex)
                {
                    _settings.General.ShowCommandInfoOnStartup = currentValue;
                    _logger?.Error(ex, "Не удалось сохранить настройку вывода сведений о командах.");
                    _msg.ErrorMessage("Не удалось сохранить настройку. Оставлено прежнее значение.", ex);
                }

                return;
            }
        }

        /// <summary>
        /// Позволяет вручную изменить период автоматической проверки обновлений.
        /// </summary>
        private void SelectUpdateCheckInterval()
        {
            while (true)
            {
                int currentInterval = _settings.Updates.UpdateCheckIntervalDays;
                _msg.InfoMessage($"Текущий период проверки обновлений: {currentInterval} дн.");
#if DEBUG
                const int minInterval = 0;
                _msg.InfoMessage("Введите период в днях (0–30; 0 — проверка при каждом запуске).");
#else
                const int minInterval = 1;
                _msg.InfoMessage("Введите период в днях (1–30).");
#endif
                _msg.InfoMessage("Enter — оставить текущий период.");

                string? input = Sys.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }

                if (!int.TryParse(input.Trim(), out int selected) ||
                    selected < minInterval ||
                    selected > 30)
                {
                    _msg.InfoMessage($"Неверный период. Допустимое значение: {minInterval}–30.");
                    continue;
                }

                _settings.Updates.UpdateCheckIntervalDays = selected;

                try
                {
                    _settings.Save();
                    _msg.InfoMessage($"Период проверки обновлений сохранён: {selected} дн.");
                }
                catch (Exception ex)
                {
                    _settings.Updates.UpdateCheckIntervalDays = currentInterval;
                    _logger?.Error(ex, "Не удалось сохранить период проверки обновлений.");
                    _msg.ErrorMessage("Не удалось сохранить период. Оставлен прежний период.", ex);
                }

                return;
            }
        }

        /// <summary>
        /// Предлагает настроить уровень основного логгера через файл <c>logger.level</c>.
        /// </summary>
        /// <remarks>
        /// Файл содержит уровень логирования в первой строке. Изменение применяется после перезапуска приложения.
        /// </remarks>
        private void SelectLogLevel()
        {
            SelectLogSetting("logger.level", "уровня основного логгера");
        }

        /// <summary>
        /// Предлагает настроить уровень внутренней диагностики NLog через файл <c>diagnostic.level</c>.
        /// </summary>
        /// <remarks>
        /// Файл содержит уровень диагностики в первой строке. Изменение применяется после перезапуска приложения.
        /// </remarks>
        private void SelectDiagnosticMode()
        {
            SelectLogSetting("diagnostic.level", "внутренней диагностики NLog");
        }

        /// <summary>
        /// Предлагает установить уровень, удалить файл переопределения или посмотреть его содержимое.
        /// </summary>
        /// <param name="fileName">Имя файла настройки, расположенного рядом с аддоном.</param>
        /// <param name="settingName">Название настройки для сообщений пользователю.</param>
        /// <remarks>
        /// Изменение файла не влияет на уже созданную конфигурацию логирования; требуется перезапуск приложения.
        /// </remarks>
        private void SelectLogSetting(string fileName, string settingName)
        {
            // Используем каталог сборки аддона, а не текущий рабочий каталог процесса.
            string path = Path.Combine(_addonInfo.AssemblyDirectory, fileName);

            while (true)
            {
                _msg.InfoMessage($"Настройка {settingName}. Файл: {fileName}");
                _msg.InfoMessage("Выберите: 0 — установить уровень, 1 — удалить файл, 2 — показать состояние.");
                _msg.InfoMessage("Enter — пропустить настройку.");

                string? input = Sys.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }

                switch (input.Trim())
                {
                    case "0":
                        SetLogSettingLevel(path, fileName, settingName);
                        return;

                    case "1":
                        DeleteLogSetting(path, fileName, settingName);
                        return;

                    case "2":
                        ShowLogSetting(path, fileName, settingName);
                        return;

                    default:
                        _msg.InfoMessage("Неверный выбор. Введите 0, 1, 2 или Enter.");
                        break;
                }
            }
        }

        /// <summary>
        /// Записывает выбранный уровень в первую строку файла настройки.
        /// </summary>
        /// <param name="path">Полный путь к файлу настройки.</param>
        /// <param name="fileName">Имя файла для сообщений пользователю.</param>
        /// <param name="settingName">Название настраиваемого уровня для сообщений пользователю.</param>
        private void SetLogSettingLevel(string path, string fileName, string settingName)
        {
            // Список должен соответствовать уровням, поддерживаемым конфигурацией логирования.
            string[] levels = { "Trace", "Debug", "Info", "Warn", "Error", "Fatal", "Off" };

            while (true)
            {
                _msg.InfoMessage($"Выберите уровень для {settingName}:");
                _msg.InfoMessage("0 — Trace, 1 — Debug, 2 — Info, 3 — Warn, 4 — Error, 5 — Fatal, 6 — Off.");
                _msg.InfoMessage("Enter — отменить.");

                string? input = Sys.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }

                if (!int.TryParse(input.Trim(), out int index) || index < 0 || index >= levels.Length)
                {
                    _msg.InfoMessage("Неверный выбор. Введите число от 0 до 6 или Enter.");
                    continue;
                }

                string level = levels[index];

                try
                {
                    File.WriteAllText(path, level + Environment.NewLine);
                    _msg.InfoMessage(
                        $"Для {settingName} установлен уровень {level} в файле {fileName}. Для применения изменения необходимо перезапустить приложение.");
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, $"Не удалось записать файл {fileName}.");
                    _msg.ErrorMessage($"Не удалось записать файл {fileName}.", ex);
                }

                return;
            }
        }

        /// <summary>
        /// Удаляет файл переопределения уровня логирования.
        /// </summary>
        /// <param name="path">Полный путь к файлу настройки.</param>
        /// <param name="fileName">Имя файла для сообщений пользователю.</param>
        /// <param name="settingName">Название настройки для сообщения о возврате к программному значению.</param>
        private void DeleteLogSetting(string path, string fileName, string settingName)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _msg.InfoMessage(
                        $"Переопределение {settingName} удалено (файл {fileName}). Для применения изменения необходимо перезапустить приложение.");
                }
                else
                {
                    _msg.InfoMessage(
                        $"Файл {fileName} отсутствует. Используется программное значение {settingName}.");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"Не удалось удалить файл {fileName}.");
                _msg.ErrorMessage($"Не удалось удалить файл {fileName}.", ex);
            }
        }

        /// <summary>
        /// Показывает наличие файла переопределения и значение его первой строки.
        /// </summary>
        /// <param name="path">Полный путь к файлу настройки.</param>
        /// <param name="fileName">Имя файла для сообщений пользователю.</param>
        /// <param name="settingName">Название настройки для сообщения о программном значении.</param>
        private void ShowLogSetting(string path, string fileName, string settingName)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _msg.InfoMessage(
                        $"Файл {fileName} отсутствует. Используется программное значение {settingName}.");
                    return;
                }

                // Контракт файла использует только первую строку; остальные строки не учитываются.
                string[] lines = File.ReadAllLines(path);
                string level = lines.Length > 0 ? lines[0].Trim() : string.Empty;

                _msg.InfoMessage(
                    string.IsNullOrWhiteSpace(level)
                        ? $"Файл {fileName} существует, но первая строка пуста."
                        : $"В файле {fileName} указан уровень: {level}.");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"Не удалось прочитать файл {fileName}.");
                _msg.ErrorMessage($"Не удалось прочитать файл {fileName}.", ex);
            }
        }

        /// <summary>
        /// Позволяет вручную выбрать режим обновления для проверки в консольном тесте.
        /// </summary>
        private void SelectUpdateMode()
        {
            while (true)
            {
                _msg.InfoMessage($"Текущий режим обновления: {_settings.Updates.Mode}");
                _msg.InfoMessage("Выберите режим: 0 — Disabled, 1 — CheckAndNotify, 2 — CheckAndInstall.");
                _msg.InfoMessage("Enter — оставить текущий режим.");

                string? input = Sys.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }

                UpdateMode? selected = input.Trim() switch
                {
                    "0" => UpdateMode.Disabled,
                    "1" => UpdateMode.CheckAndNotify,
                    "2" => UpdateMode.CheckAndInstall,
                    _ => null
                };

                if (selected == null)
                {
                    _msg.InfoMessage("Неверный выбор. Введите 0, 1, 2 или Enter.");
                    continue;
                }

                UpdateMode previous = _settings.Updates.Mode;
                _settings.Updates.Mode = selected.Value;

                try
                {
                    _settings.Save();
                    _msg.InfoMessage($"Режим обновления сохранён: {_settings.Updates.Mode}");
                }
                catch (Exception ex)
                {
                    _settings.Updates.Mode = previous;
                    _logger?.Error(ex, "Не удалось сохранить режим обновления.");
                    _msg.ErrorMessage("Не удалось сохранить режим. Оставлен прежний режим.", ex);
                }

                return;
            }
        }
    }
}