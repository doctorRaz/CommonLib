using dRz.Common.NC.Services.Updater;
using System.ComponentModel;
using Teigha.Runtime;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// Ручной запуск проверки обновлений.
    /// </summary>
    public class UpdateCheck
    {
        /// <summary>
        /// Запускает ручную проверку обновлений через общий координатор EntryPoint.
        /// </summary>
        [CommandMethod(CommandPrefix.Value + "_UpdateCheck", CommandFlags.Session)]
        [Description("Проверка обновлений")]
        public static void UpdateCheckRun()
        {
            UpdateAsync.RequestManualCheck();
        }
    }
}