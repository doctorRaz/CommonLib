namespace dRz.Abstractions.Services.Message
{
    /// <summary>Сервис оконных сообщений.</summary>
    /// <remarks>Объединяет обычные и интерактивные сообщения без дублирования их контрактов.</remarks>
    public interface IWindowMessageService : IMessageService, IMessagePromptService
    {
    }
}