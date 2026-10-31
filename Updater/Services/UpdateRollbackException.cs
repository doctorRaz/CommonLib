using System;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Ошибка установки, при которой откат также завершился ошибкой.
    /// </summary>
    public sealed class UpdateRollbackException : Exception
    {
        /// <summary>
        /// Создаёт исключение с исходной ошибкой установки и первой ошибкой отката.
        /// </summary>
        public UpdateRollbackException(Exception installException, Exception rollbackException)
            : base("Установка обновления завершилась ошибкой; откат прерван после первой ошибки.",
                  new AggregateException(installException, rollbackException))
        {
            InstallException = installException;
            RollbackException = rollbackException;
        }

        /// <summary>Исходная ошибка установки.</summary>
        public Exception InstallException { get; }

        /// <summary>Первая ошибка отката.</summary>
        public Exception RollbackException { get; }
    }
}