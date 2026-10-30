using dRz.Abstractions.Services.Message;
using System;
using System.Runtime.CompilerServices;

#if !TEST

using HostMgd.ApplicationServices;

#endif

namespace dRz.Infrastructure.NC.Services.Message
{
    /// <summary>
    /// Выводит сообщения в командную строку nanoCAD с резервным отображением уведомления.
    /// </summary>
    public class CommandLineMessageService : ICommandLineMessageService
    {
        /// <summary>Выводит обычное сообщение без префикса уровня.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void ConsoleMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage(string.Empty, message, caller, title);

        /// <summary>Выводит предупреждение.</summary>
        /// <param name="message">Текст предупреждения.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void WarningMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage("Warn", message, caller, title);

        /// <summary>Выводит исключение вместе с его сообщением и трассировкой стека.</summary>
        /// <param name="ex">Исключение для отображения.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(Exception ex, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage("Error", $"{ex.Message}\n{ex.StackTrace}", caller, title);

        /// <summary>Выводит текст ошибки и, если передано, сведения об исключении.</summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="ex">Необязательное исключение с дополнительными сведениями.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(string message, Exception? ex = null, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage("Error", ex == null ? message : $"{message}\n{ex.Message}\n{ex.StackTrace}", caller, title);

        /// <summary>Выводит информационное сообщение.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void InfoMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage("", message, caller, title);

        /// <summary>
        /// Формирует сообщение и выводит его в доступный канал хоста.
        /// </summary>
        /// <param name="prefix">Префикс уровня сообщения.</param>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="caller">Имя вызывающего члена.</param>
        /// <param name="title">Заголовок сообщения.</param>
        private void WriteMessage(string prefix, string message, string? caller, string? title)
        {
            string _caller = "";
#if DEBUG
            _caller = string.IsNullOrWhiteSpace(caller) ? string.Empty : $"{caller} >> ";
#endif

            string formatted =
                "\n" +
                (string.IsNullOrWhiteSpace(title) ? string.Empty : $"[{title}]: ") +
                (string.IsNullOrWhiteSpace(prefix) ? string.Empty : $"[{prefix}] ") +
                (_caller) +
                message;

#if !TEST
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null && doc.Editor != null)
            {
                doc.Editor.WriteMessage(formatted);
            }
            else
            {
                try
                {
                    McNotificatorMessageServise.WriteMessage(formatted, caller);
                }
                catch
                {
                    Application.ShowAlertDialog(formatted);
                }
            }
#else
            Console.WriteLine(formatted);
#endif
        }
    }
}