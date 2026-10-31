using System;
using System.Collections.Generic;

namespace dRz.Abstractions.Logger
{
    /// <summary>
    ///  Предоставляет интерфейс Fluent API для построения и записи событий логирования.
    /// </summary>
    public interface ILogEventBuilder
    {
        #region Public Methods

        /// <summary>
        /// Завершает построение и отправляет событие логирования в зарегистрированные таргеты.
        /// </summary>
        void Log();

        /// <summary>Exceptions the specified exception.</summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        ILogEventBuilder Exception(Exception exception);

        /// <summary>Messages the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        ILogEventBuilder Message(string message);

        /// <summary>Propertieses the specified properties.</summary>
        /// <param name="properties">The properties.</param>
        /// <returns></returns>
        ILogEventBuilder Properties(IEnumerable<KeyValuePair<string, object>> properties);

        /// <summary>Properties the specified name.</summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        ILogEventBuilder Property(string name, object value);

        #endregion Public Methods
    }
}