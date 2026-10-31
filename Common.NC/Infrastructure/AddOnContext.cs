using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using System;

namespace dRz.Common.NC.Infrastructure
{
    /// <summary>
    /// Предоставляет командам доступ к сервисам аддона без повторного вызова Services.Get&lt;T&gt;().
    /// Контекст инициализируется точкой входа аддона.
    /// </summary>
    public static class AddOnContext
    {
        #region Private Fields

        private static IAddOnServices? _services;

        #endregion Private Fields

        #region Public Properties

        /// <summary>Список зарегистрированных аддонов.</summary>
        public static IAddOnInfoRegistry AddOnInfoRegistry => Services.Get<IAddOnInfoRegistry>();

        /// <summary>Сведения о сборке аддона.</summary>
        public static IAddOnInfo AddOnInfo => Services.Get<IAddOnInfo>();

        /// <summary>Сведения о CAD-хосте.</summary>
        public static ICadInfo CadInfo => Services.Get<ICadInfo>();

        /// <summary>Сервис работы с документом.</summary>
        public static IDocumentService DocService => Services.Get<IDocumentService>();

        /// <summary>Сведения о системе.</summary>
        public static ISysInfo SysInfo => Services.Get<ISysInfo>();

        /// <summary>Сервис сообщений командной строки.</summary>
        public static ICommandLineMessageService MsgCmd => Services.Get<ICommandLineMessageService>();

        /// <summary>Сервис оконных сообщений.</summary>
        public static IWindowMessageService MsgW => Services.Get<IWindowMessageService>();

        /// <summary>Сервис уведомлений MultiCAD.</summary>
        public static IMcNotificatorMessageService MsgN => Services.Get<IMcNotificatorMessageService>();

        /// <summary>Общий сервис пользовательских сообщений.</summary>
        public static IMessageService Msg => Services.Get<IMessageService>();

        /// <summary>Сервис сообщений с запросом пользовательского ответа.</summary>
        public static IMessagePromptService MsgP => Services.Get<IMessagePromptService>();

        /// <summary>Фабрика логгеров аддона.</summary>
        public static IDrzLoggerFactory NLogFactory => Services.Get<IDrzLoggerFactory>();

        /// <summary>Настройки аддона.</summary>
        public static IAddonSettings AddonSettings => Services.Get<IAddonSettings>();

        /// <summary>Зарегистрированные сервисы аддона.</summary>
        /// <exception cref="InvalidOperationException">Контекст ещё не инициализирован.</exception>
        public static IAddOnServices Services =>
            _services ?? throw new InvalidOperationException("AddOnContext is not initialized.");

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Инициализирует контекст сервисами, зарегистрированными точкой входа аддона.
        /// </summary>
        /// <param name="services">Сервисы аддона.</param>
        /// <exception cref="ArgumentNullException">Сервисы не заданы.</exception>
        /// <exception cref="InvalidOperationException">Контекст уже инициализирован.</exception>
        public static void Initialize(IAddOnServices services)
        {
            ArgumentNullException.ThrowIfNull(services);

            if (_services != null)
            {
                if (ReferenceEquals(_services, services))
                {
                    return;
                }

                throw new InvalidOperationException("AddOnContext уже инициализирован другими сервисами.");
            }

            _services = services;
        }

        #endregion Public Methods
    }
}
