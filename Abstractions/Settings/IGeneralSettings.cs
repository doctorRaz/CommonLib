namespace dRz.Abstractions.Settings
{
    /// <summary>
    /// Общие настройки аддона, не относящиеся к отдельной подсистеме.
    /// </summary>
    public interface IGeneralSettings
    {
        /// <summary>
        /// Определяет, нужно ли выводить сведения о зарегистрированных командах при запуске.
        /// </summary>
        bool ShowCommandInfoOnStartup { get; set; }
    }
}