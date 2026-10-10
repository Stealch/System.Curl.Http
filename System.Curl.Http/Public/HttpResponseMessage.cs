#nullable enable

using System;

namespace System.Curl.Http
{
    /// <summary>
    /// HTTP-ответ. Содержит статус, заголовки, тело и ссылку на исходный запрос.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpResponseMessage</c>.
    /// </para>
    /// <para>
    /// Свойство <see cref="TrailingHeaders"/> добавлено для совместимости с .NET 5+:
    /// в .NET Framework 4.8 его нет, но trailing-заголовки (chunked trailers)
    /// поддерживаются и приходят после тела.
    /// </para>
    /// </remarks>
    public class HttpResponseMessage : IDisposable
    {
        private HttpStatusCode _statusCode;
        private string? _reasonPhrase;
        private HttpContent? _content;
        private bool _disposed;
        private HttpResponseHeaders? _headers;
        private HttpResponseHeaders? _trailingHeaders;

        /// <summary>
        /// Создаёт ответ со статусом 200 OK.
        /// </summary>
        public HttpResponseMessage()
            : this(HttpStatusCode.OK)
        {
        }

        /// <summary>
        /// Создаёт ответ с указанным статус-кодом.
        /// </summary>
        public HttpResponseMessage(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        // ================================================================
        //  Свойства
        // ================================================================

        /// <summary>
        /// HTTP-код состояния ответа.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get => _statusCode;
            set => _statusCode = value;
        }

        /// <summary>
        /// Причина статус-кода (например, <c>"OK"</c> для 200). Может быть <see langword="null"/>.
        /// </summary>
        public string? ReasonPhrase
        {
            get => _reasonPhrase;
            set => _reasonPhrase = value;
        }

        /// <summary>
        /// Версия HTTP, использованная в ответе. По умолчанию <c>1.1</c>.
        /// </summary>
        public Version Version { get; set; } = System.Net.HttpVersion.Version11;

        /// <summary>
        /// Исходный запрос, на который получен этот ответ.
        /// Устанавливается транспортом автоматически.
        /// </summary>
        public HttpRequestMessage? RequestMessage { get; set; }

        /// <summary>
        /// Тело ответа.
        /// </summary>
        public HttpContent? Content
        {
            get => _content;
            set => _content = value;
        }

        /// <summary>
        /// Заголовки ответа.
        /// </summary>
        public HttpResponseHeaders Headers => _headers ??= new HttpResponseHeaders();

        /// <summary>
        /// Trailing-заголовки, полученные после тела при chunked-передаче.
        /// </summary>
        /// <remarks>
        /// В .NET Framework 4.8 это свойство отсутствует в BCL; добавлено для
        /// совместимости с .NET 5+ и для доступа к trailing-заголовкам,
        /// которые поддерживаются libcurl.
        /// </remarks>
        public HttpResponseHeaders TrailingHeaders => _trailingHeaders ??= new HttpResponseHeaders();

        /// <summary>
        /// <see langword="true"/>, если статус-код в диапазоне 200–299.
        /// </summary>
        public bool IsSuccessStatusCode => _statusCode >= HttpStatusCode.OK && (int)_statusCode <= 299;

        // ================================================================
        //  Методы
        // ================================================================

        /// <summary>
        /// Бросает <see cref="HttpRequestException"/>, если статус-код не в диапазоне 200–299.
        /// </summary>
        /// <returns>Тот же экземпляр ответа для цепочки вызовов.</returns>
        /// <exception cref="HttpRequestException">Статус-код не в диапазоне 200–299.</exception>
        public HttpResponseMessage EnsureSuccessStatusCode()
        {
            if (IsSuccessStatusCode)
            {
                return this;
            }

            var message = $"Response status code does not indicate success: " +
                          $"{(int)_statusCode} ({_reasonPhrase ?? _statusCode.ToString()}).";

            throw new HttpRequestException(message, null, _statusCode);
        }

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
        /// Освобождает ресурсы ответа. По умолчанию освобождает тело
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
                _content?.Dispose();
                _content = null;
            }

            _disposed = true;
        }
    }
}