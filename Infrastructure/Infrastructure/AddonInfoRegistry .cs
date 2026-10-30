using dRz.Abstractions.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace dRz.Infrastructure.Infrastructure
{
    /// <summary>
    /// Глобальный реестр информации о загруженных сборках.
    /// Экземпляр существует один на процесс.
    /// Используется ConcurrentDictionary, поэтому безопасен
    /// при регистрации из нескольких потоков.
    /// </summary>
    public class AddOnInfoRegistry : IAddOnInfoRegistry
    {
        #region Private Fields

        /// <summary>
        /// Хранилище.
        /// Ключ - полный путь к файлу
        /// </summary>
        private static readonly ConcurrentDictionary<string, IAddOnInfo> _addons = new();

        #endregion Private Fields

        #region Public Properties

        /// <summary>Возвращает количество сборок, зарегистрированных в реестре.</summary>
        public int Count => _addons.Count;

        #endregion Public Properties

        #region Public Methods

        /// <summary>Возвращает пути сборок, зарегистрированных в реестре.</summary>
        /// <returns>Коллекция ключей реестра — полных путей к сборкам.</returns>
        public ICollection<string> GetKeys()
        {
            return _addons.Keys;
        }

        /// <summary>Возвращает сведения о сборке из реестра или создаёт и регистрирует их.</summary>
        /// <param name="assembly">Сборка, для которой требуются сведения.</param>
        /// <returns>Зарегистрированные сведения о сборке.</returns>
        public IAddOnInfo GetOrAdd(Assembly assembly)
        {
            return _addons.GetOrAdd(assembly.Location, key => new AddOnInfo(assembly));
        }

        /// <summary>Возвращает сведения о сборке, содержащей тип <typeparamref name="T"/>, или регистрирует их.</summary>
        /// <typeparam name="T">Тип, определяющий сборку.</typeparam>
        /// <returns>Зарегистрированные сведения о сборке.</returns>
        public IAddOnInfo GetOrAdd<T>() => GetOrAdd(typeof(T).Assembly);

        /// <summary>Возвращает сведения о сборке, содержащей указанный тип, или регистрирует их.</summary>
        /// <param name="type">Тип, определяющий сборку.</param>
        /// <returns>Зарегистрированные сведения о сборке.</returns>
        public IAddOnInfo GetOrAdd(Type type) => GetOrAdd(type.Assembly);

        /// <summary>Возвращает сведения обо всех сборках, зарегистрированных на текущий момент.</summary>
        /// <returns>Снимок значений реестра.</returns>
        public IReadOnlyCollection<IAddOnInfo> GetValues()
        {
            return _addons.Values.ToArray();
        }

        /// <summary>Пытается получить сведения о сборке из реестра без её регистрации.</summary>
        /// <param name="assembly">Сборка для поиска.</param>
        /// <param name="info">При успехе содержит сведения о сборке; иначе <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, если сборка найдена; иначе <see langword="false"/>.</returns>
        public bool TryGet(Assembly assembly, out IAddOnInfo? info)
        {
            return _addons.TryGetValue(
                assembly.Location,
                out info);
        }

        /// <summary>
        /// Пытается получить информацию об аддоне по типу.
        /// </summary>
        /// <typeparam name="T">
        /// Тип, определяющий сборку, для которой требуется получить информацию.
        /// </typeparam>
        /// <param name="info">
        /// При успешном выполнении содержит информацию об аддоне.
        /// </param>
        /// <returns>
        /// <see langword="true"/>, если информация найдена; иначе <see langword="false"/>.
        /// </returns>
        public bool TryGet<T>(out IAddOnInfo? info)
        {
            return TryGet(
                typeof(T).Assembly,
                out info);
        }

        /// <summary>
        /// Пытается получить информацию об аддоне по типу.
        /// </summary>
        /// <param name="type">
        /// Тип, определяющий сборку, для которой требуется получить информацию.
        /// </param>
        /// <param name="info">
        /// При успешном выполнении содержит информацию об аддоне.
        /// </param>
        /// <returns>
        /// <see langword="true"/>, если информация найдена; иначе <see langword="false"/>.
        /// </returns>
        public bool TryGet(Type type, out IAddOnInfo? info)
        {
            return TryGet(type.Assembly,
                out info);
        }

        #endregion Public Methods
    }
}