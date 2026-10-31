using System.Runtime.CompilerServices;

namespace dRz.Abstractions.Services.Message
{
    /// <summary>
    /// Сервис сообщений с поддержкой вывода в командную строку CAD.
    /// </summary>
    /// <seealso cref="IMessageService" />
    public interface ICommandLineMessageService : IMessageService
    {
        /// <summary>Выводит сообщение в командную строку CAD.</summary>
        /// <param name="message">Выводимое сообщение.</param>
        /// <param name="title">Необязательный заголовок сообщения.</param>
        /// <param name="caller">Вызывающий метод. Если значение равно <see langword="null"/>, оно определяется автоматически.</param>
        /// <example>
        /// Пример использования в C#:
        /// <code language="cs">
        /// <![CDATA[
        /// IMessageService msgService = new MyMessageService();
        /// msgService.ConsoleMessage("Консольное сообщение");
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>В зависимости от основной CAD-системы реализации может потребоваться обрамлять сообщение символами перевода строки (\n).</remarks>
        void ConsoleMessage(string message, string title = "", [CallerMemberName] string? caller = null);
    }
}