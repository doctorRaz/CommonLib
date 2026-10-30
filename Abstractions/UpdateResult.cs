namespace dRz.Abstractions.Updater
{
    /// <summary>Результат проверки и установки обновления.</summary>
    public enum UpdateResult
    {
        /// <summary>Обновление успешно установлено.</summary>
        Installed,

        /// <summary>Обновление не требуется.</summary>
        UpToDate,

        /// <summary>Пользователь отказался от необязательного обновления.</summary>
        Declined,

        /// <summary>Установка необязательного обновления отключена настройками.</summary>
        Disabled,

        /// <summary>Обновление невозможно из-за сетевого расположения аддона.</summary>
        NetworkPath,

        /// <summary>Проверка или установка завершилась ошибкой.</summary>
        Failed,

        /// <summary>Операция обновления отменена через CancellationToken.</summary>
        Cancelled
    }
}