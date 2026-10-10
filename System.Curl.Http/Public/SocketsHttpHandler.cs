#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Curl.Http.Curl;

namespace System.Curl.Http
{
    /// <summary>
    /// Обработчик HTTP-сообщений, использующий libcurl.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Этот класс — <b>заглушка совместимости</b> с <c>System.Net.Http.SocketsHttpHandler</c>.
    /// В BCL <c>SocketsHttpHandler</c> — низкоуровневая реализация транспорта на сокетах;
    /// в <c>System.Curl.Http</c> весь транспорт обеспечивает libcurl, поэтому
    /// <c>SocketsHttpHandler</c> не имеет собственной логики.
    /// </para>
    /// <para>
    /// Класс существует для того, чтобы код, написанный под BCL
    /// (<c>new HttpClient(new SocketsHttpHandler())</c>), компилировался
    /// без изменений. Поведение идентично <see cref="HttpClientHandler"/>.
    /// </para>
    /// </remarks>
    public sealed class SocketsHttpHandler : HttpMessageHandler
    {
        private readonly HttpClientHandler _inner;

        /// <summary>
        /// Создаёт обработчик с настройками по умолчанию.
        /// </summary>
        public SocketsHttpHandler()
        {
            _inner = new HttpClientHandler();
        }

        // ================================================================
        //  Свойства (проброс в HttpClientHandler)
        // ================================================================

        /// <summary>Автоматически следовать редиректам.</summary>
        public bool AllowAutoRedirect
        {
            get => _inner.AllowAutoRedirect;
            set => _inner.AllowAutoRedirect = value;
        }

        /// <summary>Максимальное число автоматических редиректов.</summary>
        public int MaxAutomaticRedirections
        {
            get => _inner.MaxAutomaticRedirections;
            set => _inner.MaxAutomaticRedirections = value;
        }

        /// <summary>Автоматическая декомпрессия ответа.</summary>
        public System.Net.DecompressionMethods AutomaticDecompression
        {
            get => _inner.AutomaticDecompression;
            set => _inner.AutomaticDecompression = value;
        }

        /// <summary>Использовать прокси.</summary>
        public bool UseProxy
        {
            get => _inner.UseProxy;
            set => _inner.UseProxy = value;
        }

        /// <summary>Прокси-сервер.</summary>
        public System.Net.IWebProxy? Proxy
        {
            get => _inner.Proxy;
            set => _inner.Proxy = value;
        }

        /// <summary>
        /// Использовать учётные данные текущего пользователя Windows
        /// (Negotiate / SPNEGO / Kerberos).
        /// </summary>
        public bool UseDefaultCredentials
        {
            get => _inner.UseDefaultCredentials;
            set => _inner.UseDefaultCredentials = value;
        }

        /// <summary>Учётные данные для аутентификации.</summary>
        public System.Net.ICredentials? Credentials
        {
            get => _inner.Credentials;
            set => _inner.Credentials = value;
        }

        /// <summary>Максимальное количество одновременных соединений на сервер.</summary>
        public int MaxConnectionsPerServer
        {
            get => _inner.MaxConnectionsPerServer;
            set => _inner.MaxConnectionsPerServer = value;
        }

        /// <summary>Опции TLS.</summary>
        public SslClientAuthenticationOptions? SslOptions
        {
            get => _inner.SslOptions;
            set => _inner.SslOptions = value;
        }

        /// <summary>Callback для валидации сертификата сервера.</summary>
        public Func<HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, System.Net.Security.SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => _inner.ServerCertificateCustomValidationCallback;
            set => _inner.ServerCertificateCustomValidationCallback = value;
        }

        // ================================================================
        //  Отправка запроса
        // ================================================================

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _inner.SendAsyncInternal(request, cancellationToken);
        }

        // ================================================================
        //  Освобождение
        // ================================================================

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}