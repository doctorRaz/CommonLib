using System;
using System.Text.Json.Serialization;

namespace dRz.Updater.Models
{
    /// <summary>
    /// Описание доступного обновления из update.json.
    /// </summary>
    public sealed class UpdateInfo
    {
        /// <summary>Имя продукта и корневого каталога продукта внутри архива.</summary>
        [JsonPropertyName("product")]
        public string Product { get; init; } = string.Empty;

        /// <summary>Версия опубликованного обновления из четырёх компонентов.</summary>
        [JsonPropertyName("version")]
        public VersionInfo Version { get; init; } = new();

        /// <summary>Тег GitHub-релиза.</summary>
        [JsonPropertyName("tag")]
        public string Tag { get; init; } = string.Empty;

        /// <summary>Имя обычного архива обновления.</summary>
        [JsonPropertyName("asset")]
        public string Asset { get; init; } = string.Empty;

        /// <summary>SHA-256 обычного архива.</summary>
        [JsonPropertyName("sha256")]
        public string Sha256 { get; init; } = string.Empty;

        /// <summary>Размер обычного архива в байтах.</summary>
        [JsonPropertyName("size")]
        public long Size { get; init; }

        /// <summary>Идентификатор коммита, из которого собран релиз.</summary>
        [JsonPropertyName("commit")]
        public string Commit { get; init; } = string.Empty;

        /// <summary>Признак обязательного обновления, устанавливаемого без подтверждения.</summary>
        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; init; }

        /// <summary>Минимальная версия для обычного перехода; объект должен содержать четыре компонента.</summary>
        [JsonPropertyName("minimumVersion")]
        public VersionInfo MinimumVersion { get; init; } = new();

        /// <summary>Время публикации релиза с часовым смещением.</summary>
        [JsonPropertyName("publishedAt")]
        public DateTimeOffset PublishedAt { get; init; }

        /// <summary>Метаданные защищённого архива; если заданы, используются вместо верхнеуровневых полей архива.</summary>
        [JsonPropertyName("protected")]
        public ProtectedPackageInfo? Protected { get; init; }
    }
}