using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using dRz.Updater.Models;
using dRz.Updater.Services;
using dRz.Updater.Services.SevenZip;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dRz.Updater
{
    /// <summary>
    /// Менеджер обновлений.
    /// </summary>
    public class UpdateManager
    {
        private readonly IDrzLogger _logger;
        private readonly IDrzLogger _cleanerLogger;
        private readonly IMessageService _messageServices;
        private readonly IMessagePromptService _promptService;
        private readonly IUpdateSettings _updateSettings;
        private readonly Downloader _downloader;

        /// <summary>
        /// Создаёт менеджер обновлений.
        /// </summary>
        /// <param name="messageServices">Сервис сообщений.</param>
        /// <param name="promptService">Сервис интерактивных запросов.</param>
        /// <param name="loggerFactory">Фабрика логгеров.</param>
        /// <param name="updateSettings">Настройки обновления, принадлежащие основному аддону.</param>
        public UpdateManager(
            IMessageService messageServices,
            IMessagePromptService promptService,
            IDrzLoggerFactory loggerFactory,
            IUpdateSettings updateSettings)
        {
            _messageServices = messageServices;
            _promptService = promptService;
            _updateSettings = updateSettings ?? throw new ArgumentNullException(nameof(updateSettings));
            _downloader = new Downloader();
            _logger = loggerFactory.GetLogger<UpdateManager>();
            _cleanerLogger = loggerFactory.GetLogger<BackupCleaner>();
        }

        /// <summary>
        /// Очищает каталог аддона от резервных копий и пустых каталогов.
        /// </summary>
        /// <param name="addOnDirectory">Полный путь к каталогу аддона.</param>
        public void Cleanup(string addOnDirectory)
        {
            if (string.IsNullOrWhiteSpace(addOnDirectory))
            {
                ArgumentException exception = new ArgumentException("Не указан каталог аддона.", nameof(addOnDirectory));
                _logger.Error(exception);
                throw exception;
            }

            BackupCleaner.DeleteBackupFiles(addOnDirectory, _cleanerLogger);
        }

        /// <summary>
        /// Подготавливает обновление согласно параметрам запроса: проверяет manifest, загружает
        /// и проверяет архив, затем распаковывает его во временный каталог. Установленные файлы
        /// не изменяются; готовый пакет возвращается в <see cref="UpdatePreparation"/>.
        /// При ручном запуске принудительно использует режим <see cref="UpdateMode.CheckAndNotify"/>.
        /// Если локальное расположение основной сборки не подтверждено, подготовка не выполняется.
        /// Установка подготовленного пакета выполняется отдельно методом <see cref="InstallPrepared"/>.
        /// </summary>
        /// <param name="request">Параметры обновления, сформированные основным аддоном.</param>
        /// <param name="cancellationToken">Токен отмены подготовки.</param>
        /// <returns>Результат подготовки обновления и, если подготовка успешна, готовый пакет.</returns>
        public async Task<UpdatePreparation> PrepareAsync(
            UpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            string tempDirectoryRoot = Path.Combine(Path.GetTempPath(), "dRz");

            string? tempDirectory = null;
            FileStream? sessionLock = null;
            PreparedUpdate? preparedUpdate = null;

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                ValidateRequest(request);

                // Без достоверно подтверждённого локального расположения обновление запрещено.
                // Проверяем флаг до создания временных файлов и любых сетевых запросов.
                if (!request.IsNotNetworkPath)
                {
                    const string message = "Обновление отключено: расположение аддона сетевое или не удалось достоверно определить его тип. Проверьте путь или обратитесь к администратору.";

                    if (_updateSettings.Mode != UpdateMode.Disabled || request.LaunchMode == UpdateLaunchMode.Manual)
                    {
                        _messageServices.WarningMessage(message, title: "Updater");
                    }

                    _logger.Warn(message);

                    return UpdatePreparation.Completed(UpdateResult.NetworkPath);
                }

                _logger.Trace("RunAsync");

                // При ручном запуске всегда проверяем обновления с уведомлением,
                // независимо от режима, заданного вызывающим кодом.
                UpdateMode effectiveMode = request.LaunchMode == UpdateLaunchMode.Manual
                    ? UpdateMode.CheckAndNotify
                    : _updateSettings.Mode;

                string sessionsDirectory = Path.Combine(tempDirectoryRoot, nameof(Updater));

                // Удаляем только каталоги завершившихся сессий. Активные экземпляры
                // nanoCAD удерживают эксклюзивную блокировку своего session.lock.
                CleanupStaleTemporaryDirectories(sessionsDirectory);

                tempDirectory = Path.Combine(sessionsDirectory, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);

                // Блокировка удерживается на всём протяжении подготовки и, если пакет готов,
                // до синхронной установки или явного отказа от неё.
                string sessionLockPath = Path.Combine(tempDirectory, "session.lock");
                sessionLock = new FileStream(
                    sessionLockPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None);

                // update.json нужен даже в Disabled, чтобы mandatory update оставался обнаруживаемым.
                string updateJsonPath = Path.Combine(tempDirectory, "update.json");
                string updateJsonUrl = ResolveUrl(request.UpdateUrl, "update.json");

                await _downloader.DownloadAsync(
                    updateJsonUrl,
                    updateJsonPath,
                    cancellationToken);

                UpdateInfo update = UpdateJsonReader.Read(updateJsonPath);

                UpdateKind? updateKind = UpdateVersionChecker.Check(
                    request.CurrentVersion,
                    update);

                if (updateKind is null)
                {
                    string message = $"Для {update.Product} обновлений нет. Установлена актуальная версия {request.CurrentVersion}.";

                    if (request.LaunchMode == UpdateLaunchMode.Manual)
                    {
                        if (request.CurrentVersion != request.RunningVersion)
                        {
                            message += $" Чтобы изменения вступили в силу, необходима перезагрузка.";
                        }

                        _messageServices.InfoMessage(message, title: "Updater");
                    }

                    _logger.Trace(message);

                    return UpdatePreparation.Completed(UpdateResult.UpToDate);
                }

                // Disabled запрещает необязательные обновления, но не mandatory.
                // Mandatory обновления выполняются независимо от режима.
                if (effectiveMode == UpdateMode.Disabled && !update.Mandatory)
                {
                    return UpdatePreparation.Completed(UpdateResult.Disabled);
                }

                bool installAutomatically =
                    update.Mandatory ||
                    effectiveMode == UpdateMode.CheckAndInstall;

                if (!installAutomatically)
                {
                    MessageResult result = _promptService.AskYesNo(
                        $"Доступно обновление {update.Product} до версии {update.Version.ToVersion()}.\nУстановить его?", "Updater");

                    if (result != MessageResult.Yes)
                    {
                        return UpdatePreparation.Completed(UpdateResult.Declined);
                    }
                    else
                    {
                        _messageServices.InfoMessage($"Обновление {update.Product} до версии {update.Version.ToVersion()} будет установлено после перезапуска CAD.");
                    }
                }

                // Автоматическая установка должна быть предварительно обозначена пользователю.
                // При CheckAndNotify запрос согласия уже является уведомлением.
                if (update.Mandatory)
                {
                    string message = $"Обнаружено обязательное обновление {update.Product} до версии {update.Version.ToVersion()}. Оно будет установлено автоматически после перезапуска CAD.";

                    _logger.Trace(message);

                    _messageServices.InfoMessage(message, title: "Updater");
                }
                else if (effectiveMode == UpdateMode.CheckAndInstall)
                {
                    string message = $"После перезапуска CAD будет выполнено обновление {update.Product} до версии {update.Version.ToVersion()}";

                    _logger.Trace(message);

                    _messageServices.InfoMessage(message, title: "Updater");
                }

                string assetFile = Path.Combine(
                    tempDirectory,
                    "update" + Path.GetExtension(GetAssetName(update)));

                string assetUrl = ResolveUrl(
                    request.UpdateUrl,
                    GetAsset(update));

                // Искусственное замедление включается только в DEBUG и только для архива обновления.
                // Загрузка update.json выше остаётся быстрой, поэтому тестовая задержка
                // не мешает обнаружить релиз и дойти до скачивания самого пакета.
                int downloadDelayMs = UpdaterTestSettings.DownloadDelayBetweenChunksMs;
                if (downloadDelayMs > 0)
                {
                    _logger.Warn(
                        $"DEBUG-ТЕСТ: включена задержка скачивания {downloadDelayMs} мс после каждого блока.");
                }

                await _downloader.DownloadAsync(
                    assetUrl,
                    assetFile,
                    cancellationToken,
                    downloadDelayMs);

                cancellationToken.ThrowIfCancellationRequested();

                if (!FileVerifier.Verify(
                    assetFile,
                    GetAssetSize(update),
                    GetAssetSha256(update)))
                {
                    throw new InvalidDataException(
                        "Проверка размера или SHA-256 пакета обновления не пройдена.");
                }

                MarkOfTheWebRemover.Remove(assetFile);

                string extractedDirectory = Path.Combine(
                    tempDirectory,
                    "extracted");

                // Диагностика потока, из которого запускается распаковка.
                Thread currentThread = Thread.CurrentThread;
                _logger.Trace(
                    $"Перед распаковкой: поток вызывающего кода, ID={currentThread.ManagedThreadId}, " +
                    $"ThreadPool={currentThread.IsThreadPoolThread}, Name={currentThread.Name ?? "(без имени)"}");

                // Выполняем распаковку в отдельном потоке и ожидаем её завершения.
                // Диагностику потока распаковки пишем в лог: UI-сообщения nanoCAD
                // не вызываем из фонового потока.
                // Проверяем отмену до постановки распаковки в ThreadPool.
                cancellationToken.ThrowIfCancellationRequested();

                // ВАЖНО: не передаём cancellationToken в Task.Run.
                // SharpSevenZip выполняет синхронную распаковку и не умеет её прерывать.
                // Если передать токен в Task.Run, отмена может перевести ожидающую задачу
                // в Canceled, пока уже запущенный делегат ещё работает с архивом и файлами.
                // Тогда внешний finally сможет попытаться удалить временный каталог
                // до фактического завершения распаковки.
                //
                // Поэтому ожидаем именно завершения делегата без токена, а отмену
                // проверяем после await. Terminate не ожидает эту задачу: он только
                // запрашивает отмену и возвращает управление. Если процесс nanoCAD
                // продолжает работать, фоновая подготовка дождётся окончания распаковки,
                // обнаружит отмену и удалит каталог в своём finally. При завершении процесса
                // сама фоновая задача прекращается вместе с процессом; оставшийся каталог
                // будет удалён при следующем проходе очистки после освобождения session.lock.
                await Task.Run(() =>
                {
                    // Дополнительная проверка внутри делегата закрывает гонку:
                    // токен мог быть отменён после проверки выше, но до старта работы.
                    cancellationToken.ThrowIfCancellationRequested();

                    Thread extractionThread = Thread.CurrentThread;
                    _logger.Trace(
                        $"Начало распаковки: ID потока={extractionThread.ManagedThreadId}, " +
                        $"ThreadPool={extractionThread.IsThreadPoolThread}, " +
                        $"Name={extractionThread.Name ?? "(без имени)"}");

                    // SharpSevenZip выполняет распаковку одним синхронным вызовом
                    // и не предоставляет используемому нами CancellationToken механизм отмены.
                    // DEBUG-задержка намеренно выполняется внутри фоновой задачи без токена.
                    // Если Terminate вызван, но процесс nanoCAD продолжает работать, после
                    // паузы задача завершит распаковку, обнаружит отмену и удалит результат.
                    // При полном завершении процесса задача прекращается вместе с ним,
                    // а оставшийся временный каталог будет очищен при следующем запуске.
                    int extractionDelayMs = UpdaterTestSettings.ExtractionDelayBeforeStartMs;
                    if (extractionDelayMs > 0)
                    {
                        _logger.Warn(
                            $"DEBUG-ТЕСТ: распаковка задержана на {extractionDelayMs} мс. " +
                            "Можно закрыть nanoCAD и проверить, что Terminate не ожидает распаковку.");

                        Thread.Sleep(extractionDelayMs);

                        _logger.Trace(
                            "DEBUG-ТЕСТ: задержка распаковки завершена; начинается SharpSevenZip Extract.");
                    }

                    SevenZipService sevenZip = new SevenZipService();

                    sevenZip.Extract(
                        assetFile,
                        extractedDirectory,
                        GetAssetPassword(update));
                });

                // Отмена во время распаковки обрабатывается только после её завершения.
                // К этому моменту SharpSevenZip больше не удерживает временные файлы,
                // поэтому общий finally сможет безопасно удалить весь каталог сессии.
                cancellationToken.ThrowIfCancellationRequested();

                string sourceDirectory = Path.Combine(extractedDirectory, update.Product);

                if (!request.IsPackage)
                {
                    string moduleDirectory = Path.GetFileName(
                        Path.TrimEndingDirectorySeparator(request.PackageDirectory));
                    sourceDirectory = Path.Combine(sourceDirectory, moduleDirectory);
                }

                // Проверяем структуру архива до очистки и изменения установленного аддона.
                if (!Directory.Exists(sourceDirectory))
                {
                    throw new InvalidDataException(
                        $"В архиве не найден каталог обновления: {sourceDirectory}");
                }

                bool fullUpdate = updateKind == UpdateKind.Full;

                // Отмена допускается до начала изменения установленных файлов.
                // Во время синхронной установки не прерываем операции, чтобы не нарушить откат.
                cancellationToken.ThrowIfCancellationRequested();

                // Подготовка завершена. Рабочие файлы аддона пока не изменялись.
                PreparedUpdate candidate = new PreparedUpdate(
                    request,
                    tempDirectory,
                    sourceDirectory,
                    update,
                    fullUpdate,
                    sessionLock);

                // Передаём владение блокировкой объекту подготовленного обновления.
                sessionLock = null;

                try
                {
                    _logger.Trace(
                        $"Обновление {update.Product} до версии {update.Version.ToVersion()} подготовлено к установке.");
                }
                catch
                {
                    // Диагностика не должна отменять корректно подготовленный пакет.
                }

                preparedUpdate = candidate;
                return UpdatePreparation.Ready(preparedUpdate);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Debug("Операция обновления отменена через CancellationToken.");
                return UpdatePreparation.Completed(UpdateResult.Cancelled);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                _messageServices.WarningMessage(
                    $"Не удалось проверить или выполнить обновление: {ex.Message}", title: "Updater");

                return UpdatePreparation.Completed(UpdateResult.Failed);
            }
            finally
            {
                // Подготовленный пакет остаётся во временном каталоге до установки или отмены.
                if (preparedUpdate is null)
                {
                    try
                    {
                        sessionLock?.Dispose();
                    }
                    finally
                    {
                        CleanupTemporaryDirectory(tempDirectory);
                    }
                }
            }
        }

        /// <summary>
        /// Совместимый сценарий полной проверки и установки.
        /// </summary>
        public async Task<UpdateResult> RunAsync(
            UpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            UpdatePreparation preparation = await PrepareAsync(request, cancellationToken);
            PreparedUpdate? preparedUpdate = preparation.PreparedUpdate;

            if (preparedUpdate is not null)
            {
                return InstallPrepared(preparedUpdate);
            }

            return preparation.Result ??
                throw new InvalidOperationException("Результат подготовки не содержит данных.");
        }

        /// <summary>
        /// Синхронно устанавливает ранее подготовленный пакет.
        /// Метод не выполняет сетевых операций и не показывает диалогов пользователю.
        /// </summary>
        public UpdateResult InstallPrepared(PreparedUpdate prepared)
        {
            ArgumentNullException.ThrowIfNull(prepared);

            try
            {
                if (!Installer.MoveDirectoryFilesWithBackup(
                    prepared.SourceDirectory,
                    prepared.Request.PackageDirectory,
                    prepared.FullUpdate))
                {
                    throw new IOException("Не удалось установить подготовленный пакет обновления.");
                }

                prepared.ResultMessage =
                    $"{prepared.Update.Product} обновлён с версии {prepared.Request.CurrentVersion} " +
                    $"до версии {prepared.Update.Version.ToVersion()}.";

                try
                {
                    _logger.Trace(prepared.ResultMessage);
                }
                catch
                {
                    // Сбой логирования не меняет результат успешно завершённой установки.
                }

                return UpdateResult.Installed;
            }
            catch (UpdateRollbackException ex)
            {
                prepared.ErrorMessage =
                    "Установка и откат завершились ошибками. Приложение может находиться в невалидном состоянии.";
                try
                {
                    _logger.Error(ex.InstallException);
                    _logger.Error(ex.RollbackException);
                }
                catch
                {
                    // Сохраняем результат установки даже при недоступности логгера.
                }

                return UpdateResult.Failed;
            }
            catch (Exception ex)
            {
                prepared.ErrorMessage =
                    $"Не удалось установить обновление; прежнее состояние восстановлено. {ex.Message}";
                try
                {
                    _logger.Error(ex);
                }
                catch
                {
                    // Сохраняем результат установки даже при недоступности логгера.
                }

                return UpdateResult.Failed;
            }
            finally
            {
                // Сначала освобождаем блокировку, затем удаляем каталог сессии.
                try
                {
                    prepared.ReleaseSessionLock();
                }
                finally
                {
                    CleanupTemporaryDirectory(prepared.TempDirectory);
                }
            }
        }

        /// <summary>Удаляет временные файлы подготовленного, но не устанавливаемого обновления.</summary>
        public void DiscardPrepared(PreparedUpdate prepared)
        {
            ArgumentNullException.ThrowIfNull(prepared);
            try
            {
                prepared.ReleaseSessionLock();
            }
            finally
            {
                CleanupTemporaryDirectory(prepared.TempDirectory);
            }
        }

        /// <summary>
        /// Очищает временные каталоги завершившихся сессий Updater.
        /// Каталоги активных сессий с занятой блокировкой не затрагиваются.
        /// </summary>
        public void CleanupStaleTemporaryDirectories()
        {
            string sessionsDirectory = Path.Combine(
                Path.GetTempPath(),
                "dRz",
                nameof(Updater));

            CleanupStaleTemporaryDirectories(sessionsDirectory);
        }

        /// <summary>
        /// Удаляет оставшиеся каталоги завершившихся сессий.
        /// Каталоги без файла блокировки и каталоги с занятой блокировкой пропускаются.
        /// </summary>
        private void CleanupStaleTemporaryDirectories(string sessionsDirectory)
        {
            try
            {
                if (!Directory.Exists(sessionsDirectory))
                {
                    return;
                }

                foreach (string directory in Directory.EnumerateDirectories(sessionsDirectory))
                {
                    string lockPath = Path.Combine(directory, "session.lock");

                    // Отсутствие lock-файла трактуем как неопределённое состояние:
                    // не удаляем каталог, чтобы не вмешаться в создание другой сессии.
                    if (!File.Exists(lockPath))
                    {
                        continue;
                    }

                    try
                    {
                        // Успешное открытие означает, что другой процесс не удерживает lock.
                        // Дескриптор закрывается до удаления каталога.
                        using (new FileStream(
                            lockPath,
                            FileMode.Open,
                            FileAccess.ReadWrite,
                            FileShare.None))
                        {
                        }

                        Directory.Delete(directory, recursive: true);
                        _logger.Trace($"Удалён временный каталог завершившейся сессии: {directory}");
                    }
                    catch (IOException)
                    {
                        // Каталог принадлежит активной сессии либо временно недоступен.
                        _logger.Trace($"Временный каталог занят; очистка пропущена: {directory}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.Warn($"Нет доступа для очистки временного каталога {directory}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Очистка старых временных данных не должна блокировать проверку обновлений.
                try
                {
                    _logger.Warn($"Не удалось выполнить очистку временных каталогов Updater: {ex.Message}");
                }
                catch
                {
                    // Ошибка диагностики не должна влиять на запуск обновления.
                }
            }
        }

        private void CleanupTemporaryDirectory(string? directory)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                    _logger.Trace($"Удалён временный каталог обновления: {directory}");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.Error(ex, $"Не удалось удалить временный каталог обновления: {directory}");
                }
                catch
                {
                    // Ошибка логирования не должна влиять на результат установки.
                }
            }
        }

        /// <summary>
        /// Предлагает открыть страницу опубликованного релиза и открывает её при подтверждении.
        /// </summary>
        private void OpenReleaseIfRequested(string updateUrl, string tag)
        {
            string releaseUrl = BuildReleaseUrl(updateUrl, tag);
            MessageResult result = _promptService.AskYesNo(
                $"Открыть страницу релиза?\n{releaseUrl}", "Updater");

            if (result != MessageResult.Yes)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(releaseUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _messageServices.WarningMessage(
                    $"Не удалось открыть страницу релиза автоматически. Откройте ссылку вручную: {releaseUrl}", title: "Updater");
            }
        }

        /// <summary>
        /// Преобразует URL каталога загрузки в URL страницы конкретного релиза.
        /// </summary>
        private static string BuildReleaseUrl(string updateUrl, string tag)
        {
            if (Uri.TryCreate(updateUrl, UriKind.Absolute, out Uri? uri))
            {
                const string downloadSuffix = "/releases/latest/download/";
                string path = uri.AbsolutePath;

                if (path.EndsWith(downloadSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string repositoryPath = path.Substring(0, path.Length - downloadSuffix.Length);
                    return new UriBuilder(uri)
                    {
                        Path = repositoryPath + "/releases/tag/" + Uri.EscapeDataString(tag),
                        Query = string.Empty,
                        Fragment = string.Empty
                    }.Uri.ToString();
                }
            }

            // Если формат URL нестандартный, хотя бы предлагаем исходный URL источника обновлений.
            return updateUrl;
        }

        /// <summary>
        /// Возвращает описание защищённого пакета, если оно задано.
        /// </summary>
        private static ProtectedPackageInfo? GetProtected(UpdateInfo update)
        {
            return update.Protected;
        }

        private static string GetAsset(UpdateInfo update)
        {
            return GetProtected(update)?.Asset ?? update.Asset;
        }

        private static string GetAssetName(UpdateInfo update)
        {
            return GetAsset(update);
        }

        private static long GetAssetSize(UpdateInfo update)
        {
            return GetProtected(update)?.Size ?? update.Size;
        }

        private static string GetAssetSha256(UpdateInfo update)
        {
            return GetProtected(update)?.Sha256 ?? update.Sha256;
        }

        private static string? GetAssetPassword(UpdateInfo update)
        {
            return GetProtected(update)?.Password;
        }

        /// <summary>
        /// Формирует URL ресурса относительно базового URL обновлений.
        /// </summary>
        private static string ResolveUrl(string baseUrl, string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new InvalidDataException("В update.json не указан ресурс.");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri))
            {
                throw new InvalidDataException("Некорректный базовый URL обновлений.");
            }

            if (Uri.TryCreate(resource, UriKind.Absolute, out Uri? absoluteUri))
            {
                return absoluteUri.ToString();
            }

            string normalizedBaseUrl = baseUri.AbsoluteUri.EndsWith(
                "/",
                StringComparison.Ordinal)
                ? baseUri.AbsoluteUri
                : baseUri.AbsoluteUri + "/";

            return new Uri(new Uri(normalizedBaseUrl), resource).ToString();
        }

        /// <summary>
        /// Проверяет обязательные параметры запроса.
        /// </summary>
        private static void ValidateRequest(UpdateRequest request)
        {
            if (request.CurrentVersion is null)
            {
                throw new ArgumentException("Не указана текущая версия.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.UpdateUrl))
            {
                throw new ArgumentException("Не указан базовый URL обновлений.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.PackageDirectory))
            {
                throw new ArgumentException("Не указан каталог аддона.", nameof(request));
            }
        }
    }
}