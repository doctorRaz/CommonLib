using System.IO;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Удаляет Mark of the Web у загруженного файла.
    /// </summary>
    public static class MarkOfTheWebRemover
    {
        /// <summary>
        /// Удаляет поток NTFS Zone.Identifier, если он существует.
        /// </summary>
        public static void Remove(string filePath)
        {
            string zoneIdentifier = filePath + ":Zone.Identifier";

            try
            {
                if (File.Exists(zoneIdentifier))
                {
                    File.Delete(zoneIdentifier);
                }
            }
            catch (IOException)
            {
                // Отсутствие возможности удалить MoTW не должно
                // маскироваться под успешную установку.
                throw;
            }
        }
    }
}