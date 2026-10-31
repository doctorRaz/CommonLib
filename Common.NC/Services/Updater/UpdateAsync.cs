using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using dRz.Updater;
using dRz.Updater.Services;
using System;
using System.Threading.Tasks;

namespace dRz.Common.NC.Services.Updater
{
    /// <summary>
    /// Координирует проверку и установку обновлений аддона из контекста CAD-хоста.
    /// </summary>
    /// <remarks>
    /// Класс использует зарегистрированные сервисы аддона. Асинхронный запуск не блокирует
    /// поток вызова; после асинхронных операций продолжение использует захваченный контекст,
    /// если он предоставлен вызывающей средой.
    /// </remarks>
    public class UpdateAsync
    {
        private readonly IDrzLogger _logger;// сервис логирования
        private readonly IMessageService _msg;// сервис для отображения сообщений пользователю
        private readonly IMessagePromptService _msgprt;// сервис для отображения сообщений с запросом подтверждения
        private readonly IDrzLoggerFactory _loggerFactory;// фабрика для создания экземпляров логгера

        /// <summary>Initializes a new instance of the <see cref="UpdateAsync"/> class.</summary>
        /// <param name="drzLoggerFactory">The DRZ logger factory.</param>
        /// <param name="messageService">The message service.</param>
        /// <param name="messagePromptService">The message prompt service.</param>
        public UpdateAsync(IDrzLoggerFactory drzLoggerFactory,
                            IMessageService messageService,
                            IMessagePromptService messagePromptService)
        {
            _loggerFactory = drzLoggerFactory;
            _logger = _loggerFactory.GetLogger(typeof(UpdateAsync));
            _msg = messageService;
            _msgprt = messagePromptService;
        }

        /// <summary>
        /// Выполняет проверку обновлений и, в зависимости от режима запуска и настроек,
        /// передаёт управление установкой обновления менеджеру обновлений.
        /// Сохраняет результат запуска и время завершённой проверки.
        /// </summary>
        /// <param name="launchMode">Режим запуска, определяющий поведение проверки и установки.</param>
        /// <param name="settings">Настройки</param>
        /// <param name="addOnInfo">Информация о аддоне</param>
        /// <returns>Задача, представляющая асинхронную операцию обновления.</returns>
        /// <remarks>
        /// Ошибки выполнения, сохранения состояния и уведомления об ошибках обрабатываются
        /// отдельно, чтобы сбой диагностики не маскировал исходную ошибку.
        /// </remarks>
        public async Task UpdateAsyncRun(UpdateLaunchMode launchMode, IAddonSettings settings, IAddOnInfo addOnInfo)
        {
            try
            {
                IUpdateSettings updateSettings = settings.Updates;// Получаем настройки обновлений аддона

                UpdateManager updateManager = new UpdateManager(
                    _msg,
                    _msgprt,
                    _loggerFactory,
                    updateSettings);

                // Очистка выполняется только при автоматическом запуске и до проверки интервала.
                // Файловые операции не блокируют синхронную инициализацию nanoCAD.
                // Ошибка очистки не должна прерывать проверку обновлений.
                if (launchMode == UpdateLaunchMode.Automatic)
                {
                    try
                    {
                        await Task.Run(() => updateManager.Cleanup(addOnInfo.PackageDirectory));
                    }
                    catch (Exception cleanupException)
                    {
                        try
                        {
                            _logger.Error(cleanupException, "Не удалось очистить резервные файлы перед автоматической проверкой обновлений.");
                        }
                        catch
                        {
                            // Ошибка логирования не должна препятствовать проверке обновлений.
                        }
                    }
                }

                int minDay = 1;

#if DEBUG
                minDay = 0;//интервал для отладки
#endif

                // Ограничиваем интервал автоматических проверок допустимым диапазоном.
                int configuredInterval = updateSettings.UpdateCheckIntervalDays;
                int normalizedInterval = Math.Clamp(configuredInterval, minDay, 30);
                bool intervalNormalized = configuredInterval != normalizedInterval;

                if (intervalNormalized)
                {
                    updateSettings.UpdateCheckIntervalDays = normalizedInterval;
                    try
                    {
                        // Сохраняем нормализованное значение даже если проверка будет пропущена.
                        settings.Save();
                    }
                    catch (Exception saveException)
                    {
                        ReportError(saveException, "Не удалось сохранить интервал проверки обновлений.");
                    }
                }

                // Автоматическая проверка выполняется не чаще заданного интервала.
                // Ручной запуск всегда игнорирует ограничение.
                if (launchMode == UpdateLaunchMode.Automatic &&
                    updateSettings.LastCheck.HasValue &&
                    DateTime.UtcNow - updateSettings.LastCheck.Value < TimeSpan.FromDays(normalizedInterval))
                {
                    _logger.Trace(
                        $"Автоматическая проверка пропущена: с последней успешной проверки " +
                        $"не прошло {normalizedInterval} дн.");
                    return;
                }

                string updateUrl = addOnInfo.RepositoryUrl +
                                   "/releases/latest/download/";
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
                    LaunchMode = launchMode,
                };

                UpdateResult result = await updateManager.RunAsync(request);

                // Обновляем LastCheck только если проверка/операция завершилась успешно.
                // Не обновляем LastCheck при ошибке или отмене.
                if (result is not (UpdateResult.Failed or UpdateResult.Cancelled))
                {
                    updateSettings.LastCheck = DateTime.UtcNow;
                }

                // Сохраняем результат фактически выполненного запуска.
                updateSettings.LastResult = result;

                try
                {
                    settings.Save();
                }
                catch (Exception saveException)
                {
                    ReportError(saveException, "Не удалось сохранить состояние обновления.");
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "Не удалось выполнить проверку или обновление.");
            }
        }

        /// <summary>
        /// Пытается записать ошибку в журнал и уведомить пользователя.
        /// Сбой одного из каналов диагностики не должен скрывать исходную ошибку.
        /// </summary>
        private void ReportError(Exception exception, string message)
        {
            try
            {
                _logger.Error(exception, message);
            }
            catch
            {
                // Не допускаем, чтобы сбой логгера прервал обработку исходной ошибки.
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