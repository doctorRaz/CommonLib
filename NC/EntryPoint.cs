using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using dRz.AddOnRuntime;
using dRz.Common.NC.Infrastructure;
using dRz.Common.NC.Services.Updater;
using dRz.Updater.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly object _updateGate = new object();
        private UpdateAsync? _updateAsync;
        private CancellationTokenSource? _updateCancellation;
        private Task? _updateTask;
        private bool _isTerminating;

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
                UpdateAsync.SetManualCheckHandler(StartManualUpdate);

                // Показываем результат установки, выполненной при предыдущем завершении nanoCAD.
                UpdateResultMarker.ReportAndDelete(_addOnInfo.AssemblyDirectory, _message, _logger);

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
            catch (Exception ex)// Ошибка общей инициализации: сообщаем пользователю и записываем её в журнал, если сервисы доступны.
                                // Если ошибка поднялась сюда, то вероятно, что аддон неработоспособен
            {
                HandleError(ex, "Приложение не загружено!!!", isFatal: true, notifyUser: true);
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
        /// Запускает обновление и очистку служебных файлов в фоновом асинхронном процессе.
        /// </summary>
        /// <remarks>
        /// Задача намеренно не ожидается, чтобы не блокировать синхронную инициализацию CAD-хоста.
        /// Исключения, возникшие до передачи управления асинхронному методу, обрабатываются здесь;
        /// ошибки внутри задачи должны обрабатываться самим <see cref="UpdateAsync"/>.
        /// </remarks>
        private void StartUpdate()
        {
            StartUpdate(UpdateLaunchMode.Automatic);
        }

        /// <summary>
        /// Запускает подготовку обновления в указанном режиме, не устанавливая пакет.
        /// Использует единый координатор, общий для автоматической и ручной проверки.
        /// </summary>
        private void StartUpdate(UpdateLaunchMode launchMode)
        {
            try
            {
                lock (_updateGate)
                {
                    _isTerminating = false;
                    _updateCancellation?.Dispose();
                    _updateCancellation = new CancellationTokenSource();
                    _updateAsync ??= new UpdateAsync(_loggerFactory, _message, _messagePrompt);

                    _updateTask = _updateAsync.PrepareUpdateAsyncRun(
                        launchMode,
                        _settings,
                        _addOnInfo,
                        _updateCancellation.Token);

                    // Если nanoCAD завершится раньше подготовки, поздно завершившаяся задача
                    // только удалит временные файлы и не сможет начать установку.
                    _ = _updateTask.ContinueWith(
                        _ =>
                        {
                            lock (_updateGate)
                            {
                                if (_isTerminating)
                                {
                                    _updateAsync?.DiscardPrepared();
                                }
                            }
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Не удалось запустить фоновую подготовку обновления.", isFatal: false, notifyUser: false);
            }
        }


        private void StartManualUpdate()
        {
            lock (_updateGate)
            {
                if (_isTerminating)
                {
                    return;
                }

                if (_updateAsync?.PreparedUpdate is not null)
                {
                    _message.InfoMessage(
                        "Обновление уже подготовлено и будет установлено при закрытии nanoCAD.",
                        title: "Updater");
                    return;
                }

                if (_updateTask is { IsCompleted: false })
                {
                    _message.InfoMessage(
                        "Проверка обновлений уже выполняется. Повторный запуск не требуется.",
                        title: "Updater");
                    return;
                }

                StartUpdate(UpdateLaunchMode.Manual);
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

        /// <summary>Terminates this instance.</summary>
        public void Terminate()
        {
            lock (_updateGate)
            {
                _isTerminating = true;
                UpdateAsync.SetManualCheckHandler(null);
                _logger?.Trace($"{nameof(EntryPoint)}: Terminate");

                try
                {
                    if (_updateTask is null || _updateAsync is null)
                    {
                        return;
                    }

                    if (_updateTask.IsCompleted)
                    {
                        // Подготовка уже закончилась: установка выполняется синхронно,
                        // пока nanoCAD выгружает загруженные сборки.
                        if (_updateAsync.PreparedUpdate is not null)
                        {
                            UpdateResult result = _updateAsync.InstallPrepared();
                            PreparedUpdate? installed = _updateAsync.LastInstalledUpdate;

                            if (installed is not null)
                            {
                                string message = result == UpdateResult.Installed
                                    ? installed.ResultMessage ?? "Обновление успешно установлено. Перезапусти nanoCAD."
                                    : installed.ErrorMessage ?? "Не удалось установить обновление. Подробности — в журнале PlotSPDS.";

                                UpdateResultMarker.Write(
                                    _addOnInfo.AssemblyDirectory,
                                    result,
                                    installed.Request.CurrentVersion.ToString(),
                                    installed.Update.Version.ToVersion().ToString(),
                                    message,
                                    _logger);
                            }
                        }

                        return;
                    }

                    // Не ждём сеть или распаковку. Позднее завершение подготовки будет отменено
                    // либо очищено continuation выше.
                    try
                    {
                        _updateCancellation?.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex, "Не удалось отменить подготовку обновления при завершении nanoCAD.");
                    }
                }
                finally
                {
                    // Удаляем временные каталоги завершившихся сессий независимо от того,
                    // была ли установка, подготовка ещё выполнялась или обновление отсутствовало.
                    // Собственная активная/подготовленная сессия защищена session.lock и будет
                    // очищена после отмены либо установки; другие активные экземпляры не затрагиваются.
                    try
                    {
                        _updateAsync?.CleanupStaleTemporaryDirectories();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex, "Не удалось очистить временные каталоги Updater при завершении nanoCAD.");
                    }
                }
            }
        }
    }
}
