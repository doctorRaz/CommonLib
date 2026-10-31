using System;

namespace dRz.Updater.Models
{
    /// <summary>
    /// Четыре компонента версии из update.json.
    /// </summary>
    public sealed class VersionInfo
    {
        /// <summary>Старший компонент версии.</summary>
        public int Major { get; init; }

        /// <summary>Младший компонент версии.</summary>
        public int Minor { get; init; }

        /// <summary>Компонент сборки.</summary>
        public int Build { get; init; }

        /// <summary>Компонент ревизии.</summary>
        public int Revision { get; init; }

        /// <summary>
        /// Преобразует описание версии в <see cref="Version"/>.
        /// </summary>
        public Version ToVersion()
        {
            return new Version(Major, Minor, Build, Revision);
        }
    }
}