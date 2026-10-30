using dRz.Abstractions.Settings;

namespace dRz.Settings
{
    /// <summary>
    /// Реализация общих настроек аддона.
    /// </summary>
    public sealed class GeneralSettings : IGeneralSettings
    {
        /// <inheritdoc />
        public bool ShowCommandInfoOnStartup { get; set; } = true;
    }
}