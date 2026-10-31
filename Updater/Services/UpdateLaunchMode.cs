using dRz.Abstractions.Settings;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Определяет способ запуска проверки обновлений.
    /// </summary>
    public enum UpdateLaunchMode
    {
        /// <summary>
        /// Автоматический запуск. Используется режим установки из настроек аддона; уведомления об отсутствии обновлений и ошибках не добавляются.
        /// </summary>
        Automatic,

        /// <summary>
        /// Ручной запуск. Принудительно использует <see cref="UpdateMode.CheckAndNotify"/> для необязательных обновлений и уведомляет об отсутствии обновлений и ошибках.
        /// </summary>
        Manual
    }
}