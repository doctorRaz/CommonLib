using dRz.Updater.Models;
using System;
using System.IO;
using System.Threading;

namespace dRz.Updater.Services
{
    /// <summary>Временные данные проверенного и распакованного обновления.</summary>
    public sealed class PreparedUpdate
    {
        internal PreparedUpdate(
            UpdateRequest request,
            string tempDirectory,
            string sourceDirectory,
            UpdateInfo update,
            bool fullUpdate,
            FileStream sessionLock)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            TempDirectory = tempDirectory ?? throw new ArgumentNullException(nameof(tempDirectory));
            SourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
            Update = update ?? throw new ArgumentNullException(nameof(update));
            FullUpdate = fullUpdate;
            _sessionLock = sessionLock ?? throw new ArgumentNullException(nameof(sessionLock));
        }

        /// <summary>Исходный запрос обновления.</summary>
        public UpdateRequest Request { get; }

        /// <summary>Временный каталог, содержащий архив и распакованные файлы.</summary>
        public string TempDirectory { get; }

        /// <summary>Каталог с файлами, предназначенными для установки.</summary>
        public string SourceDirectory { get; }

        /// <summary>Метаданные проверенного обновления.</summary>
        public UpdateInfo Update { get; }

        /// <summary>Признак полного обновления.</summary>
        public bool FullUpdate { get; }

        /// <summary>
        /// Эксклюзивная блокировка каталога сессии. Удерживается до установки или отмены пакета.
        /// </summary>
        private FileStream? _sessionLock;

        /// <summary>Освобождает блокировку каталога сессии.</summary>
        internal void ReleaseSessionLock()
        {
            Interlocked.Exchange(ref _sessionLock, null)?.Dispose();
        }

        /// <summary>Сообщение об ошибке установки, если она завершилась неудачно.</summary>
        public string? ErrorMessage { get; internal set; }

        /// <summary>Сообщение об успешной установке.</summary>
        public string? ResultMessage { get; internal set; }
    }
}