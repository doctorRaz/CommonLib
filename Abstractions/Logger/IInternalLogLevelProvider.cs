namespace dRz.Abstractions.Logger
{
    /// <summary>
    /// Предоставляет текущий уровень внутренней диагностики используемой системы логирования.
    /// </summary>
    public interface IInternalLogLevelProvider
    {
        /// <summary>
        /// Возвращает фактически установленный уровень внутренней диагностики.
        /// </summary>
        string InternalLogLevel { get; }
    }
}
