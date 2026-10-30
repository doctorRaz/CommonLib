using System;
using System.Collections.Generic;
using System.IO;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Устанавливает файлы обновления с резервированием существующих файлов и откатом при ошибке.
    /// </summary>
    public static class Installer
    {
        private sealed class FileChange
        {
            public FileChange(string target, string? backup)
            {
                Target = target;
                Backup = backup;
            }

            public string Target { get; }
            public string? Backup { get; }
            public bool Installed { get; set; }
        }

        /// <summary>
        /// Перемещает файл в целевой каталог, предварительно сохраняя существующий файл в резервной копии.
        /// </summary>
        /// <param name="sourceFile">Полный путь к существующему исходному файлу.</param>
        /// <param name="targetDirectory">Каталог, в который перемещается файл; при необходимости создаётся.</param>
        /// <exception cref="FileNotFoundException">Исходный файл не найден.</exception>
        /// <exception cref="IOException">Не удалось переместить исходный или целевой файл.</exception>
        /// <exception cref="UnauthorizedAccessException">Недостаточно прав для изменения файлов или каталогов.</exception>
        public static void MoveWithBackup(string sourceFile, string targetDirectory)
        {
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException("Файл не найден", sourceFile);
            }

            Directory.CreateDirectory(targetDirectory);

            string targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));

            if (File.Exists(targetFile))
            {
                string backupFile = GetBackupName(targetFile);
                File.Move(targetFile, backupFile);
            }

            File.Move(sourceFile, targetFile);
        }

        /// <summary>Возвращает уникальное имя резервной копии, которое ещё не занято.</summary>
        /// <param name="file">Полный путь к исходному файлу.</param>
        /// <returns>Полный путь к свободному имени резервной копии.</returns>
        private static string GetBackupName(string file)
        {
            string directory = Path.GetDirectoryName(file)!;
            string name = Path.GetFileName(file);

            string backup = Path.Combine(directory, name + ".bak");
            if (!File.Exists(backup))
            {
                return backup;
            }

            for (int i = 1; ; i++)
            {
                backup = Path.Combine(directory, $"{name}({i}).bak");
                if (!File.Exists(backup))
                {
                    return backup;
                }
            }
        }

        /// <summary>
        /// Переименовывает все файлы каталога в резервные копии, сохраняя структуру каталогов.
        /// При ошибке пытается вернуть уже переименованные файлы исходным именам.
        /// </summary>
        /// <param name="directory">Каталог установленного пакета или модуля.</param>
        /// <returns>
        /// <see langword="true"/>, если файлы успешно переименованы;
        /// <see langword="false"/>, если каталог не существует или операция завершилась ошибкой.
        /// При ошибке выполняется попытка вернуть уже переименованные файлы.
        /// </returns>
        /// <exception cref="IOException">Не удалось получить список файлов каталога.</exception>
        /// <exception cref="UnauthorizedAccessException">Недостаточно прав для чтения каталога.</exception>
        public static bool RenameDirectoryFilesWithBackup(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return false;
            }

            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            List<(string Original, string Backup)> renamedFiles = new();

            try
            {
                foreach (string file in files)
                {
                    string backup = GetBackupName(file);
                    File.Move(file, backup);
                    renamedFiles.Add((file, backup));
                }

                return true;
            }
            catch
            {
                for (int i = renamedFiles.Count - 1; i >= 0; i--)
                {
                    (string original, string backup) = renamedFiles[i];

                    try
                    {
                        if (File.Exists(backup) && !File.Exists(original))
                        {
                            File.Move(backup, original);
                        }
                    }
                    catch
                    {
                        // Сохраняем исходную ошибку; неудачный откат требует диагностики.
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Переносит файлы из распакованного обновления с резервированием заменяемых файлов.
        /// При ошибке откатывает все изменения, выполненные в рамках этой установки.
        /// Если откат не удаётся, выбрасывает <see cref="UpdateRollbackException"/>.
        /// </summary>
        /// <param name="sourceDirectory">Каталог распакованного обновления.</param>
        /// <param name="targetDirectory">Каталог установленного пакета или модуля.</param>
        /// <returns>
        /// <see langword="true"/>, если все файлы перенесены;
        /// <see langword="false"/>, если исходный каталог не существует.
        /// </returns>
        /// <exception cref="IOException">Не удалось перенести файл. После успешного отката исходная ошибка повторно выбрасывается.</exception>
        /// <exception cref="UpdateRollbackException">Установка и откат завершились ошибками.</exception>
        /// <exception cref="InvalidDataException">Относительный путь файла выходит за пределы целевого каталога.</exception>
        /// <exception cref="UnauthorizedAccessException">Недостаточно прав для изменения файлов или каталогов.</exception>
        public static bool MoveDirectoryFilesWithBackup(
            string sourceDirectory,
            string targetDirectory)
        {
            return MoveDirectoryFilesWithBackup(sourceDirectory, targetDirectory, false);
        }

        /// <summary>
        /// Переносит файлы обновления как одну операцию с возможностью отката.
        /// При полном обновлении сначала резервируются все старые файлы, в том числе отсутствующие
        /// в новой версии. При ошибке установки изменения откатываются в обратном порядке.
        /// </summary>
        /// <param name="sourceDirectory">Каталог распакованного обновления.</param>
        /// <param name="targetDirectory">Каталог установленного пакета или модуля.</param>
        /// <param name="fullUpdate">Если <see langword="true"/>, перед установкой резервируются все старые файлы целевого каталога.</param>
        /// <returns>
        /// <see langword="true"/>, если все файлы перенесены;
        /// <see langword="false"/>, если исходный каталог не существует.
        /// </returns>
        /// <exception cref="IOException">Не удалось перенести файл. После успешного отката исходная ошибка повторно выбрасывается.</exception>
        /// <exception cref="UpdateRollbackException">Установка и откат завершились ошибками.</exception>
        /// <exception cref="InvalidDataException">Относительный путь файла выходит за пределы целевого каталога.</exception>
        /// <exception cref="UnauthorizedAccessException">Недостаточно прав для изменения файлов или каталогов.</exception>
        public static bool MoveDirectoryFilesWithBackup(
            string sourceDirectory,
            string targetDirectory,
            bool fullUpdate)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                return false;
            }

            string[] sourceFiles = Directory.GetFiles(
                sourceDirectory,
                "*",
                SearchOption.AllDirectories);

            List<FileChange> changes = new();

            try
            {
                if (fullUpdate && Directory.Exists(targetDirectory))
                {
                    // Сначала резервируем весь установленный набор файлов.
                    string[] targetFiles = Directory.GetFiles(
                        targetDirectory,
                        "*",
                        SearchOption.AllDirectories);

                    foreach (string targetFile in targetFiles)
                    {
                        string backup = GetBackupName(targetFile);
                        File.Move(targetFile, backup);
                        changes.Add(new FileChange(targetFile, backup));
                    }
                }

                foreach (string sourceFile in sourceFiles)
                {
                    string relative = Path.GetRelativePath(sourceDirectory, sourceFile);
                    string target = GetSafeTargetPath(targetDirectory, relative);
                    string targetParent = Path.GetDirectoryName(target)!;

                    Directory.CreateDirectory(targetParent);

                    string? backup = null;

                    if (File.Exists(target))
                    {
                        if (fullUpdate)
                        {
                            throw new IOException(
                                $"Целевой файл уже существует после резервирования: {target}");
                        }

                        backup = GetBackupName(target);
                        File.Move(target, backup);
                    }

                    FileChange change = new(target, backup);
                    changes.Add(change);

                    File.Move(sourceFile, target);
                    change.Installed = true;
                }
            }
            catch (Exception installException)
            {
                // Откатываем в обратном порядке: сначала новые файлы, затем старые.
                // При первой ошибке дальнейшие действия прекращаются.
                for (int i = changes.Count - 1; i >= 0; i--)
                {
                    FileChange change = changes[i];

                    try
                    {
                        if (change.Installed && File.Exists(change.Target))
                        {
                            File.Delete(change.Target);
                        }

                        if (change.Backup is not null && File.Exists(change.Backup))
                        {
                            if (File.Exists(change.Target))
                            {
                                throw new IOException(
                                    $"Нельзя восстановить резервную копию: целевой файл существует: {change.Target}");
                            }

                            File.Move(change.Backup, change.Target);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        throw new UpdateRollbackException(
                            installException,
                            new IOException(
                                $"Не удалось откатить изменение файла '{change.Target}'. Резервная копия: '{change.Backup ?? "(нет)"}'.",
                                rollbackException));
                    }
                }

                // Откат завершён успешно — сохраняем исходную ошибку установки.
                throw;
            }

            // Исходный каталог находится во временной директории UpdateManager и будет удалён
            // в его finally. Не считаем очистку временных файлов частью установки.
            return true;
        }

        /// <summary>
        /// Проверяет, что относительный путь не выходит за пределы каталога установки.
        /// </summary>
        private static string GetSafeTargetPath(string targetDirectory, string relativePath)
        {
            string root = Path.GetFullPath(targetDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            string target = Path.GetFullPath(Path.Combine(root, relativePath));

            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Архив содержит путь за пределами каталога установки.");
            }

            return target;
        }
    }
}