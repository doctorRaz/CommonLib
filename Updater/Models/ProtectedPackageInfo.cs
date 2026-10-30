using System.Text.Json.Serialization;

namespace dRz.Updater.Models
{
    /// <summary>
    /// Описание защищённого пакета обновления.
    /// </summary>
    public sealed class ProtectedPackageInfo
    {
        /// <summary>Имя защищённого архива.</summary>
        [JsonPropertyName("asset")]
        public string Asset { get; init; } = string.Empty;

        /// <summary>SHA-256 защищённого архива.</summary>
        [JsonPropertyName("sha256")]
        public string Sha256 { get; init; } = string.Empty;

        /// <summary>Размер защищённого архива в байтах.</summary>
        [JsonPropertyName("size")]
        public long Size { get; init; }

        /// <summary>Формат архива, например <c>7z</c>.</summary>
        [JsonPropertyName("format")]
        public string Format { get; init; } = string.Empty;

        /// <summary>Обозначение алгоритма шифрования содержимого архива.</summary>
        [JsonPropertyName("encryption")]
        public string Encryption { get; init; } = string.Empty;

        /// <summary>Признак шифрования заголовков архива.</summary>
        [JsonPropertyName("encryptedHeaders")]
        public bool EncryptedHeaders { get; init; }

        /// <summary>Пароль архива. В публичном manifest это значение доступно получателям и не является секретом.</summary>
        [JsonPropertyName("password")]
        public string Password { get; init; } = string.Empty;
    }
}