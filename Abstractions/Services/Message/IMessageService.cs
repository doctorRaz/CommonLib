using System;
using System.Runtime.CompilerServices;

namespace dRz.Abstractions.Services.Message
{
    /// <summary>Общий сервис сообщений для пользователя.</summary>
    public interface IMessageService
    {
        /// <summary>Сообщение об исключении. Как правило, блокирует дальнейшее выполнение кода.</summary>
        /// <param name="ex">Полное описание ошибки.</param>
        /// <param name="caller">Вызывающий метод для диагностики; вычисляется автоматически.</param>
        /// <param name="title">Необязательное название компонента, сформировавшего сообщение.</param>
        void ErrorMessage(Exception ex, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Сообщение об исключении. Как правило, блокирует дальнейшее выполнение кода.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="ex">Дополнительное исключение.</param>
        /// <param name="caller">Вызывающий метод для диагностики; вычисляется автоматически.</param>
        /// <param name="title">Необязательное название компонента, сформировавшего сообщение.</param>
        void ErrorMessage(string message, Exception? ex = null, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Информационное сообщение.</summary>
        /// <param name="message">Выводимый текст.</param>
        /// <param name="caller">Вызывающий метод для диагностики; вычисляется автоматически.</param>
        /// <param name="title">Необязательное название компонента, сформировавшего сообщение.</param>
        void InfoMessage(string message, string title = "", [CallerMemberName] string? caller = null);

        /// <summary>Предупреждение, не требующее критической остановки выполнения.</summary>
        /// <param name="message">Выводимый текст.</param>
        /// <param name="caller">Вызывающий метод для диагностики; вычисляется автоматически.</param>
        /// <param name="title">Необязательное название компонента, сформировавшего сообщение.</param>
        void WarningMessage(string message, string title = "", [CallerMemberName] string? caller = null);
    }
}