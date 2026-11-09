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
        /// <param name="url">URL загружаемого файла.</param>
        /// <param name="destinationFile">Путь, по которому будет сохранён файл.</param>
        /// <param name="cancellationToken">Токен отмены загрузки.</param>
        /// <param name="delayBetweenChunksMs">
        /// DEBUG-тест: искусственная задержка после записи каждого блока данных.
        /// Нулевое значение отключает замедление. В Release вызывающий код передаёт 0.
        /// </param>
        public async Task DownloadAsync(
            string url,
            string destinationFile,
            CancellationToken cancellationToken = default,
            int delayBetweenChunksMs = 0)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Некорректный URL загрузки.", nameof(url));
            }

            if (delayBetweenChunksMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayBetweenChunksMs));
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

            if (delayBetweenChunksMs == 0)
            {
                // Обычный путь без тестового замедления: используем штатное копирование потока.
                await source.CopyToAsync(target, cancellationToken);
                return;
            }

            // Тестовый путь: уменьшаем размер блока, чтобы задержка была заметна
            // даже на небольших локальных архивах. Сама загрузка остаётся реальной:
            // каждый блок читается из HTTP-потока и записывается в файл.
            byte[] buffer = new byte[16 * 1024];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int bytesRead = await source.ReadAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await target.WriteAsync(
                    buffer,
                    0,
                    bytesRead,
                    cancellationToken);

                // Пауза находится внутри загрузки, между блоками, а не до/после неё.
                // Task.Delay принимает CancellationToken, поэтому Terminate может
                // прервать тестовую задержку так же, как и обычную сетевую операцию.
                await Task.Delay(delayBetweenChunksMs, cancellationToken);
            }

            await target.FlushAsync(cancellationToken);
        }
    }
}