using System;
using System.Collections.Generic;
using System.Reflection;

namespace dRz.Abstractions.Infrastructure
{
    /// <summary>
    /// Глобальный реестр информации о загруженных аддонах<br/>
    /// Один экземпляр существует на процесс.<br/>
    /// По хорошему этот класс наружу показывать не надо<br/>
    /// Доступ должен на добавление или чтение только через контейнер
    /// </summary>
    public interface IAddOnInfoRegistry
    {
        #region Public Methods

        /// <summary>
        /// Возвращает все пути (ключи) к зарегистрированным аддонам
        /// </summary>
        ICollection<string> GetKeys();

        // изменение вне контейнера только для тестов
#if DEBUG1
        /// <summary>
        /// Регистрирует информацию об аддоне.
        /// Если запись уже существует, возвращает существующий объект.
        /// </summary>
        IAddOnInfo GetOrAdd(Assembly assembly);

        /// <summary>Gets the or add.</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IAddOnInfo GetOrAdd<T>();

        /// <summary>Gets the or add.</summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        IAddOnInfo GetOrAdd(Type type);

#endif

        /// <summary>
        /// Возвращает все зарегистрированные аддоны.
        /// </summary>
        IReadOnlyCollection<IAddOnInfo> GetValues();

        /// <summary>
        /// Возвращает информацию по полному пути сборки.
        /// </summary>
        bool TryGet(Assembly assembly, out IAddOnInfo? info);

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
        bool TryGet<T>(out IAddOnInfo? info);

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
        bool TryGet(Type type, out IAddOnInfo? info);

        /// <summary>Количество зарегистрированных IAddOnInfo</summary>
        /// <value>The count.</value>
        int Count { get; }

        #endregion Public Methods
    }
}