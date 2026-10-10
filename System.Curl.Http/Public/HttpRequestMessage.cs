#nullable enable

using System;

namespace System.Curl.Http
{
    /// <summary>
    /// HTTP-запрос. Содержит метод, URI, заголовки, тело и параметры.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpRequestMessage</c>.
    /// </para>
    /// <para>
    /// Не потокобезопасен. Один экземпляр — один запрос.
    /// </para>
    /// </remarks>
    public class HttpRequestMessage : IDisposable
    {
        private HttpMethod _method;
        private bool _disposed;
        private HttpRequestHeaders? _headers;
        private HttpRequestOptions? _options;

        /// <summary>
        /// Создаёт запрос с методом GET и без URI.
        /// </summary>
        public HttpRequestMessage()
            : this(HttpMethod.Get, (Uri?)null)
        {
        }

        /// <summary>
        /// Создаёт запрос с указанным методом и URI.
        /// </summary>
        public HttpRequestMessage(HttpMethod method, string? requestUri)
            : this(method, requestUri == null ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
        {
        }

        /// <summary>
        /// Создаёт запрос с указанным методом и URI.
        /// </summary>
        public HttpRequestMessage(HttpMethod method, Uri? requestUri)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            RequestUri = requestUri;
        }

        // ================================================================
        //  Свойства
        // ================================================================

        /// <summary>
        /// HTTP-метод запроса.
        /// </summary>
        public HttpMethod Method
        {
            get => _method;
            set => _method = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// URI запроса. Может быть относительным (если задан <c>BaseAddress</c> у <see cref="HttpClient"/>).
        /// </summary>
        public Uri? RequestUri { get; set; }

        /// <summary>
        /// Тело запроса. <see langword="null"/> для GET/HEAD/DELETE без тела.
        /// </summary>
        public HttpContent? Content { get; set; }

        /// <summary>
        /// Заголовки запроса.
        /// </summary>
        public HttpRequestHeaders Headers => _headers ??= new HttpRequestHeaders();

        /// <summary>
        /// Параметры запроса. Используются расширениями для передачи
        /// дополнительных данных между слоями.
        /// </summary>
        public HttpRequestOptions Options => _options ??= new HttpRequestOptions();

        /// <summary>
        /// Версия HTTP. По умолчанию <c>1.1</c>.
        /// </summary>
        public Version Version { get; set; } = System.Net.HttpVersion.Version11;

        /// <summary>
        /// Политика выбора версии HTTP.
        /// </summary>
        public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;

        // ================================================================
        //  Освобождение
        // ================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождает ресурсы запроса. По умолчанию освобождает тело
        /// (<see cref="Content"/>), если оно есть.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Content?.Dispose();
                Content = null;
            }

            _disposed = true;
        }
    }
}