using System;

namespace dRz.Abstractions.Services
{
    /// <summary>Указатель на окно кад </summary>
    public interface IWindowHandleProvider
    {
        #region Public Properties

        /// <summary> Указатель на окно CAD </summary>
        IntPtr Handle { get; }

        #endregion Public Properties
    }
}