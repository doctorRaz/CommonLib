#if DEBUG
using dRz.Abstractions.Services.Message;
using dRz.Common.NC.Infrastructure;
using Teigha.Runtime;


namespace dRz.NC.Commands.Test.NewCmd
{
    public class NewInfoCmd
    {

        private static IMessageService _msg = AddOnContext.Msg;

        private static IWindowMessageService _msgGui = AddOnContext.MsgW;


        [CommandMethod(CommandPrefix.Value + "_console-New", CommandFlags.Session)]
        public static void ConsoleNewCmd()
        {
            _msg.InfoMessage($"{AddOnContext.CadInfo}");

            _msg.InfoMessage($"{AddOnContext.SysInfo}");

            _msg.WarningMessage($"{AddOnContext.AddOnInfo}");

        }

        [CommandMethod(CommandPrefix.Value + "_info-New", CommandFlags.Session)]
        public static void GuiNewCmd()
        {
            _msgGui.InfoMessage($"{AddOnContext.CadInfo}");

            _msgGui.WarningMessage($"{AddOnContext.SysInfo}");

            System.Exception ex = new System.Exception("test err");
            _msgGui.ErrorMessage($"{AddOnContext.AddOnInfo.ToLongString()}", ex);
        }

        [CommandMethod(CommandPrefix.Value + "_console-Long", CommandFlags.Session)]
        public static void ConsoleLongCmd()
        {

            _msg.WarningMessage($"{AddOnContext.AddOnInfo.ToLongString()}");

            _msg.InfoMessage($"{AddOnContext.SysInfo.ToLongString()}");


        }
    }
}
#endif