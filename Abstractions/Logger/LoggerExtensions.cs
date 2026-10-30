using System;
using System.Runtime.CompilerServices;

namespace dRz.Abstractions.Logger
{
    /// <summary>
    /// Дополнительные методы логирования с указанием вызывающего метода и строки.<br/>
    ///  [{memberName}:{line}] {message}
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>Logs a debug message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void DebugCaller(
                        this IDrzLogger logger,
                        string message,
                        [CallerMemberName] string memberName = "",
                        [CallerLineNumber] int line = 0)
        {
            logger.Debug(FormatCaller(message, memberName, line));
        }

        /// <summary>Logs a trace message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void TraceCaller(
                        this IDrzLogger logger,
                        string message,
                        [CallerMemberName] string memberName = "",
                        [CallerLineNumber] int line = 0)
        {
            logger.Trace(FormatCaller(message, memberName, line));
        }

        /// <summary>Logs an information message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void InfoCaller(
                this IDrzLogger logger,
                string message,
                [CallerMemberName] string memberName = "",
                [CallerLineNumber] int line = 0)
        {
            logger.Info(FormatCaller(message, memberName, line));
        }

        /// <summary>Logs a warning message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void WarnCaller(
                this IDrzLogger logger,
                string message,
                [CallerMemberName] string memberName = "",
                [CallerLineNumber] int line = 0)
        {
            logger.Warn(FormatCaller(message, memberName, line));
        }

        /// <summary>Logs an error message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void ErrorCaller(
                this IDrzLogger logger,
                Exception exception,
                string? message = null,
                [CallerMemberName] string memberName = "",
                [CallerLineNumber] int line = 0)
        {
            logger.Error(FormatCaller(message, memberName, line), exception);
        }

        /// <summary>Logs an error message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void ErrorCaller(
        this IDrzLogger logger,
        string message,
        Exception? exception = null,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0)
        {
            logger.Error(FormatCaller(message, memberName, line), exception);
        }

        /// <summary>Logs a fatal message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void FatalCaller(
        this IDrzLogger logger,
        Exception exception,
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0)
        {
            logger.Fatal(FormatCaller(message, memberName, line), exception);
        }

        /// <summary>Logs a fatal message with caller information.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        public static void FatalCaller(
        this IDrzLogger logger,
        string message,
        Exception? exception = null,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int line = 0)
        {
            logger.Fatal(FormatCaller(message, memberName, line), exception);
        }

        /// <summary>Formats a message with caller information.</summary>
        /// <param name="message">The message.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="line">The line.</param>
        /// <returns>Message with the caller information prefix.</returns>
        internal static string FormatCaller(
                                string? message,
                                string memberName,
                                int line)
        {
            string prefix = $"[{memberName}:{line}]";

            return string.IsNullOrEmpty(message)
                ? prefix
                : $"{prefix} {message}";
        }
    }
}