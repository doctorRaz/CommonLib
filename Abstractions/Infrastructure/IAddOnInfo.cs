using System;
using System.Collections.Generic;

namespace dRz.Abstractions.Infrastructure
{
    /// <summary>Информация о сборке </summary>
    public interface IAddOnInfo : IStringConvertible
    {
        #region Public Properties

        /// <summary>Возвращает каталог общих данных компании.</summary>
        /// <value>Каталог для данных, общих для всех продуктов компании.</value>
        string CompanyDataDirectory { get; }

        /// <summary>Возвращает каталог данных текущего продукта.</summary>
        /// <value>Каталог для данных, относящихся к текущему продукту.</value>
        string ProductDataDirectory { get; }

        /// <summary>Gets the company.</summary>
        /// <value>The company.</value>
        string Company { get; }

        /// <summary>Возвращает путь к корневому каталогу ад дона где находится package</summary>
        /// <value>путь к корневому каталогу ад дона</value>
        string PackageDirectory { get; }

        /// <summary>Возвращает имя файла package.</summary>
        /// <value>Имя файла package.</value>
        string? PackageFileName { get; }

        /// <summary>Gets a value indicating whether this instance has package.</summary>
        /// <value>
        ///   <c>true</c> if this instance has package; otherwise, <c>false</c>.
        /// </value>
        bool HasPackage { get; }

        /// <summary>
        /// Определяет, находится ли аддон в каталоге продукта.
        /// </summary>
        bool IsProductDirectory { get; }

        /// <summary>
        /// Определяет, подтверждено ли локальное расположение основной сборки аддона.
        /// </summary>
        /// <value>
        /// <see langword="true"/>, если расположение достоверно определено как локальное;
        /// иначе <see langword="false"/>, если путь сетевой или тип расположения не удалось определить.
        /// </value>
        bool IsNotNetworkPath { get; }

        /// <summary> "Полное Имя" сборки, используется для показа в заголовках диалогов, окон, сообщений </summary>
        /// <value>"Полное Имя" сборки.</value>
        string AssembleFullName { get; }

        /// <summary> Возвращает директорию сборки. </summary>
        /// <value> Директория сборки. </value>
        string AssemblyDirectory { get; }

        /// <summary>Возвращает полный путь к сборке.</summary>
        /// <value>Полный путь к сборке.</value>
        string AssemblyPath { get; }

        /// <summary>Возвращает версию ЗАГРУЖЕННОЙ в ХОСТ сборки.</summary>
        /// <value>версия сборки.</value>
        Version RunningVersion { get; }

        /// <summary>Возвращает версию ФАЙЛА сборки, может отличаться от RunningVersion загруженной в ХОСТ.<br/>
        /// Не обязательно загружена в ХОСТ, копия лежит по тому же пути что и asssemble </summary>
        /// <value>версия сборки.</value>
        Version InstalledVersion { get; }

        /// <summary>Возвращает дату-время компиляции сборки.</summary>
        /// <value>Дата-время компиляции сборки.</value>
        DateTime BuildDate { get; }

        /// <summary>Возвращает информацию о копирайте.</summary>
        /// <value>Копирайт.</value>
        string Copyright { get; }

        /// <summary>Возвращает описание сборки.</summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>Возвращает имя файла сборки без расширения.</summary>
        /// <value>Имя файла сборки без расширения.</value>
        string FileName { get; }

        /// <summary>Возвращает AssemblyFileVersionAttribute.</summary>
        string FileVersion { get; }

        /// <summary>Возвращает AssemblyInformationalVersionAttribute.</summary>
        string InformationalVersion { get; }

        /// <summary>Признак, что дата сборки получена из версии.</summary>
        bool IsAutoVersion { get; }

        /// <summary>Возвращает AssemblyProductAttribute.</summary>
        string Product { get; }

        /// <summary>Возвращает ProductName+HostCode<br/>
        ///идентификатор приложения в контексте хоста <br/>
        ///например, для AutoCAD: "SpecSPDSa<br/>
        ///например, для nanoCAD: "SpecSPDSn<br/>
        ///не зависит от названия сборки, берется из метадаты<br/>
        /// </summary>
        string ProductFamily { get; }//

        /// <summary>Возвращает AssemblyTitleAttribute.</summary>
        string ProductTitle { get; }

        /// <summary>Возвращает ProductName v.RunningVersion.</summary>
        string ProductTitlePrefix { get; }

        /// <summary>Gets the repository URL.</summary>
        /// <value>The repository URL.</value>
        string RepositoryUrl { get; }

        /// <summary>
        /// Семейство хоста, для которого собрана сборка
        /// </summary>
        string HostFamily { get; }

        /// <summary>
        /// Короткий код хоста для условной компиляции, логов и имен файлов
        /// </summary>
        string HostCode { get; }

        /// <summary>
        /// Все доступные ключи метаданных.
        /// </summary>
        IEnumerable<string> MetadataKeys { get; }

        /// <summary>
        /// Все пары ключ-значение метаданных.
        /// Используется для диагностики и вывода информации.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> MetadataItems { get; }

        /// <summary>
        /// Получить значение метаданных.
        /// </summary>
        string? GetMetadata(string key);

        /// <summary>
        /// Получить значение метаданных с заданным значением по умолчанию.
        /// </summary>
        string GetMetadata(string key, string defaultValue);

        /// <summary>
        /// Попытаться получить значение метаданных.
        /// </summary>
        bool TryGetMetadata(string key, out string? value);

        #endregion Public Properties
    }
}