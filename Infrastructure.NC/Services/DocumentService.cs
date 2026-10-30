using dRz.Abstractions.Services;
using System;
using System.IO;

#if !TEST

using HostMgd.ApplicationServices;

#endif

namespace dRz.Infrastructure.NC.Services
{
    /// <summary>
    /// Предоставляет информацию об активном документе nanoCAD.
    /// </summary>
    /// <remarks>
    /// В тестовой сборке сведения о документе имитируются; состояние активности
    /// задаётся через свойство <see cref="IsActive"/>.
    /// </remarks>
    public class DocumentService : IDocumentService
    {
        #region Public Properties

        /// <summary>
        /// Возвращает имя файла активного документа вместе с расширением.
        /// </summary>
        /// <exception cref="NullReferenceException">Активный документ отсутствует.</exception>
        public string FileName
        {
            get
            {
                return Path.GetFileName(FullPath);
            }
        }

        /// <summary>
        /// Возвращает имя файла активного документа без расширения.
        /// </summary>
        /// <exception cref="NullReferenceException">Активный документ отсутствует.</exception>
        public string FileNameNoExtension
        {
            get
            {
                return Path.GetFileNameWithoutExtension(FullPath);
            }
        }

        /// <summary>
        /// Возвращает полный путь активного документа.
        /// </summary>
        /// <exception cref="NullReferenceException">Активный документ отсутствует.</exception>
        public string FullPath
        {
            get
            {
                if (!IsActive)
                {
                    throw new NullReferenceException("Нет активного документа");
                }
#if !TEST
                Document doc = Application.DocumentManager.MdiActiveDocument;
                return doc.Name;
#else
                return "Doc Name TEST";
#endif
            }
        }

#if !TEST

        /// <summary>
        /// Возвращает признак наличия активного документа в nanoCAD.
        /// </summary>
        public bool IsActive
        {
            get
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                return doc != null;
            }
        }

#else

        /// <summary>The is active</summary>
        private bool _isActive = true;

        /// <summary>
        /// Возвращает или задаёт тестовый признак активности документа.
        /// </summary>
        /// <remarks>Используется в тестовой сборке для имитации наличия активного документа.</remarks>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

#endif

        #endregion Public Properties
    }
}