using System;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Параметры искусственного замедления этапов обновления для локального тестирования.
    /// Все задержки доступны только в DEBUG-сборке и задаются переменными окружения
    /// процесса nanoCAD. В Release оба значения всегда равны нулю.
    ///
    /// Инструкция для ручного тестирования:
    /// 1. Создай BAT-файл и перед запуском nanoCAD укажи нужные переменные:
    ///
    ///    @echo off
    ///    set "DRZ_UPDATER_TEST_DOWNLOAD_DELAY_MS=500"
    ///    set "DRZ_UPDATER_TEST_EXTRACTION_DELAY_MS=0"
    ///    start "" "C:\Path\To\nanoCAD.exe"
    ///
    /// 2. Замени путь на фактический путь к nanoCAD и запускай CAD только этим BAT-файлом.
    ///    Уже запущенный процесс не получит новые переменные окружения.
    /// 3. Для проверки скачивания закрой nanoCAD, когда в логе появится сообщение
    ///    о DEBUG-задержке скачивания. Пауза задаётся после каждого блока размером 16 КиБ.
    /// 4. Для проверки распаковки установи DOWNLOAD_DELAY_MS=0 и, например,
    ///    EXTRACTION_DELAY_MS=60000. Закрой nanoCAD после сообщения о задержке распаковки.
    ///    Terminate должен вернуться, не дожидаясь паузы. Если процесс nanoCAD остаётся
    ///    запущенным, задача продолжит работу, обнаружит отмену после распаковки и удалит
    ///    временный каталог. При полном завершении процесса задача прекратится вместе с ним;
    ///    оставшийся каталог будет удалён при следующем проходе очистки.
    /// 5. Чтобы отключить тестирование, установи обе переменные в 0 или удали их.
    ///    Изменение BAT-файла действует только на новые процессы nanoCAD.
    /// </summary>
    internal static class UpdaterTestSettings
    {
        /// <summary>
        /// Задержка после каждого блока скачиваемого архива, миллисекунды.
        /// Переменная окружения: DRZ_UPDATER_TEST_DOWNLOAD_DELAY_MS.
        /// </summary>
        public static int DownloadDelayBetweenChunksMs =>
#if DEBUG
            ReadDelay("DRZ_UPDATER_TEST_DOWNLOAD_DELAY_MS");
#else
            0;
#endif

        /// <summary>
        /// Задержка перед вызовом SharpSevenZip, миллисекунды.
        /// Переменная окружения: DRZ_UPDATER_TEST_EXTRACTION_DELAY_MS.
        /// </summary>
        public static int ExtractionDelayBeforeStartMs =>
#if DEBUG
            ReadDelay("DRZ_UPDATER_TEST_EXTRACTION_DELAY_MS");
#else
            0;
#endif

#if DEBUG
        /// <summary>
        /// Читает тестовую задержку из окружения. Некорректные, отрицательные
        /// и слишком большие значения безопасно заменяются на 0 или ограничиваются.
        /// Верхний предел 10 минут защищает от случайного бесконечно долгого теста.
        /// </summary>
        private static int ReadDelay(string variableName)
        {
            string? value = Environment.GetEnvironmentVariable(variableName);

            if (!int.TryParse(value, out int delayMs) || delayMs <= 0)
            {
                return 0;
            }

            return Math.Min(delayMs, 10 * 60 * 1000);
        }
#endif
    }
}