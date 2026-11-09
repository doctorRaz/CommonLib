using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using dRz.Updater;
using dRz.Updater.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace dRz.Common.NC.Services.Updater
{
    /// <summary>
    /// Координирует подготовку и синхронную установку обновлений аддона.
    /// </summary>
    public class UpdateAsync
    {
        private static Action? _manualCheckRequested;

        /// <summary>Регистрирует обработчик ручной проверки обновлений.</summary>
        public static void SetManualCheckHandler(Action? handler)
        {
            Interlocked.Exchange(ref _manualCheckRequested, handler);
        }

        /// <summary>Запрашивает ручную проверку через зарегистрированный координатор.</summary>
        public static void RequestManualCheck()
        {
            Volatile.Read(ref _manualCheckRequested)?.Invoke();
        }

        private readonly IDrzLogger _logger;
        private readonly IMessageService _msg;
        private readonly IMessagePromptService _msgprt;
        private readonly IDrzLoggerFactory _loggerFactory;

        private UpdateManager? _updateManager;
        private IAddonSettings? _settings;
        private IUpdateSettings? _updateSettings;

        /// <summary>Пакет, подготовленный для синхронной установки в Terminate.</summary>
        public PreparedUpdate? PreparedUpdate { get; private set; }

        /// <summary>Последний пакет, для которого была выполнена попытка установки.</summary>
        public PreparedUpdate? LastInstalledUpdate { get; private set; }

        /// <summary>Результат последней синхронной установки.</summary>
        public UpdateResult? LastInstallResult { get; private set; }

        /// <summary>Создаёт координатор обновления.</summary>
        public UpdateAsync(
            IDrzLoggerFactory drzLoggerFactory,
            IMessageService messageService,
            IMessagePromptService messagePromptService)
        {
            _loggerFactory = drzLoggerFactory;
            _logger = _loggerFactory.GetLogger(typeof(UpdateAsync));
            _msg = messageService;
            _msgprt = messagePromptService;
        }

        /// <summary>
        /// Асинхронно проверяет, загружает и распаковывает обновление.
        /// Установленные файлы не изменяются.
        /// </summary>
        public async Task PrepareUpdateAsyncRun(
            UpdateLaunchMode launchMode,
            IAddonSettings settings,
            IAddOnInfo addOnInfo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _updateSettings = settings.Updates;

                _updateManager = new UpdateManager(
                    _msg,
                    _msgprt,
                    _loggerFactory,
                    _updateSettings);

                if (launchMode == UpdateLaunchMode.Automatic)
                {
                    try
                    {
                        await Task.Run(
                            () => _updateManager.Cleanup(addOnInfo.PackageDirectory),
                            cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        SaveResult(UpdateResult.Cancelled);
                        return;
                    }
                    catch (Exception cleanupException)
                    {
                        try
                        {
                            _logger.Error(cleanupException,
                                "Не удалось очистить резервные файлы перед автоматической проверкой обновлений.");
                        }
                        catch
                        {
                            // Сбой очистки и логирования не должен прерывать проверку.
                        }
                    }
                }

                int minDay = 1;
#if DEBUG
                minDay = 0;
#endif
                int configuredInterval = _updateSettings.UpdateCheckIntervalDays;
                int normalizedInterval = Math.Clamp(configuredInterval, minDay, 30);

                if (configuredInterval != normalizedInterval)
                {
                    _updateSettings.UpdateCheckIntervalDays = normalizedInterval;
                    try
                    {
                        settings.Save();
                    }
                    catch (Exception saveException)
                    {
                        ReportError(saveException, "Не удалось сохранить интервал проверки обновлений.");
                    }
                }

                if (launchMode == UpdateLaunchMode.Automatic &&
                    _updateSettings.LastCheck.HasValue &&
                    DateTime.UtcNow - _updateSettings.LastCheck.Value < TimeSpan.FromDays(normalizedInterval))
                {
                    _logger.Trace(
                        $"Автоматическая проверка пропущена: с последней успешной проверки " +
                        $"не прошло {normalizedInterval} дн.");
                    return;
                }

                string updateUrl = addOnInfo.RepositoryUrl + "/releases/latest/download/";

#if DEBUG
                updateUrl = $"http://127.0.0.1:8080/{addOnInfo.Product}/{addOnInfo.HostFamily}";
#endif

                _logger.Trace($"updateUrl: {updateUrl}");

                UpdateRequest request = new UpdateRequest
                {
                    CurrentVersion = addOnInfo.InstalledVersion,
                    RunningVersion = addOnInfo.RunningVersion,
                    UpdateUrl = updateUrl,
                    PackageDirectory = addOnInfo.PackageDirectory,
                    IsPackage = addOnInfo.IsProductDirectory,
                    IsNotNetworkPath = addOnInfo.IsNotNetworkPath,
                    LaunchMode = launchMode
                };

                UpdatePreparation preparation = await _updateManager.PrepareAsync(request, cancellationToken);
                PreparedUpdate = preparation.PreparedUpdate;

                if (PreparedUpdate is null)
                {
                    SaveResult(preparation.Result ??
                        throw new InvalidOperationException("Результат подготовки не содержит данных."));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                SaveResult(UpdateResult.Cancelled);
            }
            catch (Exception ex)
            {
                ReportError(ex, "Не удалось выполнить подготовку обновления.");
                SaveResult(UpdateResult.Failed);
            }
        }

        /// <summary>
        /// Синхронно устанавливает подготовленный пакет и сохраняет результат.
        /// Не выполняет сетевых операций и не показывает пользовательских сообщений.
        /// </summary>
        public UpdateResult InstallPrepared()
        {
            if (PreparedUpdate is null || _updateManager is null)
            {
                throw new InvalidOperationException("Нет подготовленного пакета обновления.");
            }

            PreparedUpdate prepared = PreparedUpdate;
            PreparedUpdate = null;
            LastInstalledUpdate = prepared;

            UpdateResult result = _updateManager.InstallPrepared(prepared);
            LastInstallResult = result;
            SaveResult(result);
            return result;
        }

        /// <summary>Удаляет временные файлы пакета, если завершение хоста отменило установку.</summary>
        public void DiscardPrepared()
        {
            if (PreparedUpdate is null || _updateManager is null)
            {
                return;
            }

            PreparedUpdate prepared = PreparedUpdate;
            PreparedUpdate = null;
            _updateManager.DiscardPrepared(prepared);
        }

        /// <summary>
        /// Очищает временные каталоги завершившихся сессий Updater.
        /// Вызывается при завершении CAD-хоста независимо от результата установки.
        /// </summary>
        public void CleanupStaleTemporaryDirectories()
        {
            _updateManager?.CleanupStaleTemporaryDirectories();
        }

        /// <summary>
        /// Совместимый метод запуска проверки и подготовки обновления.
        /// Установка выполняется только вызовом <see cref="InstallPrepared"/>.
        /// </summary>
        public async Task UpdateAsyncRun(
            UpdateLaunchMode launchMode,
            IAddonSettings settings,
            IAddOnInfo addOnInfo)
        {
            await PrepareUpdateAsyncRun(launchMode, settings, addOnInfo);
        }

        private void SaveResult(UpdateResult result)
        {
            if (_settings is null || _updateSettings is null)
            {
                return;
            }

            if (result is not (UpdateResult.Failed or UpdateResult.Cancelled))
            {
                _updateSettings.LastCheck = DateTime.UtcNow;
            }

            _updateSettings.LastResult = result;

            try
            {
                _settings.Save();
            }
            catch (Exception saveException)
            {
                ReportError(saveException, "Не удалось сохранить состояние обновления.");
            }
        }

        private void ReportError(Exception exception, string message)
        {
            try
            {
                _logger.Error(exception, message);
            }
            catch
            {
                // Сбой логирования не должен скрывать исходную ошибку.
            }

            try
            {
                _msg.ErrorMessage(message, exception);
            }
            catch (Exception notificationException)
            {
                try
                {
                    _logger.Error(notificationException, "Не удалось показать сообщение об ошибке.");
                }
                catch
                {
                    // Оба канала диагностики могут быть недоступны.
                }
            }
        }
    }
}