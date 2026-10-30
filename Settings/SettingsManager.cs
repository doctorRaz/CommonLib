using dRz.Abstractions.Settings;
using System;

namespace dRz.Settings
{
    /// <summary>Загружает настройки и обеспечивает значения по умолчанию.</summary>
    public sealed class SettingsManager
    {
        private readonly JsonSettingsStore _store;

        /// <summary>
        /// Инициализирует менеджер настроек с использованием JSON-хранилища.
        /// </summary>
        /// <param name="store">Хранилище настроек в формате JSON.</param>
        public SettingsManager(JsonSettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Загружает настройки или создаёт и сохраняет значения по умолчанию.</summary>
        public IAddonSettings Load()
        {
            AddonSettings settings = _store.Load() ?? new AddonSettings();
            settings.AttachStore(_store);

            if (_store.LoadUsedDefaults)
            {
                settings.Save();
            }

            return settings;
        }
    }
}