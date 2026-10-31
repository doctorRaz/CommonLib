using System;

namespace dRz.Abstractions.Services.Message
{
    /// <summary>Определяет значок, отображаемый в сообщении пользователю.</summary>
    [Flags]
    public enum MessageIcon : uint
    {
        /// <summary>Не отображать значок.</summary>
        None = 0x00000000,

        /// <summary>Значок ошибки.</summary>
        Error = 0x00000010,

        /// <summary>Значок вопроса.</summary>
        Question = 0x00000020,

        /// <summary>Значок предупреждения.</summary>
        Warning = 0x00000030,

        /// <summary>Значок информационного сообщения.</summary>
        Information = 0x00000040
    }
}