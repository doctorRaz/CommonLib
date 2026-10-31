using dRz.Common.NC.Infrastructure;
using dRz.Common.NC.Services.Updater;
using dRz.Updater.Services;
using System.ComponentModel;
using Teigha.Runtime;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// ручной запуск проверки обновления
    /// </summary>
    public class UpdateCheck
    {
        /// <summary>
        /// Запускает ручную проверку обновлений
        /// </summary>
        [CommandMethod(CommandPrefix.Value + "_UpdateCheck", CommandFlags.Session)]
        [Description("Проверка обновлений")]
        public static void UpdateCheckRun()
        {
            //команд метод точка входа в приложение, поэтому можно использовать сервисы напрямую
            //создаем экземпляр класса UpdateAsync и запускаем метод UpdateAsyncRun с параметром UpdateLaunchMode.Manual
            UpdateAsync updateAsync = new UpdateAsync(AddOnContext.NLogFactory,
                                                        AddOnContext.Msg,
                                                        AddOnContext.MsgP);

            _ = updateAsync.UpdateAsyncRun(UpdateLaunchMode.Manual,
                                            AddOnContext.AddonSettings,
                                            AddOnContext.AddOnInfo);
        }
    }
}