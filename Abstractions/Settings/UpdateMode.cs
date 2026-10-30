using System.ComponentModel;

namespace dRz.Abstractions.Settings
{
    /// <summary>
    /// Режим проверки и установки необязательных обновлений.
    /// Обязательные обновления имеют приоритет.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// Не устанавливать необязательные обновления; обязательные остаются доступными.
        /// </summary>
        [Description("Отключено")]
        Disabled,

        /// <summary>
        /// Проверить наличие обновления и запросить согласие перед установкой.
        /// </summary>
        [Description("Уведомлять")]
        CheckAndNotify,

        /// <summary>
        /// Проверить наличие обновления и установить его без запроса согласия.
        /// </summary>
        [Description("Автоматически")]
        CheckAndInstall
    }
}