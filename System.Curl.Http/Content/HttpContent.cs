#nullable enable

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// Базовый класс для тела HTTP-сообщения (запроса или ответа).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpContent</c>.
    /// </para>
    /// <para>
    /// Наследники должны переопределить <see cref="SerializeToStreamAsync(Stream, TransportContext)"/>,
    /// чтобы записать тело в поток. Опционально можно переопределить
    /// <see cref="TryComputeLength"/> и <see cref="CreateContentReadStreamAsync(CancellationToken)"/>.
    /// </para>
    /// <para>
    /// Тело может быть прочитано только один раз. После первого чтения
    /// результат кэшируется в памяти. Для повторного чтения используйте
    /// <see cref="LoadIntoBufferAsync()"/> до первого чтения.
    /// </para>
    /// </remarks>
    public abstract class HttpContent : IDisposable
    {
        private const int DefaultBufferSize = 81920;

        private MemoryStream? _bufferedContent;
        private bool _disposed;
        private HttpContentHeaders? _headers;

        /// <summary>
        /// Заголовки тела сообщения.
        /// </summary>
        public HttpContentHeaders Headers => _headers ??= new HttpContentHeaders();

        /// <summary>
        /// Освобождает ресурсы, занятые телом.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождает ресурсы. Переопределяется в наследниках, владеющих
        /// неуправляемыми или потоковыми ресурсами.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _bufferedContent?.Dispose();
                _bufferedContent = null;
            }

            _disposed = true;
        }

        // ================================================================
        //  Чтение тела
        // ================================================================

        /// <summary>
        /// Читает тело как массив байт.
        /// </summary>
        public Task<byte[]> ReadAsByteArrayAsync()
        {
            return ReadAsByteArrayAsync(CancellationToken.None);
        }

        /// <summary>
        /// Читает тело как массив байт с поддержкой отмены.
        /// </summary>
        public async Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);
            return _bufferedContent!.ToArray();
        }

        /// <summary>
        /// Читает тело как строку. Кодировка берётся из <c>Content-Type</c>,
        /// если указан <c>charset</c>; иначе — UTF-8.
        /// </summary>
        public Task<string> ReadAsStringAsync()
        {
            return ReadAsStringAsync(CancellationToken.None);
        }

        /// <summary>
        /// Читает тело как строку с поддержкой отмены.
        /// </summary>
        public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);

            var encoding = GetEncodingFromContentType();
            var bytes = _bufferedContent!.ToArray();
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Возвращает тело как поток для чтения.
        /// </summary>
        public Task<Stream> ReadAsStreamAsync()
        {
            return ReadAsStreamAsync(CancellationToken.None);
        }

        /// <summary>
        /// Возвращает тело как поток для чтения с поддержкой отмены.
        /// </summary>
        public async Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);
            return new MemoryStream(_bufferedContent!.ToArray(), writable: false);
        }

        // ================================================================
        //  Копирование и буферизация
        // ================================================================

        /// <summary>
        /// Копирует тело в указанный поток.
        /// </summary>
        public Task CopyToAsync(Stream stream)
        {
            return CopyToAsync(stream, CancellationToken.None);
        }

        /// <summary>
        /// Копирует тело в указанный поток с поддержкой отмены.
        /// </summary>
        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            return CopyToAsync(stream, null, cancellationToken);
        }

        /// <summary>
        /// Копирует тело в указанный поток с транспортным контекстом.
        /// </summary>
        public Task CopyToAsync(Stream stream, TransportContext? context)
        {
            return CopyToAsync(stream, context, CancellationToken.None);
        }

        /// <summary>
        /// Копирует тело в указанный поток с транспортным контекстом и отменой.
        /// </summary>
        public async Task CopyToAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ThrowIfDisposed();

            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);

            var bytes = _bufferedContent!.ToArray();
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Загружает тело в буфер в памяти.
        /// </summary>
        public Task LoadIntoBufferAsync()
        {
            return LoadIntoBufferAsync(DefaultBufferSize);
        }

        /// <summary>
        /// Загружает тело в буфер в памяти с указанным максимальным размером.
        /// </summary>
        public async Task LoadIntoBufferAsync(long maxBufferSize)
        {
            ThrowIfDisposed();
            await LoadIntoBufferAsyncInternal(CancellationToken.None).ConfigureAwait(false);

            if (_bufferedContent!.Length > maxBufferSize)
            {
                throw new HttpRequestException(
                    $"Размер тела ({_bufferedContent.Length} байт) превышает допустимый максимум ({maxBufferSize} байт).");
            }
        }

        // ================================================================
        //  Переопределяемые методы
        // ================================================================

        /// <summary>
        /// Записывает тело в поток. Обязателен к переопределению в наследниках.
        /// </summary>
        /// <param name="stream">Целевой поток.</param>
        /// <param name="context">Транспортный контекст или <see langword="null"/>.</param>
        protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext? context);

        /// <summary>
        /// Записывает тело в поток с поддержкой отмены. По умолчанию вызывает
        /// <see cref="SerializeToStreamAsync(Stream, TransportContext)"/> и проверяет
        /// отмену до и после. Наследники могут переопределить, если поддерживают
        /// отмену внутри сериализации.
        /// </summary>
        protected virtual async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SerializeToStreamAsync(stream, context).ConfigureAwait(false);
        }

        /// <summary>
        /// Пытается вычислить длину тела в байтах. По умолчанию возвращает
        /// <see langword="false"/> (длина неизвестна).
        /// </summary>
        protected virtual bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        /// <summary>
        /// Создаёт поток для чтения тела без буферизации. По умолчанию
        /// буферизует и возвращает <see cref="MemoryStream"/>.
        /// </summary>
        protected virtual Task<Stream> CreateContentReadStreamAsync()
        {
            return CreateContentReadStreamAsync(CancellationToken.None);
        }

        /// <summary>
        /// Создаёт поток для чтения тела без буферизации с поддержкой отмены.
        /// </summary>
        protected virtual async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);
            return new MemoryStream(_bufferedContent!.ToArray(), writable: false);
        }

        // ================================================================
        //  Внутренний API для транспорта
        // ================================================================

        /// <summary>
        /// Сериализует тело в массив байт. Используется транспортом для
        /// отправки тела запроса.
        /// </summary>
        internal async Task<byte[]> ReadAsByteArrayInternalAsync(CancellationToken cancellationToken)
        {
            await LoadIntoBufferAsyncInternal(cancellationToken).ConfigureAwait(false);
            return _bufferedContent!.ToArray();
        }

        /// <summary>
        /// Возвращает длину тела, если её удалось вычислить; иначе — <see langword="null"/>.
        /// </summary>
        internal long? GetContentLength()
        {
            if (_bufferedContent != null)
            {
                return _bufferedContent.Length;
            }

            return TryComputeLength(out var length) ? length : (long?)null;
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private async Task LoadIntoBufferAsyncInternal(CancellationToken cancellationToken)
        {
            if (_bufferedContent != null)
            {
                return;
            }

            var buffer = new MemoryStream();
            await SerializeToStreamAsync(buffer, null, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            _bufferedContent = buffer;
        }

        private Encoding GetEncodingFromContentType()
        {
            var contentType = Headers.ContentType;
            if (contentType?.CharSet != null)
            {
                var charset = contentType.CharSet.Trim('"', '\'');
                if (!string.IsNullOrEmpty(charset))
                {
                    try
                    {
                        return Encoding.GetEncoding(charset);
                    }
                    catch (ArgumentException)
                    {
                        // Неизвестная кодировка — используем UTF-8.
                    }
                }
            }

            return Encoding.UTF8;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}