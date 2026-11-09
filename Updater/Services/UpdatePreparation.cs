using System;
using dRz.Abstractions.Updater;

namespace dRz.Updater.Services
{
    /// <summary>Результат асинхронной подготовки обновления.</summary>
    public sealed class UpdatePreparation
    {
        private UpdatePreparation(UpdateResult? result, PreparedUpdate? preparedUpdate)
        {
            if (result.HasValue == (preparedUpdate is not null))
            {
                throw new ArgumentException(
                    "Результат подготовки должен содержать либо результат проверки, либо подготовленный пакет.");
            }

            Result = result;
            PreparedUpdate = preparedUpdate;
        }

        /// <summary>Результат проверки, если подготовленный пакет отсутствует.</summary>
        public UpdateResult? Result { get; }

        /// <summary>Подготовленный пакет, ожидающий синхронной установки.</summary>
        public PreparedUpdate? PreparedUpdate { get; }

        /// <summary>Признак успешной подготовки пакета, ожидающего установки.</summary>
        public bool IsReady => PreparedUpdate is not null;

        /// <summary>Создаёт результат завершённой проверки без подготовленного пакета.</summary>
        public static UpdatePreparation Completed(UpdateResult result) => new(result, null);

        /// <summary>Создаёт результат успешной подготовки пакета без результата установки.</summary>
        public static UpdatePreparation Ready(PreparedUpdate preparedUpdate) =>
            new(null, preparedUpdate);
    }
}