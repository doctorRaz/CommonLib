using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using System;

namespace dRz.Settings
{
    /// <summary>
    /// Реализация настроек обновления.
    /// </summary>
    public sealed class UpdateSettings : IUpdateSettings
    {
        /// <inheritdoc />
        public UpdateMode Mode { get; set; } = UpdateMode.CheckAndInstall;

        /// <inheritdoc />
        public int UpdateCheckIntervalDays { get; set; } = 7;

        /// <inheritdoc />
        public DateTime? LastCheck { get; set; }

        /// <inheritdoc />
        public UpdateResult? LastResult { get; set; }
    }
}