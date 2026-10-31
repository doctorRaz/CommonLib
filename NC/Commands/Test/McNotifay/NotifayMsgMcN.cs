#if DEBUG
using dRz.Abstractions.Services.Message;
using dRz.Common.NC.Infrastructure;
using Teigha.Runtime;

namespace dRz.NC.Commands.Test.McNotifay
{
    public class NotifayMsgMcN
    {

        private static IMcNotificatorMessageService _msgMcN = AddOnContext.MsgN;

        private static System.Exception ex = new System.Exception("Properties is null");

        [CommandMethod(CommandPrefix.Value + "_noti1", CommandFlags.Session)]
        public static void noti1()
        {
            _msgMcN.InfoMessage("Без иконки");
        }

        [CommandMethod(CommandPrefix.Value + "_noti2", CommandFlags.Session)]
        public static void noti2()
        {

            _msgMcN.ErrorMessage("С иконкой err", ex);
        }

        [CommandMethod(CommandPrefix.Value + "_noti20", CommandFlags.Session)]
        public static void noti20()
        {

            _msgMcN.ErrorMessage("С иконкой err");
        }

        [CommandMethod(CommandPrefix.Value + "_noti3", CommandFlags.Session)]
        public static void noti3()
        {
            _msgMcN.ErrorMessage(ex);
        }

        [CommandMethod(CommandPrefix.Value + "_noti4", CommandFlags.Session)]
        public static void InfoMessageNoti()
        {
            _msgMcN.WarningMessage($"{AddOnContext.SysInfo.ToLongString()}");

        }
    }
}
#endif