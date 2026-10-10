#nullable enable

using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// HTTP-клиент для отправки запросов и получения ответов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpClient</c>.
    /// </para>
    /// <para>
    /// Экземпляр можно использовать многократно. Для параллельных запросов
    /// один <see cref="HttpClient"/> потокобезопасен (транспорт — нет).
    /// </para>
    /// <para>
    /// После <see cref="Dispose"/> экземпляр непригоден к использованию.
    /// </para>
    /// </remarks>
    public class HttpClient : IDisposable
    {
        private const int DefaultTimeoutSeconds = 100;

        private readonly HttpMessageHandler _handler;
        private readonly bool _disposeHandler;
        private HttpRequestHeaders? _defaultRequestHeaders;
        private bool _disposed;

        /// <summary>
        /// Создаёт клиент с обработчиком по умолчанию
        /// (<see cref="HttpClientHandler"/>, внутри — libcurl).
        /// </summary>
        public HttpClient()
            : this(new HttpClientHandler(), disposeHandler: true)
        {
        }

        /// <summary>
        /// Создаёт клиент с указанным обработчиком.
        /// </summary>
        /// <param name="handler">Обработчик. Не может быть <see langword="null"/>.</param>
        public HttpClient(HttpMessageHandler handler)
            : this(handler, disposeHandler: true)
        {
        }

        /// <summary>
        /// Создаёт клиент с указанным обработчиком и решает, освобождать ли
        /// обработчик при <see cref="Dispose"/>.
        /// </summary>
        /// <param name="handler">Обработчик. Не может быть <see langword="null"/>.</param>
        /// <param name="disposeHandler">
        /// <see langword="true"/> — освобождать обработчик при <see cref="Dispose"/>.
        /// </param>
        public HttpClient(HttpMessageHandler handler, bool disposeHandler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _disposeHandler = disposeHandler;
            Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
            MaxResponseContentBufferSize = int.MaxValue;
        }

        // ================================================================
        //  Свойства
        // ================================================================

        /// <summary>
        /// Базовый адрес для относительных URI запросов.
        /// </summary>
        public Uri? BaseAddress { get; set; }

        /// <summary>
        /// Заголовки, добавляемые к каждому запросу.
        /// </summary>
        public HttpRequestHeaders DefaultRequestHeaders => _defaultRequestHeaders ??= new HttpRequestHeaders();

        /// <summary>
        /// Таймаут запроса. По умолчанию 100 секунд.
        /// </summary>
        /// <remarks>
        /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> отключает таймаут.
        /// </remarks>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Максимальный размер буфера тела ответа. По умолчанию <see cref="int.MaxValue"/>.
        /// </summary>
        public long MaxResponseContentBufferSize { get; set; }

        // ================================================================
        //  SendAsync
        // ================================================================

        /// <summary>
        /// Отправляет запрос.
        /// </summary>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
        }

        /// <summary>
        /// Отправляет запрос с поддержкой отмены.
        /// </summary>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        /// <summary>
        /// Отправляет запрос с указанной опцией завершения и поддержкой отмены.
        /// </summary>
        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Применяем BaseAddress к относительному URI.
            PrepareRequestMessage(request);

            // Применяем заголовки по умолчанию, если они не заданы в запросе.
            ApplyDefaultRequestHeaders(request);

            // Настраиваем таймаут через CancellationTokenSource.
            using var timeoutCts = CreateTimeoutTokenSource(cancellationToken);
            var linkedToken = timeoutCts?.Token ?? cancellationToken;

            try
            {
                var response = await _handler.SendAsyncInternal(request, linkedToken).ConfigureAwait(false);
                response.RequestMessage = request;

                // При ResponseContentRead буферизуем тело целиком.
                if (completionOption == HttpCompletionOption.ResponseContentRead && response.Content != null)
                {
                    await response.Content.LoadIntoBufferAsync(MaxResponseContentBufferSize).ConfigureAwait(false);
                }

                return response;
            }
            catch (OperationCanceledException) when (
                timeoutCts != null &&
                timeoutCts.IsCancellationRequested &&
                !cancellationToken.IsCancellationRequested)
            {
                // Отмена по таймауту, а не пользователем.
                throw new TaskCanceledException(
                    $"Запрос превысил таймаут {Timeout}.",
                    new TimeoutException());
            }
        }

        // ================================================================
        //  GetAsync
        // ================================================================

        /// <summary>GET-запрос.</summary>
        public Task<HttpResponseMessage> GetAsync(string? requestUri)
        {
            return GetAsync(CreateUri(requestUri));
        }

        /// <summary>GET-запрос.</summary>
        public Task<HttpResponseMessage> GetAsync(Uri? requestUri)
        {
            return GetAsync(requestUri, HttpCompletionOption.ResponseContentRead);
        }

        /// <summary>GET-запрос с указанной опцией завершения.</summary>
        public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(CreateUri(requestUri), completionOption);
        }

        /// <summary>GET-запрос с указанной опцией завершения.</summary>
        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption)
        {
            return GetAsync(requestUri, completionOption, CancellationToken.None);
        }

        /// <summary>GET-запрос с отменой.</summary>
        public Task<HttpResponseMessage> GetAsync(string? requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), cancellationToken);
        }

        /// <summary>GET-запрос с отменой.</summary>
        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            return GetAsync(requestUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        /// <summary>GET-запрос с опцией завершения и отменой.</summary>
        public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return GetAsync(CreateUri(requestUri), completionOption, cancellationToken);
        }

        /// <summary>GET-запрос с опцией завершения и отменой.</summary>
        public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
        }

        // ================================================================
        //  PostAsync
        // ================================================================

        /// <summary>POST-запрос.</summary>
        public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content)
        {
            return PostAsync(CreateUri(requestUri), content);
        }

        /// <summary>POST-запрос.</summary>
        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content)
        {
            return PostAsync(requestUri, content, CancellationToken.None);
        }

        /// <summary>POST-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            return PostAsync(CreateUri(requestUri), content, cancellationToken);
        }

        /// <summary>POST-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        // ================================================================
        //  PutAsync
        // ================================================================

        /// <summary>PUT-запрос.</summary>
        public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content)
        {
            return PutAsync(CreateUri(requestUri), content);
        }

        /// <summary>PUT-запрос.</summary>
        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content)
        {
            return PutAsync(requestUri, content, CancellationToken.None);
        }

        /// <summary>PUT-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            return PutAsync(CreateUri(requestUri), content, cancellationToken);
        }

        /// <summary>PUT-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        // ================================================================
        //  DeleteAsync
        // ================================================================

        /// <summary>DELETE-запрос.</summary>
        public Task<HttpResponseMessage> DeleteAsync(string? requestUri)
        {
            return DeleteAsync(CreateUri(requestUri));
        }

        /// <summary>DELETE-запрос.</summary>
        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri)
        {
            return DeleteAsync(requestUri, CancellationToken.None);
        }

        /// <summary>DELETE-запрос с отменой.</summary>
        public Task<HttpResponseMessage> DeleteAsync(string? requestUri, CancellationToken cancellationToken)
        {
            return DeleteAsync(CreateUri(requestUri), cancellationToken);
        }

        /// <summary>DELETE-запрос с отменой.</summary>
        public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        // ================================================================
        //  PatchAsync
        // ================================================================

        /// <summary>PATCH-запрос.</summary>
        public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content)
        {
            return PatchAsync(CreateUri(requestUri), content);
        }

        /// <summary>PATCH-запрос.</summary>
        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content)
        {
            return PatchAsync(requestUri, content, CancellationToken.None);
        }

        /// <summary>PATCH-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            return PatchAsync(CreateUri(requestUri), content, cancellationToken);
        }

        /// <summary>PATCH-запрос с отменой.</summary>
        public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content };
            return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        // ================================================================
        //  GetStringAsync / GetByteArrayAsync / GetStreamAsync
        // ================================================================

        /// <summary>GET-запрос, возвращает тело как строку.</summary>
        public async Task<string> GetStringAsync(string? requestUri)
        {
            using var response = await GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;
        }

        /// <summary>GET-запрос, возвращает тело как строку.</summary>
        public async Task<string> GetStringAsync(Uri? requestUri)
        {
            using var response = await GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;
        }

        /// <summary>GET-запрос, возвращает тело как массив байт.</summary>
        public async Task<byte[]> GetByteArrayAsync(string? requestUri)
        {
            using var response = await GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)
                : Array.Empty<byte>();
        }

        /// <summary>GET-запрос, возвращает тело как массив байт.</summary>
        public async Task<byte[]> GetByteArrayAsync(Uri? requestUri)
        {
            using var response = await GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)
                : Array.Empty<byte>();
        }

        /// <summary>GET-запрос, возвращает тело как поток.</summary>
        public async Task<System.IO.Stream> GetStreamAsync(string? requestUri)
        {
            var response = await GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
                : System.IO.Stream.Null;
        }

        /// <summary>GET-запрос, возвращает тело как поток.</summary>
        public async Task<System.IO.Stream> GetStreamAsync(Uri? requestUri)
        {
            var response = await GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content != null
                ? await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
                : System.IO.Stream.Null;
        }

        // ================================================================
        //  CancelPendingRequests / Dispose
        // ================================================================

        /// <summary>
        /// Отменяет все незавершённые запросы.
        /// </summary>
        /// <remarks>
        /// В текущей реализации no-op: каждый запрос имеет свой
        /// <see cref="CancellationToken"/>, и отмена конкретного запроса
        /// делается через него.
        /// </remarks>
        public void CancelPendingRequests()
        {
            // TODO: реализовать, если понадобится. Требует отслеживания
            // активных CancellationTokenSource на уровне клиента.
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождает клиент и, если <c>disposeHandler == true</c>, обработчик.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing && _disposeHandler)
            {
                _handler.Dispose();
            }

            _disposed = true;
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private void PrepareRequestMessage(HttpRequestMessage request)
        {
            if (request.RequestUri == null)
            {
                if (BaseAddress == null)
                {
                    throw new InvalidOperationException(
                        "URI запроса не задан, и BaseAddress не установлен.");
                }

                // Оставляем RequestUri == null: транспорт получит BaseAddress отдельно.
                // (На практике HttpClient требует URI в запросе; здесь
                // мы просто полагаемся на то, что пользователь передаст
                // абсолютный URI или BaseAddress.)
                return;
            }

            if (BaseAddress != null && !request.RequestUri.IsAbsoluteUri)
            {
                request.RequestUri = new Uri(BaseAddress, request.RequestUri);
            }
        }

        private void ApplyDefaultRequestHeaders(HttpRequestMessage request)
        {
            if (_defaultRequestHeaders == null)
            {
                return;
            }

            foreach (var header in _defaultRequestHeaders)
            {
                if (!request.Headers.Contains(header.Key))
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
        }

        private CancellationTokenSource? CreateTimeoutTokenSource(CancellationToken cancellationToken)
        {
            if (Timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                return null;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);
            return cts;
        }

        private Uri? CreateUri(string? requestUri)
        {
            if (string.IsNullOrEmpty(requestUri))
            {
                return null;
            }

            return new Uri(requestUri, UriKind.RelativeOrAbsolute);
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