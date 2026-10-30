using dRz.Abstractions.Services;
using System;
using System.Diagnostics;

#if !TEST

using HostMgd.ApplicationServices;

#endif

namespace dRz.Infrastructure.NC.Services
{
    /// <summary>
    /// Предоставляет дескриптор главного окна текущего процесса без зависимости от API CAD.
    /// </summary>
    /// <seealso cref="IWindowHandleProvider" />
    public class CadWindowProvider : IWindowHandleProvider
    {
        #region Private Fields

        private IntPtr? _handle;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Возвращает дескриптор главного окна текущего процесса.
        /// </summary>
        /// <remarks>
        /// Значение кэшируется; если дескриптор не задан или равен <see cref="IntPtr.Zero"/>,
        /// выполняется повторное получение из текущего процесса.
        /// </remarks>
        public IntPtr Handle
        {
            get
            {
                // Кэшируем, чтобы не опрашивать процесс постоянно
                if (!_handle.HasValue || _handle.Value == IntPtr.Zero)
                {
                    // Получаем хэндл главного окна текущего запущенного процесса
                    _handle = Process.GetCurrentProcess().MainWindowHandle;
                }
                return _handle.Value;
            }
        }

        #endregion Public Properties
    }

    /// <summary>
    /// Предоставляет дескриптор главного окна через API HostMgd.
    /// </summary>
    /// <remarks>В текущем коде этот вариант помечен как неиспользуемый.</remarks>
    /// <seealso cref="IWindowHandleProvider" />
    public class CadWindowProvider_Cad : IWindowHandleProvider
    {
#if !TEST

        /// <summary>
        /// Возвращает дескриптор главного окна CAD.
        /// </summary>
        public IntPtr Handle => Application.MainWindow.Handle;

#else

        /// <summary>
        /// Возвращает <see cref="IntPtr.Zero"/> в тестовой сборке.
        /// </summary>
        public IntPtr Handle => IntPtr.Zero;

#endif
    }
}