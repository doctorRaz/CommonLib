using System;

namespace dRz.Updater.Services
{
    /// <summary>Входные параметры для проверки и установки обновления.</summary>
    public sealed class UpdateRequest
    {
        /// <summary>Версия сборки аддона на диске.</summary>
        public Version CurrentVersion { get; init; } = new Version();

        /// <summary>Версия сборки аддона, загруженной в хост (в памяти).</summary>
        public Version RunningVersion { get; init; } = new Version();

        /// <summary>URL источника файла update.json.</summary>
        public string UpdateUrl { get; init; } = string.Empty;

        /// <summary>Способ запуска проверки. При ручном запуске используется CheckAndNotify.</summary>
        public UpdateLaunchMode LaunchMode { get; init; } = UpdateLaunchMode.Automatic;

        /// <summary>Полный путь к целевому каталогу установленного аддона или модуля.</summary>
        public string PackageDirectory { get; init; } = string.Empty;

        /// <summary>Определяет структуру каталога продукта внутри архива.</summary>
        public bool IsPackage { get; init; }

        /// <summary>
        /// Признак достоверно подтверждённого локального расположения основной сборки аддона.
        /// При <see langword="false"/> обновление запрещено, поскольку путь сетевой или его тип не удалось определить.
        /// </summary>
        public bool IsNotNetworkPath { get; init; }
    }
}