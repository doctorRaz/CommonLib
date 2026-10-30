using dRz.Abstractions.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace dRz.Infrastructure.Infrastructure;

/// <summary>
/// Сведения о CAD-хосте, в процессе которого загружено приложение.
/// </summary>
public class CadInfo : ICadInfo
{
    #region Public Constructors

    /// <summary>Инициализирует сведения о текущем CAD-процессе.</summary>
    /// <remarks>Если получить сведения о процессе не удаётся, используются значения по умолчанию и устанавливается <see cref="IsFallback"/>.</remarks>
    public CadInfo()
    {
        try
        {
            HostArchitecture = Environment.Is64BitProcess ? "X64" : "X32";// RuntimeInformation.ProcessArchitecture.ToString();

            string? path = GetExePath();

            if (string.IsNullOrEmpty(path))
            {
                throw new FileNotFoundException("Path not found");
            }

            ExePath = path!;

            FileInfo fileInfo = new FileInfo(path);

            InstallDirectory = fileInfo.DirectoryName ?? InstallDirectory; // Safe(() => new FileInfo(exePath).DirectoryName);

            FileName = fileInfo.Name;// Safe(() => Path.GetFileName(exePath));

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);// Safe(() => FileVersionInfo.GetVersionInfo(exePath));

            ProductName = fvi.ProductName ?? ProductName;

            CompanyName = fvi.CompanyName ?? CompanyName;

            FileDescription = fvi.FileDescription ?? FileDescription;

            OriginalFilename = fvi.OriginalFilename ?? OriginalFilename;

            Copyright = fvi.LegalCopyright ?? Copyright;

            ProductVersion = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart, fvi.ProductPrivatePart);

            FileVersion = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

            IsFallback = false;
        }
        catch
        {
            IsFallback = true;
        }
    }

    #endregion Public Constructors

    #region Public Properties

    // Значения по умолчанию
    /// <summary>Возвращает название компании — разработчика CAD-приложения.</summary>
    public string CompanyName { get; } = string.Empty;

    /// <summary>Возвращает сведения об авторских правах CAD-приложения.</summary>
    public string Copyright { get; } = string.Empty;

    /// <summary>Возвращает полный путь к исполняемому файлу CAD-процесса.</summary>
    public string ExePath { get; } = string.Empty;

    /// <summary>Возвращает описание исполняемого файла CAD-приложения.</summary>
    public string FileDescription { get; } = string.Empty;

    /// <summary>Возвращает имя исполняемого файла CAD-приложения.</summary>
    public string FileName { get; } = string.Empty;

    /// <summary>Возвращает версию файла CAD-приложения.</summary>
    public Version FileVersion { get; } = new(0, 0, 0, 0);

    /// <summary>Возвращает разрядность текущего процесса в формате <c>X64</c> или <c>X32</c>.</summary>
    public string HostArchitecture { get; } = string.Empty;

    /// <summary>Возвращает каталог установки CAD-приложения.</summary>
    public string InstallDirectory { get; } = string.Empty;

    /// <summary>Указывает, является ли текущий процесс 64-разрядным.</summary>
    public bool Is64BitProcess { get; } = Environment.Is64BitProcess;

    /// <summary>Указывает, что сведения о CAD-приложении не удалось полностью получить и остались значения по умолчанию.</summary>
    public bool IsFallback { get; private set; }

    /// <summary>Возвращает исходное имя файла из версии исполняемого файла.</summary>
    public string OriginalFilename { get; } = string.Empty;

    /// <summary>Возвращает название продукта CAD-приложения.</summary>
    public string ProductName { get; } = "Unknown CAD";

    /// <summary>Возвращает версию продукта CAD-приложения.</summary>
    public Version ProductVersion { get; } = new(0, 0, 0, 0);

    #endregion Public Properties

    #region Private Properties

    private string _defString =>
       $"{(IsFallback ? "CAD (fallback):" : "CAD:")}" +
       $"{(string.IsNullOrWhiteSpace(FileDescription) ? ProductName : FileDescription)}" +
       $"{ProductVersion} {FileVersion} [{HostArchitecture}]";

    private string _longString =>
       $"{(IsFallback ? "CAD (fallback):" : "CAD:")}" +
       $"{(string.IsNullOrWhiteSpace(FileDescription) ? ProductName : FileDescription)}" +
       $"{ProductVersion} {FileVersion} [{HostArchitecture}] CompanyName:{CompanyName} Copyright:{Copyright} Product:{ProductName}";

    private string _shortString =>
       $"{(IsFallback ? "CAD (fallback):" : "CAD:")}" +
       $"{(string.IsNullOrWhiteSpace(FileDescription) ? ProductName : FileDescription)}" +
       $"{ProductVersion} [{HostArchitecture}]";

    #endregion Private Properties

    #region Public Methods

    /// <summary>Возвращает подробное текстовое представление сведений о CAD-приложении.</summary>
    /// <returns>Строка с версиями, разрядностью и дополнительными сведениями о продукте.</returns>
    public string ToLongString()
    {
        return _longString;
    }

    /// <summary>Возвращает краткое текстовое представление сведений о CAD-приложении.</summary>
    /// <returns>Строка с названием, версиями продукта и разрядностью.</returns>
    public string ToShortString()
    {
        return _shortString;
    }

    /// <summary>Возвращает основное текстовое представление сведений о CAD-приложении.</summary>
    /// <returns>Строка с названием и версиями продукта и файла.</returns>
    public override string ToString()
    {
        return _defString;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Получить полный путь к exe текущего процесса с fallback
    /// </summary>
    private static string? GetExePath()
    {
        // Сначала через Process
        try
        {
            string? path = Process.GetCurrentProcess().MainModule?.FileName;

            //path = @"c:\Program Files\Nanosoft\nanoCAD x64 26.0\nCads.exe";//todo заглушка
            //path = @"c:\Program Files\Autodesk\AutoCAD 2026\acad.exe";//todo заглушка

            if (!string.IsNullOrEmpty(path))
            {
                return path!; // подавляем предупреждение — path точно не null тут
            }
        }
        catch { } // fallback

        // Через нативный GetModuleFileName
        try
        {
            StringBuilder sb = new(1024);
            return GetModuleFileName(IntPtr.Zero, sb, sb.Capacity) > 0 ? sb.ToString() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

    #endregion Private Methods
}