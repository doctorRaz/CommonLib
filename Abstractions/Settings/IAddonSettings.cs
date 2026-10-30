namespace dRz.Abstractions.Settings
{
    /// <summary>
    /// Корневой контракт настроек аддона.
    /// </summary>
    public interface IAddonSettings
    {
        /// <summary>
        /// Общие настройки аддона.
        /// </summary>
        IGeneralSettings General { get; }

        /// <summary>
        /// Настройки проверки и установки обновлений.
        /// </summary>
        IUpdateSettings Updates { get; }

        /// <summary>
        /// Явно сохраняет текущие значения настроек.
        /// </summary>
        void Save();
    }
}