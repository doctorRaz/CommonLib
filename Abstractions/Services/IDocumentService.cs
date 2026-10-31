namespace dRz.Abstractions.Services
{
    /// <summary>Информация о активном документе</summary>
    public interface IDocumentService
    {
        #region Public Properties

        /// <summary> Имя файла с расширением. Если документа нет, должен генерировать ошибку </summary>
        string FileName { get; }

        /// <summary> Имя файла без расширения. Если документа нет, должен генерировать ошибку </summary>
        string FileNameNoExtension { get; }

        /// <summary> Полный путь к документу. В случае, если документа нет, должен генерировать ошибку </summary>
        string FullPath { get; }

        /// <summary> Активен ли документ </summary>
        bool IsActive { get; }

        #endregion Public Properties
    }
}