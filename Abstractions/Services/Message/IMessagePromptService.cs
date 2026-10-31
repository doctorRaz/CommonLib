using System.Runtime.CompilerServices;

namespace dRz.Abstractions.Services.Message
{
    /// <summary>Сервис интерактивных сообщений.</summary>
    public interface IMessagePromptService
    {
        /// <summary>Запрос подтверждения действия у пользователя.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Необязательное название компонента, сформировавшего сообщение.</param>
        /// <param name="caller">Вызывающий метод для диагностики; вычисляется автоматически.</param>
        MessageResult AskYesNo(string message, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Запрос подтверждения или отмены действия.</summary>
        MessageResult AskOkCancel(string message, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Запрос повтора действия или отмены.</summary>
        MessageResult AskRetryCancel(string message, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Запрос выбора «Да», «Нет» или «Отмена».</summary>
        MessageResult AskYesNoCancel(string message, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Запрос выбора «Прервать», «Повторить» или «Пропустить».</summary>
        MessageResult AskAbortRetryIgnore(string message, string title = "", [CallerMemberName] string? caller = null);
    }
}