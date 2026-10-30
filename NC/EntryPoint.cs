using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.AddOnRuntime;
using dRz.Common.NC.Infrastructure;
using dRz.Common.NC.Services.Updater;
using dRz.Updater.Services;
using System;

#if !TEST

using dRz.Common.NC.Commands;

#endif

#if NC

using App = HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Rtm = Teigha.Runtime;
using dRz.Common.NC.Commands.Common;

#elif AC

using Autodesk.AutoCAD.DatabaseServices;
using App = Autodesk.AutoCAD.ApplicationServices;
using Cad = Autodesk.AutoCAD.ApplicationServices.Application;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Gem = Autodesk.AutoCAD.Geometry;
using Ed = Autodesk.AutoCAD.EditorInput;
using Rtm = Autodesk.AutoCAD.Runtime;

#endif

namespace dRz.NC
{
    /// <summary>
    /// Точка входа аддона для инициализации и завершения работы в CAD-хосте.
    /// </summary>
    public class EntryPoint
#if !TEST
        : Rtm.IExtensionApplication
#endif
    {
        private static bool _isAddOnCompositionRoot;//контейнер наполнен

        private IAddonSettings _settings = null!;// Настройки аддона, полученные из контейнера сервисов.

        private IDrzLogger _logger = null!;// Логгер для записи сообщений в журнал.

        private IDrzLoggerFactory _loggerFactory = null!;// Фабрика логгеров для создания логгеров в других классах.

        private IMessageService _message = null!;// Сервис пользовательских сообщений.

        private IWindowMessageService _windowMessage = null!;// Сервис оконных сообщений.

        private IMessagePromptService _messagePrompt = null!;// Сервис пользовательских сообщений с запросом.

        private IAddOnInfo _addOnInfo = null!;// Сведения о сборке аддона.

        private ICadInfo _cadInfo = null!;// Сведения о CAD-хосте.

        private ISysInfo _sysInfo = null!;// Сведения о системе.

        /// <summary>
        /// Инициализирует аддон: регистрирует сервисы и настройки, запускает проверку обновлений
        /// и выполняет оставшуюся инициализацию.
        /// </summary>
        /// <remarks>
        /// Метод вызывается CAD-хостом через <see cref="Rtm.IExtensionApplication"/>.
        /// Ошибки инициализации перехватываются здесь, чтобы не передавать исключение хосту.
        /// </remarks>
        public void Initialize()
        {
            try
            {
                // Инициализация сервисов аддона и получение необходимых сервисов из контейнера.
                InitializeServices();

                // Проверяем, что аддон загружен в родной каталог, и предупреждаем пользователя о возможных ошибках.

                if (!_addOnInfo.IsProductDirectory)
                {
                    _windowMessage.WarningMessage(
                        $"Приложение не в своем родном каталоге" +
                        $"\n{_addOnInfo.PackageDirectory}" +
                        $"\nили не найден {_addOnInfo.Product}*" +
                        $".package" +
                        $"\n\nВозможны ошибки в работе и при обновлении!");
                }

                // Запускаем асинхронное обновление и очистку служебных файлов в фоновом процессе.
                StartUpdate();

                // Записываем сведения о сборке аддона и CAD-хосте в журнал и выводим приветственное сообщение.
                ReportInitialization();
            }
            catch (Exception ex) // Ошибка общей инициализации: сообщаем пользователю и записываем её в журнал, если сервисы доступны.
                                 // Если ошибка поднялась сюда, то вероятно, что аддон неработоспособен
            {
                HandleError(ex, "Приложение не загружено!!!", isFatal: true, notifyUser: true);
            }
        }

        /// <summary>
        /// Запускает обновление и очистку служебных файлов в фоновом асинхронном процессе.
        /// </summary>
        /// <remarks>
        /// Задача намеренно не ожидается, чтобы не блокировать синхронную инициализацию CAD-хоста.
        /// Исключения, возникшие до передачи управления асинхронному методу, обрабатываются здесь;
        /// ошибки внутри задачи должны обрабатываться самим <see cref="UpdateAsync"/>.
        /// </remarks>
        private void StartUpdate()
        {
            try
            {
                // Создаём экземпляр класса UpdateAsync для выполнения обновления и очистки служебных файлов.
                UpdateAsync updateAsync = new UpdateAsync(_loggerFactory, _message, _messagePrompt);

                // Запускаем асинхронное обновление и очистку служебных файлов в фоновом процессе.
                _ = updateAsync.UpdateAsyncRun(UpdateLaunchMode.Automatic,
                                                _settings,
                                                _addOnInfo);
            }
            catch (Exception ex)
            {
                // Ошибка запуска фоновой операции не должна прерывать инициализацию CAD-хоста.
                HandleError(ex, "Не удалось запустить фоновое обновление.", isFatal: false, notifyUser: false);
            }
        }

        /// <summary>
        /// Создаёт корень композиции при первом вызове и получает необходимые сервисы аддона.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Не удалось создать корень композиции или получить необходимые сервисы.
        /// </exception>
        private void InitializeServices()
        {
            try
            {
                // Проверяем, был ли уже создан корень композиции. Если нет, создаём его и регистрируем сервисы.
                if (!_isAddOnCompositionRoot)
                {
                    //***** РЕГИСТРИРУЕМ СЕРВИСЫ *************
                    // Регистрируем сервисы один раз при инициализации CAD-хоста.
                    AddOnCompositionRoot root = new AddOnCompositionRoot(typeof(EntryPoint).Assembly);

                    // Передаём зарегистрированные сервисы в общий контекст аддона.
                    AddOnContext.Initialize(root.Get<IAddOnServices>());

                    _isAddOnCompositionRoot = true;// Корень композиции успешно инициализирован.
                }

                // Получаем фабрику логгеров из контейнера сервисов.
                _loggerFactory = AddOnContext.NLogFactory;

                // Получаем логгер для текущего класса из фабрики логгеров.
                _logger = _loggerFactory.GetLogger(typeof(EntryPoint));

                // Получаем сервис сообщений из контейнера сервисов.
                _message = AddOnContext.Msg;

                // Получаем сервис оконных сообщений из контейнера сервисов.
                _windowMessage = AddOnContext.MsgW;

                // Получаем сервис сообщений с запросом из контейнера сервисов.
                _messagePrompt = AddOnContext.MsgP;

                // Получаем настройки аддона из контейнера сервисов.
                _settings = AddOnContext.AddonSettings;

                // Получаем сведения о сборке аддона из контейнера сервисов.
                _addOnInfo = AddOnContext.AddOnInfo;

                // Получаем сведения о CAD-хосте из контейнера сервисов.
                _cadInfo = AddOnContext.CadInfo;

                // Получаем сведения о системе из контейнера сервисов.
                _sysInfo = AddOnContext.SysInfo;
            }
            catch (Exception ex)
            {
                // Логирование и уведомление выполняются в Initialize(), чтобы не записывать ошибку дважды.
                // Сохраняем исходное исключение во внутренней цепочке для диагностики.
                throw new InvalidOperationException("AddOnCompositionRoot initialization failed", ex);
            }
        }

        /// <summary>
        /// Записывает сведения об аддоне и CAD-хосте в журнал и выводит приветственное сообщение.
        /// </summary>
        private void ReportInitialization()
        {
            try
            {
                _logger.Trace(_addOnInfo.ToLongString());
                _logger.Trace(_cadInfo.ToLongString());
                _logger.Trace(_sysInfo.ToLongString());

                _message.InfoMessage($"Hello {_addOnInfo.ToShortString()} for {_cadInfo}");

                if (_settings.General.ShowCommandInfoOnStartup)
                {
#if !TEST
                    // Показываем сведения о зарегистрированных командах только при включённой настройке.
                    CommandInfo.CmdInfo();

#else
                    _message.InfoMessage("Cmd Info");
#endif
                }
            }
            catch (Exception ex)
            {
                // Ошибка вспомогательного отчёта не должна прерывать инициализацию.
                HandleError(ex, "Не удалось вывести сведения при инициализации.", isFatal: false, notifyUser: false);
            }
        }

        /// <summary>
        /// Централизованно записывает ошибку и при необходимости уведомляет пользователя.
        /// Сбой логирования или уведомления не должен скрывать исходное исключение.
        /// </summary>
        /// <param name="ex">Исходное исключение.</param>
        /// <param name="message">Контекст ошибки.</param>
        /// <param name="isFatal">Признак фатальной ошибки инициализации.</param>
        /// <param name="notifyUser">Нужно ли уведомлять пользователя.</param>
        private void HandleError(Exception ex, string message, bool isFatal, bool notifyUser)
        {
            string errorMessage = message;

            if (isFatal && _logger != null)
            {
                errorMessage += "\nОтправьте разработчику логфайлы";

                errorMessage += $": {_addOnInfo?.ProductDataDirectory}\\logs";
            }

            // Ошибка логирования не должна мешать резервному уведомлению пользователя.
            if (_logger != null)
            {
                try
                {
                    if (isFatal)
                    {
                        _logger.Fatal(ex, errorMessage);
                    }
                    else
                    {
                        _logger.Error(ex, errorMessage);
                    }
                }
                catch
                {
                    // Не подменяем исходную ошибку сбоем логгера.
                }
            }
            if (!notifyUser)
            {
                return;
            }

            if (_message != null)
            {
                try
                {
                    _message.ErrorMessage(errorMessage, ex);
                    return;
                }
                catch
                {
                    // Если сервис сообщений недоступен, используем резервный вывод.
                }
            }

            string fallbackMessage =
                $"Exception: {errorMessage}\nСкопируйте и отправьте разработчику это сообщение\n{ex.Message}\n{ex.StackTrace}";

#if !TEST
            try
            {
                App.Document document = App.Application.DocumentManager.MdiActiveDocument;

                if (document != null)
                {
                    Editor editor = document.Editor;
                    editor?.WriteMessage(fallbackMessage);
                }
                else
                {
                    App.Application.ShowAlertDialog(fallbackMessage);
                }
            }
            catch
            {
                // CAD API может быть недоступен в аварийном сценарии; пробуем показать диалог.
                try
                {
                    App.Application.ShowAlertDialog(fallbackMessage);
                }
                catch
                {
                    Console.WriteLine(fallbackMessage);
                }
            }
#else
            Console.WriteLine(fallbackMessage);
#endif
        }

        /// <summary>
        ///  Выполняет завершающие действия при выгрузке аддона из CAD-хоста.
        /// </summary>
        public void Terminate()
        {
            _logger?.Trace($"{nameof(EntryPoint)}: Terminate");

            // Сохранение настроек здесь не выполняется: состояние настроек управляется отдельным сервисом.
            //dsf.SaveDataSet();
            //dsf.SetGeneralSetting.WriteValue("TEST", "terminate", "true");
        }
    }
}
