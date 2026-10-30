using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using Forms = System.Windows.Forms;

namespace dRz.Infrastructure.Services.Message
{
    /// <summary>Оконная реализация обычных и интерактивных сообщений.</summary>
    public class WindowMessageService : IWindowMessageService
    {
        private readonly string _productTitlePrefix;
        private readonly IntPtr _cadWindowHandle;

        /// <summary>Создаёт оконный сервис сообщений с привязкой диалогов к окну CAD-приложения.</summary>
        /// <param name="productTitlePrefix">Префикс заголовка окна, обычно содержащий название и версию продукта.</param>
        /// <param name="handleProvider">Поставщик дескриптора главного окна CAD-приложения.</param>
        public WindowMessageService(string productTitlePrefix, IWindowHandleProvider handleProvider)
        {
            _productTitlePrefix = productTitlePrefix;
            _cadWindowHandle = handleProvider.Handle;
        }

        /// <summary>Показывает оконное сообщение об исключении.</summary>
        /// <param name="ex">Исключение, сведения о котором требуется показать.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; в сборке Debug выводится перед текстом.</param>
        public void ErrorMessage(Exception ex, string title = "", [CallerMemberName] string? caller = null)
        {
            ActivateCadWindow();
            MessageBox.Show(FormatMessage(ex.ToString(), caller),
                BuildTitle(title), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>Показывает оконное сообщение об ошибке, при необходимости дополняя его исключением.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="ex">Необязательное исключение.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; в сборке Debug выводится перед текстом.</param>
        public void ErrorMessage(string message, Exception? ex = null, string title = "", [CallerMemberName] string? caller = null)
        {
            ActivateCadWindow();
            string text = ex == null ? message : $"{ex}\n{message}";
            MessageBox.Show(FormatMessage(text, caller),
                BuildTitle(title), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>Показывает информационное окно.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; в сборке Debug выводится перед текстом.</param>
        public void InfoMessage(string message, string title = "", [CallerMemberName] string? caller = null)
        {
            ActivateCadWindow();
            MessageBox.Show(FormatMessage(message, caller),
                BuildTitle(title), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>Показывает окно предупреждения.</summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="title">Дополнительный заголовок окна.</param>
        /// <param name="caller">Имя вызывающего метода; в сборке Debug выводится перед текстом.</param>
        public void WarningMessage(string message, string title = "", [CallerMemberName] string? caller = null)
        {
            ActivateCadWindow();
            MessageBox.Show(FormatMessage(message, caller),
                BuildTitle(title), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>Показывает запрос с вариантами «Прервать», «Повторить» и «Пропустить».</summary>
        /// <param name="message">Текст запроса.</param><param name="title">Дополнительный заголовок окна.</param><param name="caller">Имя вызывающего метода.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskAbortRetryIgnore(string message, string title = "", [CallerMemberName] string? caller = null) => ShowPrompt(message, title, caller, Forms.MessageBoxButtons.AbortRetryIgnore, Forms.MessageBoxIcon.Warning);

        /// <summary>Показывает запрос с вариантами «ОК» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param><param name="title">Дополнительный заголовок окна.</param><param name="caller">Имя вызывающего метода.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskOkCancel(string message, string title = "", [CallerMemberName] string? caller = null) => ShowPrompt(message, title, caller, Forms.MessageBoxButtons.OKCancel, Forms.MessageBoxIcon.Question);

        /// <summary>Показывает запрос с вариантами «Повторить» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param><param name="title">Дополнительный заголовок окна.</param><param name="caller">Имя вызывающего метода.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskRetryCancel(string message, string title = "", [CallerMemberName] string? caller = null) => ShowPrompt(message, title, caller, Forms.MessageBoxButtons.RetryCancel, Forms.MessageBoxIcon.Question);

        /// <summary>Показывает запрос с вариантами «Да» и «Нет».</summary>
        /// <param name="message">Текст запроса.</param><param name="title">Дополнительный заголовок окна.</param><param name="caller">Имя вызывающего метода.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskYesNo(string message, string title = "", [CallerMemberName] string? caller = null) => ShowPrompt(message, title, caller, Forms.MessageBoxButtons.YesNo, Forms.MessageBoxIcon.Question);

        /// <summary>Показывает запрос с вариантами «Да», «Нет» и «Отмена».</summary>
        /// <param name="message">Текст запроса.</param><param name="title">Дополнительный заголовок окна.</param><param name="caller">Имя вызывающего метода.</param>
        /// <returns>Результат выбора пользователя.</returns>
        public MessageResult AskYesNoCancel(string message, string title = "", [CallerMemberName] string? caller = null) => ShowPrompt(message, title, caller, Forms.MessageBoxButtons.YesNoCancel, Forms.MessageBoxIcon.Question);

        private MessageResult ShowPrompt(
            string message,
            string? title,
            string? caller,
            Forms.MessageBoxButtons buttons,
            Forms.MessageBoxIcon icon)
        {
            ActivateCadWindow();

            Forms.DialogResult result = _cadWindowHandle == IntPtr.Zero
                ? Forms.MessageBox.Show(
                    FormatMessage(message, caller),
                    BuildTitle(title),
                    buttons,
                    icon)
                : Forms.MessageBox.Show(
                    new WindowOwner(_cadWindowHandle),
                    FormatMessage(message, caller),
                    BuildTitle(title),
                    buttons,
                    icon);

            return result switch
            {
                Forms.DialogResult.OK => MessageResult.Ok,
                Forms.DialogResult.Cancel => MessageResult.Cancel,
                Forms.DialogResult.Yes => MessageResult.Yes,
                Forms.DialogResult.No => MessageResult.No,
                Forms.DialogResult.Retry => MessageResult.Retry,
                Forms.DialogResult.Abort => MessageResult.Abort,
                Forms.DialogResult.Ignore => MessageResult.Ignore,
                _ => MessageResult.None
            };
        }

        private string BuildTitle(string? title)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                return $"{_productTitlePrefix}: {title}";
            }
            else
            {
                return $"{_productTitlePrefix}";
            }
        }

        private static string FormatMessage(string message, string? caller)
        {
            string _caller = "";
#if DEBUG
            _caller = $"{caller} >> ";
#endif
            return _caller + message;
        }

        private void ActivateCadWindow()
        {
            if (_cadWindowHandle != IntPtr.Zero)
            {
                SetForegroundWindow(_cadWindowHandle);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>Предоставляет WinForms-диалогу HWND окна nanoCAD в качестве владельца.</summary>
        private sealed class WindowOwner : Forms.IWin32Window
        {
            public WindowOwner(IntPtr handle) => Handle = handle;

            public IntPtr Handle { get; }
        }
    }
}