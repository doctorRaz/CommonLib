#if DEBUG
using dRz.Abstractions.Services.Message;
using dRz.Common.NC.Infrastructure;
using Teigha.Runtime;

namespace dRz.NC.Commands.Test.Message
{
    public class PromtMessage
    {
        private IMessagePromptService _msgP = AddOnContext.MsgP;
        private IMessageService _msg = AddOnContext.Msg;

        [CommandMethod(CommandPrefix.Value + "_promt-message", CommandFlags.Session)]
        public void ConsoleMessageCommand()
        {
            MessageResult res = _msgP.AskAbortRetryIgnore("AskAbortRetryIgnore");
            _msg.InfoMessage($"{res}");

            res = _msgP.AskOkCancel("AskOkCancel");
            _msg.InfoMessage($"{res}");

            res = _msgP.AskRetryCancel("AskRetryCancel");
            _msg.InfoMessage($"{res}");

            res = _msgP.AskYesNoCancel("AskAbortRetryIgnore");
            _msg.InfoMessage($"{res}");
        }
    }
}
#endif