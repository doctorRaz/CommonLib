using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Common.NC.CadServices;
using dRz.Common.NC.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using App = HostMgd.ApplicationServices;
using Rtm = Teigha.Runtime;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// Изменение общих настроек аддона через командную строку nanoCAD.
    /// </summary>
    public sealed class AddonSettingsCommand
    {
        private readonly IMessageService _msg = AddOnContext.Msg;
        private readonly IAddonSettings _settings = AddOnContext.AddonSettings;

        /// <summary>
        /// Позволяет включить или отключить вывод сведений о командах при запуске.
        /// Команда: drz_Settings.
        /// </summary>
        [Rtm.CommandMethod(CommandPrefix.Value + "_Settings", Rtm.CommandFlags.Session)]
        [Description("Настройка параметров аддона")]
        public void SetSettings()
        {
            App.Document? document = App.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            IAddonSettings settings = _settings;
            IGeneralSettings generalSettings = settings.General;
            bool previousValue = generalSettings.ShowCommandInfoOnStartup;

            List<KeywordOption> keywords = new List<KeywordOption>
            {
                new KeywordOption(nameof(AskYesNo.Yes), AskYesNo.Yes, previousValue),
                new KeywordOption(nameof(AskYesNo.No), AskYesNo.No, !previousValue)
            };

            Enum? answer = KeywordPrompt.GetAnswer(
                document,
                keywords,
                $"\nВыводить сведения о командах при запуске? Текущее значение: {(previousValue ? "Да" : "Нет")}. ",
                allowNone: true);

            if (answer == null)
            {
                return;
            }

            bool newValue = answer is AskYesNo.Yes;
            if (newValue == previousValue)
            {
                _msg.InfoMessage("Настройки не изменены.");
                return;
            }

            generalSettings.ShowCommandInfoOnStartup = newValue;

            try
            {
                settings.Save();
                _msg.InfoMessage(
                    $"Вывод сведений о командах при запуске {(newValue ? "включён" : "отключён")}.");
            }
            catch (Exception ex)
            {
                // При ошибке сохранения возвращаем прежнее значение в памяти.
                generalSettings.ShowCommandInfoOnStartup = previousValue;

                _msg.ErrorMessage(
                    "Не удалось сохранить настройки. Восстановлено прежнее значение.",
                    ex);
            }
        }
    }
}