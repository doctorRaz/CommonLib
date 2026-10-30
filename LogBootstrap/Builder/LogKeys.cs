namespace dRz.LogBootstrap.Builder
{
    /// <summary>
    /// Имена файлов, в первых строках которых задаются уровни логирования.
    /// </summary>
    internal static class LogKeys
    {
        /// <summary>
        /// Имя файла настройки уровня Internal Logger.
        /// Первая строка должна содержать уровень NLog, включая <c>Off</c>.
        /// </summary>
        public const string DiagnosticMode = "diagnostic.level";

        /// <summary>
        /// Имя файла настройки уровня основного логирования.
        /// Первая строка должна содержать уровень NLog, включая <c>Off</c>.
        /// </summary>
        public const string LogLevel = "logger.level";
    }
}