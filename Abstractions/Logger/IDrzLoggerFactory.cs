using System;

namespace dRz.Abstractions.Logger
{
    /// <summary>
    /// Фабрика логгеров.
    /// </summary>
    public interface IDrzLoggerFactory : IInternalLogLevelProvider
    {
        #region Public Methods

        /// <summary>
        /// Возвращает логгер для указанного типа.
        /// </summary>
        /// <typeparam name="T">Тип, для которого создаётся логгер.</typeparam>
        /// <returns>Логгер для указанного типа.</returns>
        IDrzLogger GetLogger<T>();

        /// <summary>
        /// Возвращает логгер для указанного типа.
        /// </summary>
        /// <param name="type">Тип, для которого создаётся логгер.</param>
        /// <returns>Логгер для указанного типа.</returns>
        IDrzLogger GetLogger(Type type);

        #endregion Public Methods
    }
}