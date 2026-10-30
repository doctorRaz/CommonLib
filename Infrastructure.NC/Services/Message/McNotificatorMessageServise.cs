using drz.MulticadInterop.McNotificator;
using dRz.Abstractions.Services.Message;
using System;
using System.Runtime.CompilerServices;

namespace dRz.Infrastructure.NC.Services.Message
{
    /// <summary>
    /// Адаптер уведомлений nanoCAD на основе McNotificator.
    /// </summary>
    public class McNotificatorMessageServise : IMcNotificatorMessageService
    {
        /// <summary>Показывает уведомление-предупреждение.</summary>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="title">Необязательный заголовок.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void WarningMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage(NotificationType.neWarning, message, caller, title);

        /// <summary>Показывает уведомление об исключении с сообщением и трассировкой стека.</summary>
        /// <param name="ex">Исключение для отображения.</param>
        /// <param name="title">Необязательный заголовок.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(Exception ex, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage(NotificationType.neError, $"{ex.Message}\n{ex.StackTrace}", caller, title);

        /// <summary>Показывает текст ошибки и, если передано, сведения об исключении.</summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="ex">Необязательное исключение.</param>
        /// <param name="title">Необязательный заголовок.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(string message, Exception? ex = null, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage(NotificationType.neError,
                ex == null ? message : $"{message}\n{ex.Message}\n{ex.StackTrace}", caller, title);

        /// <summary>Показывает информационное уведомление.</summary>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="title">Необязательный заголовок.</param>
        /// <param name="caller">Имя вызывающего члена; по умолчанию подставляется компилятором.</param>
        public void InfoMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            WriteMessage(NotificationType.neUsual, message, caller, title);

        /// <summary>
        /// Создаёт уведомление указанного типа с форматированным текстом.
        /// </summary>
        /// <param name="notificationType">Тип уведомления McNotificator.</param>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="caller">Имя вызывающего члена.</param>
        /// <param name="title">Необязательный заголовок.</param>
        public static void WriteMessage(NotificationType notificationType, string message, string? caller, string? title = null) =>
            MulticadNotificator.CreateMessage(Formatted(message, caller, title), notificationType);

        /// <summary>
        /// Создаёт обычное уведомление для резервного вывода сообщений командной строки.
        /// </summary>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="caller">Имя вызывающего члена.</param>
        public static void WriteMessage(string message, string? caller) =>
            MulticadNotificator.CreateMessage(Formatted(message, caller, null), NotificationType.neCure);

        /// <summary>Добавляет к сообщению заголовок и имя вызывающего члена, если они заданы.</summary>
        /// <param name="message">Исходный текст.</param>
        /// <param name="caller">Имя вызывающего члена.</param>
        /// <param name="title">Заголовок сообщения.</param>
        /// <returns>Форматированный текст уведомления.</returns>
        private static string Formatted(string message, string? caller, string? title)
        {
            string source = string.IsNullOrWhiteSpace(title) ? string.Empty : $"[{title}] ";
            return source + (string.IsNullOrWhiteSpace(caller) ? string.Empty : $"{caller} >> ") + message;
        }
    }
}