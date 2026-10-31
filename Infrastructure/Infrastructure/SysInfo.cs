using dRz.Abstractions.Infrastructure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management; // Не забудьте добавить ссылку

namespace dRz.Infrastructure.Infrastructure
{
    /// <summary>Предоставляет сведения об операционной системе и аппаратном обеспечении компьютера.</summary>
    public class SysInfo : ISysInfo
    {
        #region Private Fields

        private const string RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        //private static string? _gpuInfo;
        private static readonly Lazy<string> _gpuInfo = new(GetGpuData);

        //private static  string? _processorName;

        private static readonly Lazy<string> _processorName = new(() => GetWmiValue("Win32_Processor", "Name") ?? "Unknown");

        //private static string? _ramTotal;

        private static readonly Lazy<string> _ramTotal = new(() => GetRamTotal());

        #endregion Private Fields

        #region Public Constructors

        /// <summary>Инициализирует сведения об операционной системе, используя данные среды выполнения и реестра Windows.</summary>
        /// <remarks>Если данные реестра недоступны, сохраняются базовые значения и <see cref="IsFallback"/> остаётся равным <see langword="true"/>.</remarks>
        public SysInfo()
        {
            // Устанавливаем базовые значения через Environment (служит дефолтом)
            Architecture = Environment.Is64BitOperatingSystem ? "X64" : "X32";

            Version envVersion = Environment.OSVersion.Version;
            OsVersion = new Version(envVersion.Major,
                        envVersion.Minor,
                        Math.Max(0, envVersion.Build),
                        Math.Max(0, envVersion.Revision));

            IsFallback = true;

            // Пытаемся обогатить данными из реестра
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                                                                    RegistryView.Registry64);

                using RegistryKey? key = baseKey.OpenSubKey(RegPath);

                if (key != null)
                {
                    ProductName = GetString(key, "Product", ProductName);
                    DisplayVersion = GetString(key, "DisplayVersion") ?? GetString(key, "ReleaseId") ?? DisplayVersion;
                    EditionId = GetString(key, "EditionID", EditionId);
                    InstallationType = GetString(key, "InstallationType", InstallationType);
                    BuildLab = GetString(key, "BuildLab", BuildLab);

                    // Уточняем версию
                    int major = GetInt(key, "CurrentMajorVersionNumber", OsVersion.Major);
                    int minor = GetInt(key, "CurrentMinorVersionNumber", OsVersion.Minor);

                    // Если Win7/8 (CurrentMajor будет 0), пробуем парсить CurrentVersion
                    if (major == 0)
                    {
                        string cv = GetString(key, "CurrentVersion");
                        if (Version.TryParse(cv, out Version? parsed))
                        {
                            major = parsed.Major;
                            minor = parsed.Minor;
                        }
                    }

                    int build = int.TryParse(GetString(key, "CurrentBuild"), out int b) ? b : OsVersion.Build;
                    int rev = GetInt(key, "UBR", OsVersion.Revision);

                    OsVersion = new Version(major, minor, Math.Max(0, build), Math.Max(0, rev));

                    IsFallback = false;
                }
            }
            catch { }
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Возвращает разрядность операционной системы в формате <c>X64</c> или <c>X32</c>.</summary>
        public string Architecture { get; init; }

        /// <summary>Возвращает идентификатор сборки Windows из реестра.</summary>
        public string BuildLab { get; init; } = "Unknown";

        /// <summary>Возвращает отображаемую версию Windows.</summary>
        public string DisplayVersion { get; init; } = "Unknown";

        /// <summary>Возвращает идентификатор редакции Windows.</summary>
        public string EditionId { get; init; } = "Unknown";

        //public string GpuInfo => _gpuInfo ??= GetGpuData();
        /// <summary>Возвращает названия обнаруженных графических адаптеров или <c>Unknown</c>, если сведения недоступны.</summary>
        public string GpuInfo => _gpuInfo.Value;

        /// <summary>Возвращает тип установки Windows, например клиентскую или серверную.</summary>
        public string InstallationType { get; init; } = "Unknown";

        /// <summary>Указывает, что сведения из реестра не были получены и используются базовые значения.</summary>
        public bool IsFallback { get; init; }

        /// <summary>Возвращает версию операционной системы, включая номер сборки и ревизию.</summary>
        public Version OsVersion { get; init; }

        //public string ProcessorName => _processorName ??= GetWmiValue("Win32_Processor", "Name");
        /// <summary>Возвращает название процессора или <c>Unknown</c>, если сведения недоступны.</summary>
        public string ProcessorName => _processorName.Value;

        /// <summary>Возвращает название операционной системы.</summary>
        public string ProductName { get; init; } = "Windows";

        //public string RamTotalGb => _ramTotal ??= GetRamTotal();
        /// <summary>Возвращает общий объём оперативной памяти в гигабайтах в текстовом формате.</summary>
        public string RamTotalGb => _ramTotal.Value;

        /// <summary>Возвращает версию операционной системы в строковом формате.</summary>
        public string VersionString => OsVersion.ToString();

        #endregion Public Properties

        #region Private Properties

        private string defStr => $"{(IsFallback ? "OS (fallback):" : "OS:")} {ProductName} {DisplayVersion} ({OsVersion}) [{Architecture}]";

        private string longStr => $"CPU: {ProcessorName}\n" +
                                  $"RAM: {RamTotalGb}\n" +
                                  $"GPU: {GpuInfo}";

        private string shortStr => $"{(IsFallback ? "OS (fallback):" : "OS:")} {ProductName} {DisplayVersion} [{Architecture}]";

        #endregion Private Properties

        #region Public Methods

        /// <summary>Возвращает подробное текстовое представление операционной системы и аппаратного обеспечения.</summary>
        /// <returns>Строка с данными ОС, процессора, оперативной памяти и графических адаптеров.</returns>
        public string ToLongString() => $"{defStr}\n" +
                                         $"{longStr}";

        /// <summary>Возвращает краткое текстовое представление операционной системы.</summary>
        /// <returns>Строка с названием, версией и разрядностью ОС.</returns>
        public string ToShortString() => shortStr;

        /// <summary>Возвращает основное текстовое представление операционной системы.</summary>
        /// <returns>Строка с названием, версией и разрядностью ОС.</returns>
        public override string ToString() => defStr;

        #endregion Public Methods

        #region Private Methods

        private static string GetGpuData()
        {
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, VideoProcessor FROM Win32_VideoController");
                List<ManagementBaseObject> gpus = searcher.Get().Cast<ManagementBaseObject>()
                    .Where(obj =>
                        obj["VideoProcessor"] != null   // У виртуальных здесь часто null
                        ).ToList();

                if (gpus.Any())
                {
                    IEnumerable<string> results = gpus.Select(gpu =>
                    {
                        string name = gpu["Name"]?.ToString() ?? "Unknown";

                        return $"{name}".Trim();
                    });

                    return string.Join(" / ", results);
                }
            }
            catch { }
            return "Unknown";
        }

        private static int GetInt(RegistryKey key, string name, int fallback = 0) =>
                    key.GetValue(name) is int val ? val : fallback;

        private static string GetString(RegistryKey key, string name, string fallback = null!) =>
                    key.GetValue(name)?.ToString() ?? fallback;

        // Новые свойства железа
        private static string GetWmiValue(string table, string property)
        {
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {table}");
                foreach (ManagementBaseObject? obj in searcher.Get())
                {
                    return obj[property]?.ToString()?.Trim() ?? "Unknown";
                }
            }
            catch { }
            return "Unknown";
        }

        private static string GetRamTotal()
        {
            double.TryParse(GetWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory"), out double ramBytes);
            return $"{(ramBytes / (1024 * 1024 * 1024)):F1} GB";
        }

        #endregion Private Methods
    }
}