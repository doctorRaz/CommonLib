using System;

namespace dRz.Abstractions.Services.Message
{
    /// <summary>Определяет набор кнопок, отображаемых в сообщении пользователю.</summary>
    [Flags]
    public enum MessageButtons : uint
    {
        /// <summary>Единственная кнопка «ОК».</summary>
        Ok = 0x00000000,

        /// <summary>Кнопки «ОК» и «Отмена».</summary>
        OkCancel = 0x00000001,

        /// <summary>Кнопки «Прервать», «Повторить» и «Пропустить».</summary>
        AbortRetryIgnore = 0x00000002,

        /// <summary>Кнопки «Да», «Нет» и «Отмена».</summary>
        YesNoCancel = 0x00000003,

        /// <summary>Кнопки «Да» и «Нет».</summary>
        YesNo = 0x00000004,

        /// <summary>Кнопки «Повторить» и «Отмена».</summary>
        RetryCancel = 0x00000005
    }
}