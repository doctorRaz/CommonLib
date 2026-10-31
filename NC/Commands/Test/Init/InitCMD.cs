#if DEBUG

using dRz.Common.NC;
using dRz.Common.NC.Infrastructure;
using Rtm = Teigha.Runtime;
using Scm = System.ComponentModel;

namespace dRz.NC.Commands.Test.Init
{
    public class InitCMD
    {
        [Rtm.CommandMethod(CommandPrefix.Value + "_инит", Rtm.CommandFlags.Session)]
        [Scm.Description("ручной инит загрузчика для SPEC")]
        public static void test()
        {
            AddOnContext.Msg.InfoMessage($"инит SPEC");

            EntryPoint entryPoint = new EntryPoint();

            entryPoint.Initialize();
        }

        [Rtm.CommandMethod("-" + CommandPrefix.Value + "_console-message-test", Rtm.CommandFlags.Session)]
        public static void ConsoleMessageCommand()
        {
            AddOnContext.MsgCmd.ConsoleMessage("Console message");
        }
    }
}

#endif