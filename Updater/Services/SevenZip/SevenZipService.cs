using SharpSevenZip;
using System;
using System.IO;

namespace dRz.Updater.Services.SevenZip
{
    /// <summary>
    /// Сервис распаковки архивов 7z.
    /// Поддерживает обычные и защищённые паролем архивы,
    /// включая архивы с зашифрованными заголовками.
    /// </summary>
    public sealed class SevenZipService
    {
        /// <summary>
        /// Инициализирует сервис для работы с архивами 7z.
        /// </summary>
        public SevenZipService()
        {
            // Каталог сборки Updater, а не основного аддона.
            string updaterDirectory = Path.GetDirectoryName(
                                    typeof(SevenZipService).Assembly.Location)
                                    ?? throw new InvalidOperationException(
                                        "Не удалось определить каталог Updater.");

            // SharpSevenZip самостоятельно выберет x86 или x64
            // в зависимости от разрядности процесса.
            string libraryDirectory = Path.Combine(
                                    updaterDirectory,
                                    Environment.Is64BitProcess ? "x64" : "x86");

            string libraryPath = Path.Combine(libraryDirectory, "7z.dll");

            if (!File.Exists(libraryPath))
            {
                throw new FileNotFoundException("Не найдена библиотека 7-Zip.", libraryPath);
            }

            _ = SharpSevenZipBase.TrySetLibraryPath(libraryPath);
        }

        /// <summary>
        /// Распаковывает архив 7z в указанный каталог.
        /// </summary>
        /// <param name="archivePath">Путь к архиву.</param>
        /// <param name="destination">
        /// Каталог, в который будут распакованы файлы.
        /// Если каталог не существует, он будет создан.
        /// </param>
        /// <param name="password">
        /// Пароль архива. Если <see langword="null"/> или пустая строка,
        /// считается, что архив не защищён паролем.
        /// </param>
        /// <exception cref="FileNotFoundException">
        /// Архив не найден.
        /// </exception>
        public void Extract(
            string archivePath,
            string destination,
            string? password = null)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException(
                    "Архив не найден",
                    archivePath);
            }

            Directory.CreateDirectory(destination);

            // SharpSevenZip передаёт пароль при открытии архива и поддерживает
            // чтение зашифрованных заголовков (-mhe=on).
            using SharpSevenZipExtractor archive = new SharpSevenZipExtractor(
                archivePath,
                password!);

            archive.ExtractArchive(destination);
        }
    }
}