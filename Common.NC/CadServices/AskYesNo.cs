using System.ComponentModel;

namespace dRz.Common.NC.CadServices
{
    /// <summary>
    /// Результат ответа пользователя на вопрос с вариантами «Да» и «Нет».
    /// </summary>
    public enum AskYesNo
    {
        /// <summary>
        /// Пользователь подтвердил действие.
        /// </summary>
        [Description("Да")]
        Yes,

        /// <summary>
        /// Пользователь отказался от действия.
        /// </summary>
        [Description("Нет")]
        No
    }
}