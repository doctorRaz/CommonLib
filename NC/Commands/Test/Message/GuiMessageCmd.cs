#if DEBUG
using dRz.Common.NC.Infrastructure;
using Teigha.Runtime;

namespace dRz.NC.Commands.Test.Message
{
    public class GuiMessageCmd
    {

        [CommandMethod(CommandPrefix.Value + "_info-message", CommandFlags.Session)]
        public static void InfoMessageCommand()
        {
            AddOnContext.MsgW.InfoMessage("Info message");

        }
    }
}
#endif