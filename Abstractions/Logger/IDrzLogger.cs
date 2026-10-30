using System;

namespace dRz.Abstractions.Logger
{
    /// <summary> Provides logging interface and utility functions. </summary>
    public interface IDrzLogger
    {
        #region Public Properties

        /// <summary>Gets a value indicating whether this instance is debug enabled.</summary>
        /// <value><c>true</c> if this instance is debug enabled; otherwise, <c>false</c>.</value>
        bool IsDebugEnabled { get; }

        /// <summary>Gets a value indicating whether this instance is error enabled.</summary>
        /// <value><c>true</c> if this instance is error enabled; otherwise, <c>false</c>.</value>
        bool IsErrorEnabled { get; }

        /// <summary>Gets a value indicating whether this instance is fatal enabled.</summary>
        /// <value><c>true</c> if this instance is fatal enabled; otherwise, <c>false</c>.</value>
        bool IsFatalEnabled { get; }

        /// <summary>Gets a value indicating whether this instance is information enabled.</summary>
        /// <value><c>true</c> if this instance is information enabled; otherwise, <c>false</c>.</value>
        bool IsInfoEnabled { get; }

        /// <summary>Gets a value indicating whether this instance is trace enabled.</summary>
        /// <value><c>true</c> if this instance is trace enabled; otherwise, <c>false</c>.</value>
        bool IsTraceEnabled { get; }

        /// <summary>Gets a value indicating whether this instance is warning enabled.</summary>
        /// <value><c>true</c> if this instance is warning enabled; otherwise, <c>false</c>.</value>
        bool IsWarnEnabled { get; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>Logs a debug message.</summary>
        /// <param name="message">The message.</param>
        void Debug(string message);

        /// <summary>Logs an error message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        void Error(string message, Exception? exception = null);

        /// <summary>Logs an error exception.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        void Error(Exception exception, string? message = null);

        /// <summary>Logs a fatal message with the specified exception.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        void Fatal(Exception exception, string? message = null);

        /// <summary>Logs a fatal message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        void Fatal(string message, Exception? exception = null);

        /// <summary>Creates a builder for a debug event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForDebugEvent();

        /// <summary>Creates a builder for an error event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForErrorEvent();

        /// <summary>Creates a builder for a fatal event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForFatalEvent();

        /// <summary>Creates a builder for an information event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForInfoEvent();

        /// <summary>Creates a builder for a trace event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForTraceEvent();

        /// <summary>Creates a builder for a warning event.</summary>
        /// <returns></returns>
        ILogEventBuilder ForWarnEvent();

        /// <summary>Logs an information message.</summary>
        /// <param name="message">The message.</param>
        void Info(string message);

        /// <summary>Logs a trace message.</summary>
        /// <param name="message">The message.</param>
        void Trace(string message);

        /// <summary>Logs a warning message.</summary>
        /// <param name="message">The message.</param>
        void Warn(string message);

        #endregion Public Methods
    }
}