using dRz.Updater.Models;
using System;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Определяет тип обновления с учётом версии и метаданных релиза.
    /// </summary>
    public static class UpdateVersionChecker
    {
        /// <summary>
        /// Сравнивает установленную и доступную версии и определяет тип обновления.
        /// </summary>
        /// <param name="currentVersion">Текущая установленная версия.</param>
        /// <param name="update">Метаданные доступного обновления.</param>
        /// <returns>
        /// <see langword="null"/>, если обновление не требуется;
        /// <see cref="UpdateKind.Normal"/>, если требуется обычное обновление;
        /// <see cref="UpdateKind.Full"/>, если требуется полное обновление.
        /// </returns>
        public static UpdateKind? Check(Version currentVersion, UpdateInfo update)
        {
            ArgumentNullException.ThrowIfNull(currentVersion);
            ArgumentNullException.ThrowIfNull(update);

            Version updateVersion = update.Version.ToVersion();

            // Актуальность версии проверяется первой: устаревший или тот же релиз
            // не должен запускать остальные проверки.
            if (updateVersion <= currentVersion)
            {
                return null;
            }

            // Обязательный релиз всегда требует полного обновления.
            if (update.Mandatory)
            {
                return UpdateKind.Full;
            }

            Version minimumVersion = update.MinimumVersion.ToVersion();

            // Ниже минимальной версии разрешено только полное обновление,
            // но решение об установке остаётся за UpdateMode.
            if (currentVersion < minimumVersion)
            {
                return UpdateKind.Full;
            }

            // Переход на новую Major-версию требует полного обновления.
            if (updateVersion.Major > currentVersion.Major)
            {
                return UpdateKind.Full;
            }

            return UpdateKind.Normal;
        }
    }

    /// <summary>
    /// Тип обновления, определяющий способ замены файлов.
    /// </summary>
    public enum UpdateKind
    {
        /// <summary>Заменяются файлы, входящие в новый релиз.</summary>
        Normal,

        /// <summary>Все установленные файлы резервируются перед установкой релиза.</summary>
        Full
    }
}