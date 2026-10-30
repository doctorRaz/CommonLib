using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using System;
using System.Runtime.CompilerServices;

namespace dRz.Infrastructure.Services.Message
{
    /// <summary>
    /// Универсальный сервис сообщений. Выбирает способ вывода в зависимости от состояния nanoCAD.
    /// Интерактивные запросы всегда показываются оконной реализацией.
    /// </summary>
    public sealed class MessageService : IMessageService, IMessagePromptService
    {
        private readonly ICommandLineMessageService _commandLine;
        private readonly IDocumentService _documentService;
        private readonly IWindowMessageService _window;

        /// <summary>Создаёт сервис, выбирающий способ вывода сообщений в зависимости от состояния CAD-документа.</summary>
        /// <param name="commandLine">Сервис вывода сообщений в командную строку.</param>
        /// <param name="window">Сервис оконных сообщений и интерактивных запросов.</param>
        /// <param name="documentService">Сервис, сообщающий, активен ли CAD-документ.</param>
        /// <exception cref="ArgumentNullException">Один из обязательных сервисов равен <see langword="null"/>.</exception>
        public MessageService(
            ICommandLineMessageService commandLine,
            IWindowMessageService window,
            IDocumentService documentService)
        {
            _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        private IMessageService Current =>
            _documentService.IsActive ? _commandLine : _window;

        /// <summary>Выводит сообщение об ошибке, сформированное из исключения.</summary>
        /// <param name="ex">Исключение, сведения о котором требуется показать.</param>
        /// <param name="title">Дополнительный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(Exception ex, string title = "", [CallerMemberName] string? caller = null) =>
            Current.ErrorMessage(ex, title, caller);

        /// <summary>Выводит сообщение об ошибке и при необходимости сведения об исключении.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="ex">Необязательное исключение, добавляемое к сообщению.</param>
        /// <param name="title">Дополнительный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        public void ErrorMessage(string message, Exception? ex = null, string title = "", [CallerMemberName] string? caller = null) =>
            Current.ErrorMessage(message, ex, title, caller);

        /// <summary>Выводит информационное сообщение.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Дополнительный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        public void InfoMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            Current.InfoMessage(message, title, caller);

        /// <summary>Выводит предупреждение.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Дополнительный заголовок сообщения.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        public void WarningMessage(string message, string title = "", [CallerMemberName] string? caller = null) =>
            Current.WarningMessage(message, title, caller);

        /// <summary>Показывает оконный запрос с вариантами «Да» и «Нет».</summary>
        /// <param name="message">Текст запроса.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskYesNo(string message, string title = "", [CallerMemberName] string? caller = null) =>
            _window.AskYesNo(message, title, caller);

        /// <summary>Показывает оконный запрос с вариантами «ОК» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskOkCancel(string message, string title = "", [CallerMemberName] string? caller = null) =>
            _window.AskOkCancel(message, title, caller);

        /// <summary>Показывает оконный запрос с вариантами «Повторить» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskRetryCancel(string message, string title = "", [CallerMemberName] string? caller = null) =>
            _window.AskRetryCancel(message, title, caller);

        /// <summary>Показывает оконный запрос с вариантами «Да», «Нет» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskYesNoCancel(string message, string title = "", [CallerMemberName] string? caller = null) =>
            _window.AskYesNoCancel(message, title, caller);

        /// <summary>Показывает оконный запрос с вариантами «Прервать», «Повторить» и «Пропустить».</summary>
        /// <param name="message">Текст запроса.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; по умолчанию подставляется компилятором.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskAbortRetryIgnore(string message, string title = "", [CallerMemberName] string? caller = null) =>
            _window.AskAbortRetryIgnore(message, title, caller);
    }
}