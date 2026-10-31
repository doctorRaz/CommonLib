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
        /// Выполняет проверку и установку обновления согласно параметрам запроса. При ручном запуске
        /// принудительно использует режим <see cref="UpdateMode.CheckAndNotify"/>.
        /// Если локальное расположение основной сборки не подтверждено, обновление не выполняется.
        /// </summary>
        /// <param name="request">Параметры обновления, сформированные основным аддоном.</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат проверки и установки обновления.</returns>
        public async Task<UpdateResult> RunAsync(
            UpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            string tempDirectoryRoot = Path.Combine(Path.GetTempPath(), "dRz");

            string? tempDirectory = null;

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

                    return UpdateResult.NetworkPath;
                }

                _logger.Trace("RunAsync");

                // При ручном запуске всегда проверяем обновления с уведомлением,
                // независимо от режима, заданного вызывающим кодом.
                UpdateMode effectiveMode = request.LaunchMode == UpdateLaunchMode.Manual
                    ? UpdateMode.CheckAndNotify
                    : _updateSettings.Mode;

                tempDirectory = Path.Combine(tempDirectoryRoot, nameof(Updater), Guid.NewGuid().ToString("N"));

                Directory.CreateDirectory(tempDirectory);

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

                    return UpdateResult.UpToDate;
                }

                // Disabled запрещает необязательные обновления, но не mandatory.
                // Mandatory обновления выполняются независимо от режима.
                if (effectiveMode == UpdateMode.Disabled && !update.Mandatory)
                {
                    return UpdateResult.Disabled;
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
                        return UpdateResult.Declined;
                    }
                }

                // Автоматическая установка должна быть предварительно обозначена пользователю.
                // При CheckAndNotify запрос согласия уже является уведомлением.
                if (update.Mandatory)
                {
                    string message = $"Обнаружено обязательное обновление {update.Product} до версии {update.Version.ToVersion()}. Оно будет установлено автоматически.";

                    _logger.Trace(message);

                    _messageServices.InfoMessage(message, title: "Updater");
                }
                else if (effectiveMode == UpdateMode.CheckAndInstall)
                {
                    string message = $"Будет выполнено обновление {update.Product} до версии {update.Version.ToVersion()}";

                    _logger.Trace(message);

                    _messageServices.InfoMessage(message, title: "Updater");
                }

                string assetFile = Path.Combine(
                    tempDirectory,
                    "update" + Path.GetExtension(GetAssetName(update)));

                string assetUrl = ResolveUrl(
                    request.UpdateUrl,
                    GetAsset(update));

                await _downloader.DownloadAsync(
                    assetUrl,
                    assetFile,
                    cancellationToken);

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

                //консоль:
                // Перед распаковкой: поток вызывающего кода, ID=7, ThreadPool=True, Name=.NET ThreadPool Worker
                //нанокад:
                //  Перед распаковкой: поток вызывающего кода, ID=1, ThreadPool=False, Name=(без имени)

                // Диагностика потока, из которого запускается распаковка.
                Thread currentThread = Thread.CurrentThread;
                _logger.Trace(
                    $"Перед распаковкой: поток вызывающего кода, ID={currentThread.ManagedThreadId}, " +
                    $"ThreadPool={currentThread.IsThreadPoolThread}, Name={currentThread.Name ?? "(без имени)"}");

                // Выполняем распаковку в отдельном потоке и ожидаем её завершения.
                // Диагностику потока распаковки пишем в лог: UI-сообщения nanoCAD
                // не вызываем из фонового потока.
                await Task.Run(() =>
                {
                    Thread extractionThread = Thread.CurrentThread;
                    _logger.Trace(
                        $"Начало распаковки: ID потока={extractionThread.ManagedThreadId}, " +
                        $"ThreadPool={extractionThread.IsThreadPoolThread}, " +
                        $"Name={extractionThread.Name ?? "(без имени)"}");

                    SevenZipService sevenZip = new SevenZipService();

                    sevenZip.Extract(
                        assetFile,
                        extractedDirectory,
                        GetAssetPassword(update));
                }, cancellationToken);

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

                try
                {
                    if (!Installer.MoveDirectoryFilesWithBackup(
                        sourceDirectory,
                        request.PackageDirectory,
                        fullUpdate))
                    {
                        throw new IOException("Не удалось установить пакет обновления.");
                    }
                }
                catch (UpdateRollbackException ex)
                {
                    // Откат не завершён: логируем обе первичные ошибки и не повторяем установку.
                    _logger.Error(ex.InstallException);
                    _logger.Error(ex.RollbackException);

                    _messageServices.WarningMessage(
                        $"Не удалось установить обновление {update.Version.ToVersion()}, и восстановление завершилось ошибкой. " +
                        $"Приложение может находиться в невалидном состоянии. Скачайте полный релиз и распакуйте его с заменой файлов.", title: "Updater");

                    OpenReleaseIfRequested(request.UpdateUrl, update.Tag);
                    return UpdateResult.Failed;
                }
                catch (Exception installException)
                {
                    // Installer выбросил исходную ошибку только после успешного отката.
                    _logger.Error(installException);

                    _messageServices.WarningMessage(
                        $"Не удалось установить обновление {update.Version.ToVersion()}; прежнее состояние восстановлено. " +
                        $"Попробуйте повторить обновление после перезапуска nanoCAD или скачайте релиз вручную.", title: "Updater");

                    OpenReleaseIfRequested(request.UpdateUrl, update.Tag);
                    return UpdateResult.Failed;
                }

                string message2 = $"{update.Product} обновлён с версии {request.CurrentVersion} до версии {update.Version.ToVersion()}\nЧтобы изменения вступили в силу, необходима перезагрузка.";

                _logger.Trace(message2);

                _messageServices.InfoMessage(message2, title: "Updater");

                return UpdateResult.Installed;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Debug("Операция обновления отменена через CancellationToken.");
                return UpdateResult.Cancelled;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                _messageServices.WarningMessage(
                    $"Не удалось проверить или выполнить обновление: {ex.Message}", title: "Updater");

                return UpdateResult.Failed;
            }
            finally
            {
#if DEBUG
                tempDirectory = Path.Combine(tempDirectoryRoot, nameof(Updater));
#endif
                try

                {
                    if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }

                    _logger.Trace("Удалён временный каталог обновления.");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
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