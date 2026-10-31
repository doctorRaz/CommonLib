using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Загружает файлы обновления по HTTP(S).
    /// </summary>
    public sealed class Downloader
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Создаёт загрузчик.
        /// </summary>
        public Downloader(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Загружает файл по указанному URL.
        /// </summary>
        public async Task DownloadAsync(
            string url,
            string destinationFile,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Некорректный URL загрузки.", nameof(url));
            }

            string? directory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            await using Stream source =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            await using FileStream target = new(
                destinationFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await source.CopyToAsync(target, cancellationToken);
        }
    }
}