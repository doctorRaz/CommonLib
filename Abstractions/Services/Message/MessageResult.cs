namespace dRz.Abstractions.Services.Message
{
    /// <summary>Представляет результат выбора пользователя в сообщении.</summary>
    public enum MessageResult
    {
        /// <summary>Результат не определён.</summary>
        None = 0,

        /// <summary>Пользователь выбрал «ОК».</summary>
        Ok,

        /// <summary>Пользователь выбрал «Отмена».</summary>
        Cancel,

        /// <summary>Пользователь выбрал «Да».</summary>
        Yes,

        /// <summary>Пользователь выбрал «Нет».</summary>
        No,

        /// <summary>Пользователь выбрал «Повторить».</summary>
        Retry,

        /// <summary>Пользователь выбрал «Прервать».</summary>
        Abort,

        /// <summary>Пользователь выбрал «Пропустить».</summary>
        Ignore
    }
}