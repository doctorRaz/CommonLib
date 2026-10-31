using dRz.Abstractions.Updater;
using System;

namespace dRz.Abstractions.Settings
{
    /// <summary>
    /// Настройки и состояние обновления аддона.
    /// </summary>
    public interface IUpdateSettings
    {
        /// <summary>
        /// Режим проверки и установки необязательных обновлений.
        /// </summary>
        UpdateMode Mode { get; set; }

        /// <summary>
        /// Интервал между автоматическими проверками обновлений в днях. Допустимый диапазон — от 1 до 30.
        /// </summary>
        int UpdateCheckIntervalDays { get; set; }

        /// <summary>
        /// Время последней успешно завершённой проверки/операции обновления в UTC.
        /// Не обновляется при ошибке, отмене или блокировке обновления из-за сетевого расположения.
        /// </summary>
        DateTime? LastCheck { get; set; }

        /// <summary>
        /// Результат последней проверки или попытки обновления.
        /// </summary>
        UpdateResult? LastResult { get; set; }
    }
}