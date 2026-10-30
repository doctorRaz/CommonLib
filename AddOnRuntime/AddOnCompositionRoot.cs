using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Infrastructure.Infrastructure;
using dRz.Infrastructure.NC.Services;
using dRz.Infrastructure.NC.Services.Message;
using dRz.Infrastructure.Services.Message;
using dRz.LogBootstrap;
using dRz.Settings;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.IO;
using System.Reflection;

namespace dRz.AddOnRuntime
{
    /// <summary> Наполнение SimpleInjector объектами </summary>
    /// <seealso cref="System.IDisposable" />
    public class AddOnCompositionRoot : IDisposable
    {
        #region Private Fields

        private readonly Container _container;

        private bool _disposed;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddOnCompositionRoot"/> class.
        /// </summary>
        /// <param name="addOnAssembly">The add on assembly.</param>
        public AddOnCompositionRoot(Assembly addOnAssembly)
        {
            _container = new Container();

            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            RegisterInfrastructure(_container, addOnAssembly);

            RegisterServices(_container);

            // регистрируем собственно сам контейнер
            // всегда доступен через AddOnContext
            // можно передавать в другие сборки параметром или по сервисам /требуются интерфейсы Abstractions/
            _container.RegisterInstance<IAddOnServices>(new AddOnServices(_container));

            _container.Verify();
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Создать скоуп для Scoped-сервисов
        /// </summary>
        public Scope BeginScope() => AsyncScopedLifestyle.BeginScope(_container);

        /// <summary>
        /// Выполняет определяемые приложением задачи, связанные с удалением, высвобождением или сбросом неуправляемых ресурсов.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _container.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Получить сервис
        /// </summary>
        public TService Get<TService>() where TService : class
        {
            return _container.GetInstance<TService>();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>Registers the infrastructure.</summary>
        /// <param name="container">The container.</param>
        /// <param name="addOnAssembly">The add on assembly.</param>
        private void RegisterInfrastructure(Container container, Assembly addOnAssembly)
        {
            //регистрируемая сборка
            container.RegisterInstance(addOnAssembly);

            //инфо о системе
            container.RegisterSingleton<ISysInfo, SysInfo>();

            //инфо о хосте
            container.RegisterSingleton<ICadInfo, CadInfo>();

            // AddOnInfoRegistry регистрируем интерфейс
            //      создания IAddOnInfo (только в контейнере)
            //      получения IAddOnInfo
            //
            // хранится в ConcurrentDictionary<string, IAddOnInfo> _addons = new();
            //      ключ полный путь к файлу addOnAssembly
            //
            //доступ на запись только из контейнера,
            //чтение при получении в контейнере IAddOnInfoRegistry
            container.RegisterSingleton<IAddOnInfoRegistry, AddOnInfoRegistry>();

            //регистрация-получение новых IAddOnInfo и получение сущ объекта по полному пути к файлу addOnAssembly
            // регистрация только из контейнера
            container.RegisterSingleton<IAddOnInfo>(() => AddOnInfoProvider.GetOrAdd(addOnAssembly));
        }

        /// <summary>Registers the services.</summary>
        /// <param name="container">The container.</param>
        private void RegisterServices(Container container)
        {
            container.RegisterSingleton<IWindowHandleProvider, CadWindowProvider>();//IntPtr Handle

            // серви сообщений ком строки
            container.RegisterSingleton<ICommandLineMessageService, CommandLineMessageService>();

            //сервис мультикад сообщений
            //   внутри маршрутизация:
            //      если вызов без документа, то попытается отправить в мультикад окошко
            //          если неуспех выведет алерт кад
            container.RegisterSingleton<IMcNotificatorMessageService, McNotificatorMessageServise>();

            //сервис Win сообщений
            container.RegisterSingleton<IWindowMessageService>(() => new WindowMessageService(
                container.GetInstance<IAddOnInfo>().ProductTitlePrefix,
                container.GetInstance<IWindowHandleProvider>()));

            //сервис маршрутизации сообщений,
            //  документ есть ->ком строка
            //  документа нет -> Win
            container.RegisterSingleton<MessageService>();

            container.RegisterSingleton<IMessageService>(() => container.GetInstance<MessageService>());

            container.RegisterSingleton<IMessagePromptService>(() => container.GetInstance<MessageService>());

            // сервис документов
            container.RegisterSingleton<IDocumentService, DocumentService>();

            // фабрика логгера, одна на ProduktName
            //  при повторном создании с тем же продукт наме, будет использоваться эта же фабрика
            //      если даже внутри одного аддона, но в другой сборке будет создан контейнер и имя продукта другое,
            //      в этой сборке будет использоваться другая фабрика
            container.RegisterSingleton<IDrzLoggerFactory>(() => NLogBootstrap.GetLoggerFactory(container.GetInstance<IAddOnInfo>()));

            //запрашивает фабрику из словаря при каждом обращении
            //container.GetOrAdd<IDrzLoggerFactory>(() => NLogBootstrap.GetLoggerFactory(container.GetInstance<IAddOnInfo>()), Lifestyle.Transient);

            // Настройки принадлежат основному аддону и загружаются один раз.

            container.RegisterSingleton<JsonSettingsStore>(() =>
            {
                IAddOnInfo addonInfo = container.GetInstance<IAddOnInfo>();

                string filePath = Path.Combine(
                                                addonInfo.ProductDataDirectory,
                                                "Settings",
                                                addonInfo.HostFamily,
                                                "GeneralSettings.json"
                                               );

                return new JsonSettingsStore(
                    filePath,
                    container.GetInstance<IDrzLoggerFactory>(),
                    container.GetInstance<IMessageService>());
            });

            container.RegisterSingleton<SettingsManager>();

            container.RegisterSingleton<IAddonSettings>(() => container.GetInstance<SettingsManager>().Load());
        }

        #endregion Private Methods
    }
}