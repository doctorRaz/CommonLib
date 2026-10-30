using System;

namespace dRz.Abstractions.Infrastructure
{
    /// <summary>
    /// Info CAd host
    /// </summary>
    public interface ICadInfo : IStringConvertible
    {
        #region Public Properties

        /// <summary>Gets the name of the company.</summary>
        /// <value>The name of the company.</value>
        string CompanyName { get; }

        /// <summary>Gets the copyright.</summary>
        /// <value>The copyright.</value>
        string Copyright { get; }

        /// <summary>Gets the executable path.</summary>
        /// <value>The executable path.</value>
        string ExePath { get; }

        /// <summary>Gets the file description.</summary>
        /// <value>The file description.</value>
        string FileDescription { get; }

        /// <summary>Gets the name of the file.</summary>
        /// <value>The name of the file.</value>
        string FileName { get; }

        /// <summary>Gets the file version.</summary>
        /// <value>The file version.</value>
        Version FileVersion { get; }

        /// <summary>Gets the host architecture.</summary>
        /// <value>The host architecture.</value>
        string HostArchitecture { get; }

        /// <summary>Gets the install directory.</summary>
        /// <value>The install directory.</value>
        string InstallDirectory { get; }

        /// <summary>
        /// Gets a value indicating whether [is64 bit process].
        /// </summary>
        /// <value><c>true</c> if [is64 bit process]; otherwise, <c>false</c>.</value>
        bool Is64BitProcess { get; }

        /// <summary>Gets a value indicating whether this instance is fallback.</summary>
        /// <value>
        ///   <c>true</c> if this instance is fallback; otherwise, <c>false</c>.
        /// </value>
        bool IsFallback { get; }

        /// <summary>Gets the original filename.</summary>
        /// <value>The original filename.</value>
        string OriginalFilename { get; }

        /// <summary>Gets the name of the product.</summary>
        /// <value>The name of the product.</value>
        string ProductName { get; }

        /// <summary>Gets the product version.</summary>
        /// <value>The product version.</value>
        Version ProductVersion { get; }

        #endregion Public Properties
    }
}