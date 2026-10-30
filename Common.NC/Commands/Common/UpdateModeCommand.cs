using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Common.NC.CadServices;
using dRz.Common.NC.Infrastructure;
using HostMgd.EditorInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using App = HostMgd.ApplicationServices;
using Rtm = Teigha.Runtime;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// Изменение режима и периода автоматического обновления через командную строку nanoCAD.
    /// </summary>
    public sealed class UpdateModeCommand
    {
        private IDrzLogger _logger = AddOnContext.NLogFactory.GetLogger(typeof(UpdateModeCommand));

        private IMessageService _msg = AddOnContext.Msg;

        private IAddonSettings _settings = AddOnContext.AddonSettings;

        /// <summary>
        /// Показывает текущие настройки обновления и позволяет изменить их.
        /// Команда: drz_UpdateMode.
        /// </summary>
        [Rtm.CommandMethod(CommandPrefix.Value + "_UpdateMode", Rtm.CommandFlags.Session)]
        [Description("Настройка режима обновления")]
        public void SetUpdateMode()
        {
            App.Document? document = App.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            IAddonSettings settings = _settings;
            IUpdateSettings updateSettings = settings.Updates;

            UpdateMode previousMode = updateSettings.Mode;
            int previousInterval = updateSettings.UpdateCheckIntervalDays;

            List<KeywordOption> modeKeywords = new List<KeywordOption>
            {
                new KeywordOption(nameof(UpdateMode.Disabled), UpdateMode.Disabled, previousMode == UpdateMode.Disabled),
                new KeywordOption(nameof(UpdateMode.CheckAndNotify), UpdateMode.CheckAndNotify, previousMode == UpdateMode.CheckAndNotify),
                new KeywordOption(nameof(UpdateMode.CheckAndInstall), UpdateMode.CheckAndInstall, previousMode == UpdateMode.CheckAndInstall)
            };

            Enum? modeAnswer = KeywordPrompt.GetAnswer(
                document,
                modeKeywords,
                $"\nТекущий режим: {GetDescription(previousMode)}. Новый режим: ",
                allowNone: true);

            if (modeAnswer == null)
            {
                return;
            }

            UpdateMode newMode = (UpdateMode)modeAnswer;

            // Период можно изменить независимо от режима, в том числе если режим оставлен прежним.
            List<KeywordOption> intervalKeywords = new List<KeywordOption>
            {
                new KeywordOption(nameof(AskYesNo.Yes), AskYesNo.Yes),
                new KeywordOption(nameof(AskYesNo.No), AskYesNo.No, isDefault: true)
            };

            Enum? intervalAnswer = KeywordPrompt.GetAnswer(
                document,
                intervalKeywords,
                $"\nТекущий период проверки: {previousInterval} дн. Изменить период? ",
                allowNone: true);

            int newInterval = previousInterval;
            if (intervalAnswer is AskYesNo.Yes)
            {
                PromptIntegerOptions daysOptions = new PromptIntegerOptions(
                    $"\nПериод автоматической проверки в днях (1–30) [{previousInterval}]: ")
                {
                    AllowNone = true,
                    DefaultValue = previousInterval,
                    LowerLimit = 1,
                    UpperLimit = 30,
                    AllowNegative = false,
                    AllowZero = false
                };

                PromptIntegerResult daysResult = document.Editor.GetInteger(daysOptions);
                if (daysResult.Status == PromptStatus.OK)
                {
                    newInterval = daysResult.Value;
                }
                // Отмена ввода периода не отменяет выбранный режим.
            }

            bool modeChanged = newMode != previousMode;
            bool intervalChanged = newInterval != previousInterval;

            if (!modeChanged && !intervalChanged)
            {
                _msg.InfoMessage("Настройки обновления не изменены.");
                return;
            }

            updateSettings.Mode = newMode;
            updateSettings.UpdateCheckIntervalDays = newInterval;

            try
            {
                settings.Save();

                if (modeChanged)
                {
                    _msg.InfoMessage($"Режим обновления сохранён: {GetDescription(newMode)}.");
                }

                if (intervalChanged)
                {
                    _msg.InfoMessage($"Период автоматической проверки сохранён: {newInterval} дн.");
                }
            }
            catch (Exception ex)
            {
                // При ошибке сохранения восстанавливаем обе настройки в памяти.
                updateSettings.Mode = previousMode;
                updateSettings.UpdateCheckIntervalDays = previousInterval;

                _msg.ErrorMessage(
                    "Не удалось сохранить настройки обновления. Восстановлены прежние значения.",
                    ex);
            }
        }

        private static string GetDescription(UpdateMode mode)
        {
            return mode switch
            {
                UpdateMode.Disabled => "отключено (обязательные обновления устанавливаются)",
                UpdateMode.CheckAndNotify => "проверка с подтверждением установки",
                UpdateMode.CheckAndInstall => "автоматическая установка необязательных обновлений",
                _ => mode.ToString()
            };
        }
    }
}