using dRz.Abstractions.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dRz.Infrastructure.Infrastructure
{
    /// <summary>
    /// Предоставляет сведения о сборке аддона, её версии, метаданных и расположении.
    /// </summary>
    /// <seealso cref="dRz.Abstractions.Infrastructure.IAddOnInfo" />
    public class AddOnInfo : IAddOnInfo
    {
        private readonly Assembly _assembly;

        // ---AppData ---
        private readonly string _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private readonly AssemblyMetadata _metadata;

        private DateTime? _buildDate;

        private string? _copyright;

        private string? _description;

        private string? _fileVersion;

        private string? _informationalVersion;

        private bool _isAutoVersion;

        private string? _productTitle;

        /// <summary>
        /// Создаёт объект со сведениями об указанной сборке аддона.
        /// </summary>
        /// <param name="assembly">Сборка, сведения о которой требуется получить.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> равен <see langword="null"/>.</exception>
        public AddOnInfo(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

            // 1. Базовые данные о путях (работа со строками — это быстро)
            AssemblyPath = _assembly.Location ?? string.Empty;

            // Расположение сборки не меняется в течение жизни AddOnInfo — проверяем один раз.
            IsNotNetworkPath = IsLocalPath(AssemblyPath);

            AssembleFullName = _assembly.FullName ?? string.Empty;

            AssemblyName assemblyName = _assembly.GetName();

            if (!string.IsNullOrEmpty(AssemblyPath))
            {
                AssemblyDirectory = Path.GetDirectoryName(AssemblyPath) ?? string.Empty;

                FileName = Path.GetFileNameWithoutExtension(AssemblyPath);
            }
            else
            {
                AssemblyDirectory = string.Empty;

                FileName = assemblyName.Name ?? "Unknown";
            }

            // 2. Данные версии (GetName тоже относительно быстр, но вызываем 1 раз)
            RunningVersion =
                assemblyName.Version ?? new Version(0, 0, 0, 0);

            // 3. Подготовка путей AppData (без Reflection)

            if (string.IsNullOrEmpty(_appData))
            {
                _appData = Path.GetTempPath(); // fallback
            }

            // Внимание: Product здесь берется лениво ниже,
            // поэтому для путей используем FilePrefix или вычисляем Product сразу, если он критичен.
            // Но лучше Product вычислить в конструкторе, так как он нужен для путей:
            Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? ExtractProductPrefix(FileName);

            Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "dRz";

            CompanyDataDirectory = Path.Combine(_appData, Company);

            ProductDataDirectory = Path.Combine(CompanyDataDirectory, Product);

            FileInfo? package = FindPackageFile(AssemblyDirectory, Product);

            PackageFileName = package?.Name;

            DirectoryInfo addonDirectory = new(AssemblyDirectory);
            DirectoryInfo? packageDirectory = addonDirectory.Parent;

            // Маркер пакета должен находиться непосредственно над каталогом аддона,
            // а имя этого каталога должно начинаться с Product без учёта регистра.
            bool packageIsInParent = package != null &&
                packageDirectory != null &&
                string.Equals(package.DirectoryName, packageDirectory.FullName, StringComparison.OrdinalIgnoreCase);

            IsProductDirectory = packageIsInParent &&
                Path.GetFileName(packageDirectory!.FullName).StartsWith(Product, StringComparison.OrdinalIgnoreCase);

            // При нештатном расположении не используем внешний родительский каталог
            // как корень полного обновления.
            PackageDirectory = IsProductDirectory
                ? packageDirectory!.FullName
                : AssemblyDirectory;

            _metadata = new AssemblyMetadata(_assembly);

            RepositoryUrl = GetMetadata(AssemblyMetadataKeys.RepositoryUrl, $"https://github.com/doctorRaz/{Product}");

            HostFamily = GetMetadata(AssemblyMetadataKeys.HostFamily, "");

            HostCode = GetMetadata(AssemblyMetadataKeys.HostCode, "");

            ProductFamily = GetMetadata(AssemblyMetadataKeys.ProductFamily, Product);

            ProductTitlePrefix = $"{ProductTitle} v.{RunningVersion.Major}.{RunningVersion.Minor}.{RunningVersion.Build}";
        }

        /// <summary>
        /// Проверяет, подтверждено ли локальное расположение основной сборки аддона.
        /// Неопределённый результат считается неподтверждённым локальным расположением.
        /// </summary>
        private static bool IsLocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // UNC-путь является сетевым независимо от доступности ресурса.
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                string? root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
                {
                    return false;
                }

                // DriveInfo распознаёт обычные подключённые сетевые диски.
                try
                {
                    if (new DriveInfo(root).DriveType == DriveType.Network)
                    {
                        return false;
                    }
                }
                catch (IOException)
                {
                    // Продолжаем проверку через QueryDosDevice.
                }
                catch (UnauthorizedAccessException)
                {
                    // Продолжаем проверку через QueryDosDevice.
                }

                return IsLocalDevice(root.Substring(0, 2), 0);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static bool IsLocalDevice(string drive, int depth)
        {
            // Не считаем расположение локальным, если цепочка SUBST слишком глубока.
            if (depth > 8)
            {
                return false;
            }

            char[] target = new char[1024];
            uint length;

            try
            {
                length = QueryDosDevice(drive, target, (uint)target.Length);
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }

            // Невозможность получить цель устройства означает неизвестное расположение.
            if (length == 0)
            {
                return false;
            }

            string devicePath = new string(target, 0, (int)length).Split('\0')[0];

            if (devicePath.StartsWith(@"\Device\Mup", StringComparison.OrdinalIgnoreCase) ||
                devicePath.StartsWith(@"\Device\LanmanRedirector", StringComparison.OrdinalIgnoreCase) ||
                devicePath.StartsWith(@"\??\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // SUBST может ссылаться на другой DOS-путь: проверяем всю цепочку.
            if (devicePath.StartsWith(@"\??\", StringComparison.OrdinalIgnoreCase))
            {
                string substitutedPath = devicePath.Substring(4);
                if (substitutedPath.Length >= 2 &&
                    char.IsLetter(substitutedPath[0]) &&
                    substitutedPath[1] == ':')
                {
                    return IsLocalDevice(substitutedPath.Substring(0, 2), depth + 1);
                }

                // Неизвестный формат цели не подтверждает локальное расположение.
                return false;
            }

            // QueryDosDevice успешно вернул цель, не относящуюся к сетевым или SUBST-путям.
            return true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint QueryDosDevice(
            string lpDeviceName,
            [Out] char[] lpTargetPath,
            uint ucchMax);

        /// <summary>Возвращает каталог общих данных компании.</summary>
        /// <value>Каталог для данных, общих для всех продуктов компании.</value>
        public string CompanyDataDirectory { get; }

        /// <summary>Возвращает каталог данных текущего продукта.</summary>
        /// <value>Каталог для данных, относящихся к текущему продукту.</value>
        public string ProductDataDirectory { get; }

        /// <summary>Возвращает название компании — разработчика сборки.</summary>
        /// <value>Название компании из <see cref="AssemblyCompanyAttribute"/> либо <c>dRz</c>, если атрибут отсутствует.</value>
        public string Company { get; }

        /// <summary>
        /// "Полное Имя" сборки, используется для показа в заголовках диалогов, окон, сообщений
        /// </summary>
        /// <value>Полное имя сборки, включающее сведения, предоставленные средой выполнения.</value>
        public string AssembleFullName { get; }

        /// <summary>Возвращает директорию сборки.</summary>
        /// <value>Директория сборки.</value>
        public string AssemblyDirectory { get; }

        /// <summary>Возвращает полный путь к сборке.</summary>
        /// <value>Полный путь к сборке.</value>
        public string AssemblyPath { get; }

        /// <summary>Возвращает дату-время компиляции сборки.</summary>
        /// <value>Дата-время компиляции сборки.</value>
        public DateTime BuildDate => _buildDate ??= ComputeBuildDate(_assembly, out _isAutoVersion);

        /// <summary>Возвращает информацию о копирайте.</summary>
        /// <value>Копирайт.</value>
        public string Copyright => _copyright ??= _assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "Unknown";

        /// <summary>Возвращает описание сборки.</summary>
        /// <value>Описание из <see cref="AssemblyDescriptionAttribute"/> либо <c>Unknown</c>, если атрибут отсутствует.</value>
        public string Description => _description ??= _assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Unknown";

        /// <summary>Возвращает имя файла сборки без расширения.</summary>
        /// <value>Имя файла сборки без расширения.</value>
        public string FileName { get; }

        /// <summary>Возвращает AssemblyFileVersionAttribute.</summary>
        public string FileVersion => _fileVersion ??= _assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "Unknown";

        /// <summary>Указывает, найден ли файл пакета с именем, начинающимся с имени продукта и точки.</summary>
        /// <value><see langword="true"/>, если файл пакета найден; иначе <see langword="false"/>.</value>
        public bool HasPackage => PackageFileName != null;

        /// <summary>
        /// Определяет, подтверждено ли локальное расположение основной сборки аддона.
        /// </summary>
        public bool IsNotNetworkPath { get; }

        /// <summary>
        /// Определяет, находится ли аддон в каталоге продукта.
        /// </summary>
        public bool IsProductDirectory { get; }

        /// <summary>Возвращает код CAD-хоста из метаданных сборки.</summary>
        /// <value>Значение метаданных <c>HostCode</c> либо пустая строка, если оно не задано.</value>
        public string HostCode { get; }

        /// <summary>Возвращает семейство CAD-хоста из метаданных сборки.</summary>
        /// <value>Значение метаданных <c>HostFamily</c> либо пустая строка, если оно не задано.</value>
        public string HostFamily { get; }

        /// <summary>Возвращает AssemblyInformationalVersionAttribute.</summary>
        public string InformationalVersion => _informationalVersion ??= _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? "Unknown";

        /// <summary>Возвращает версию установленной сборки.</summary>
        /// <value>версия сборки.</value>
        public Version InstalledVersion => AssemblyName.GetAssemblyName(AssemblyPath).Version ?? new Version(0, 0);

        /// <summary>Признак, что дата сборки получена из версии.</summary>
        public bool IsAutoVersion
        {
            get
            {
                if (_buildDate == null) { DateTime _ = BuildDate; } // Триггерим вычисление даты
                return _isAutoVersion;
            }
        }

        /// <summary>
        /// Все пары ключ-значение метаданных.
        /// Используется для диагностики и вывода информации.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> MetadataItems => _metadata.Items;

        /// <summary>Все доступные ключи метаданных.</summary>
        public IEnumerable<string> MetadataKeys => _metadata.Keys;

        /// <summary>Возвращает каталог пакета, используемый как корень аддона.</summary>
        /// <value>Каталог, содержащий файл пакета, если расположение подтверждено; иначе каталог сборки.</value>
        public string PackageDirectory { get; }

        /// <summary>Возвращает имя файла package.</summary>
        /// <value>Имя файла package.</value>
        public string? PackageFileName { get; }

        /// <summary>Возвращает название продукта из <see cref="AssemblyProductAttribute"/> либо из имени файла сборки.</summary>
        public string Product { get; }

        /// <summary>
        /// Возвращает идентификатор продукта в контексте CAD-хоста.
        /// </summary>
        /// <remarks>
        /// Значение берётся из метаданных сборки; если метаданные не заданы, используется название продукта.
        /// </remarks>
        public string ProductFamily { get; }

        /// <summary>Возвращает заголовок сборки из <see cref="AssemblyTitleAttribute"/> либо имя файла сборки.</summary>
        public string ProductTitle => _productTitle ??= _assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? FileName;

        /// <summary>Возвращает Product v.RunningVersion.</summary>
        public string ProductTitlePrefix { get; }

        /// <summary>Возвращает URL репозитория проекта.</summary>
        /// <value>Значение метаданных <c>RepositoryUrl</c> либо URL репозитория GitHub, сформированный по имени продукта.</value>
        public string RepositoryUrl { get; }

        /// <summary>Возвращает версию загруженной сборки.</summary>
        /// <value>версия сборки.</value>
        public Version RunningVersion { get; }

        /// <summary>Получает значение метаданных сборки по ключу.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <returns>Значение метаданных или <see langword="null"/>, если ключ отсутствует.</returns>
        public string? GetMetadata(string key)
        {
            return _metadata.Get(key);
        }

        /// <summary>Получает значение метаданных по ключу либо значение по умолчанию.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <param name="defaultValue">Значение, возвращаемое, если ключ отсутствует.</param>
        /// <returns>Найденное значение или <paramref name="defaultValue"/>.</returns>
        public string GetMetadata(string key, string defaultValue)
        {
            return _metadata.Get(key, defaultValue);
        }

        /// <summary>Возвращает подробное текстовое представление сведений о сборке.</summary>
        /// <returns>Многострочная строка с идентификаторами, версиями, путями и метаданными сборки.</returns>
        public string ToLongString()
        {
            return @$"{Product} v{RunningVersion}
  ProductTitle: {ProductTitle}
  Product: {Product}
  ProductTitlePrefix: {ProductTitlePrefix}
  HostFamily: {HostFamily}
  ProductFamily: {ProductFamily}
  RunningVersion: {RunningVersion}
  InformationalVersion: {InformationalVersion}
  FileName: {FileName}
  File: {AssemblyPath}
  AssemblyDirectory: {AssemblyDirectory}
  Copyright:{Copyright}
  Description: {Description}
  FileVersion: {FileVersion}
  BuildDate: {BuildDate}
  ";
        }

        /// <summary>Возвращает краткое текстовое представление сведений о сборке.</summary>
        /// <returns>Строка с заголовком продукта, версией и датой сборки.</returns>
        public string ToShortString()
        {
            return $"{ProductTitle} v{RunningVersion}({BuildDate:dd.MM.yyyy})";
        }

        /// <summary>Возвращает основное текстовое представление сведений о сборке.</summary>
        /// <returns>Строка с названием продукта, версией, именем файла и информационной версией.</returns>
        public override string ToString()
        {
            return $"{Product} v{RunningVersion}({BuildDate:dd.MM.yyyy}); assembly: {FileName}; [{InformationalVersion}]";
        }

        /// <summary>Пытается получить значение метаданных сборки по ключу.</summary>
        /// <param name="key">Ключ метаданных.</param>
        /// <param name="value">При успехе содержит найденное значение; иначе <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, если ключ найден; иначе <see langword="false"/>.</returns>
        public bool TryGetMetadata(string key, out string? value)
        {
            return _metadata.TryGet(key, out value);
        }

        /// <summary>Computes the build date.</summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="isAuto">if set to <c>true</c> [is automatic].</param>
        /// <returns></returns>
        private DateTime ComputeBuildDate(Assembly assembly, out bool isAuto)
        {
            Version? version = assembly.GetName().Version;

            // 1. Попробуем вычислить из Build/Revision
            DateTime? dt = TryGetBuildDate(version!);
            if (dt.HasValue)
            {
                isAuto = true;
                return dt.Value;
            }

            // 2. Файл сборки (fallback)
            try
            {
                if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location))
                {
                    isAuto = false;
                    return File.GetLastWriteTimeUtc(assembly.Location);
                }
            }
            catch
            {
                // ignore
            }

            // 3. Последний fallback
            isAuto = false;
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Extracts the product prefix.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        private string ExtractProductPrefix(string fileName)
        {
            int dotIndex = fileName.IndexOf('.');
            return dotIndex > 0
                ? fileName.Substring(0, dotIndex)
                : fileName;
        }

        //получаем путь к папке ROOT с аддоном, ищем в ней все пакеты
        private FileInfo? FindPackageFile(string startDirectory, string packageName)
        {
            string prefix = packageName + ".";

            DirectoryInfo current = new(startDirectory);
            DirectoryInfo? parent = current.Parent;

            foreach (DirectoryInfo? dir in new[] { parent, current })
            {
                if (dir == null)
                {
                    continue;
                }

                FileInfo? package = dir.EnumerateFiles("*.package",
                                                       SearchOption.TopDirectoryOnly)
                                                       .FirstOrDefault(f => f.Name.StartsWith(prefix,
                                                       StringComparison.OrdinalIgnoreCase));

                if (package != null)
                {
                    return package;
                }
            }

            // Если пакет не найден, по умолчанию считаем корнем родительский каталог
            return null;// parent?.FullName ?? current.FullName;
        }

        /// <summary>
        /// Tries the get build date.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        private DateTime? TryGetBuildDate(Version version)
        {
            if (version == null || version.Build < 0 || version.Revision < 0)
            {
                return null;
            }

            try
            {
                // .NET auto-version: Build = дни с 2000-01-01, Revision = секунды / 2
                DateTime baseDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime buildDate = baseDate.AddDays(version.Build).AddSeconds(version.Revision * 2);

                // Паранойя: если дата получилась из будущего, значит это не авто-версия .NET,
                // а просто какие-то числа от CI/CD системы.
                return buildDate > DateTime.UtcNow.AddDays(1) ? null : buildDate;
            }
            catch
            {
                return null;
            }
        }

        /*

используем FindPackageFile для поиска папки ROOT с аддоном, затем ищем в ней все *.bak и *.~* и удаляем их

string start = Path.GetDirectoryName(typeof(Updater).Assembly.Location)!;

string? root = FindPackageFile(start, "SpecSPDS");

  */
    }
}