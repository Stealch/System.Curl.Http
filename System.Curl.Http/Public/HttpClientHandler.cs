#nullable enable

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Curl.Http.Curl;

namespace System.Curl.Http
{
    /// <summary>
    /// Обработчик HTTP-сообщений на основе libcurl. Предоставляет
    /// BCL-совместимые свойства для настройки транспорта.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpClientHandler</c>.
    /// </para>
    /// <para>
    /// Используется как обработчик по умолчанию в <see cref="HttpClient"/>.
    /// Внутри создаёт <c>CurlHttpMessageHandler</c> и пробрасывает в него
    /// настройки через <c>HttpRequestOptions</c> или напрямую через
    /// опции libcurl.
    /// </para>
    /// </remarks>
    public class HttpClientHandler : HttpMessageHandler
    {
        private readonly CurlHttpMessageHandler _inner;

        /// <summary>
        /// Создаёт обработчик с настройками по умолчанию.
        /// </summary>
        public HttpClientHandler()
        {
            _inner = new CurlHttpMessageHandler();
        }

        // ================================================================
        //  Свойства, пробрасываемые в транспорт
        // ================================================================

        /// <summary>
        /// Автоматически следовать редиректам. По умолчанию <see langword="true"/>.
        /// </summary>
        public bool AllowAutoRedirect
        {
            get => _inner.FollowLocation;
            set => _inner.FollowLocation = value;
        }

        /// <summary>
        /// Максимальное число автоматических редиректов. По умолчанию 50.
        /// </summary>
        public int MaxAutomaticRedirections
        {
            get => _inner.MaxRedirs;
            set => _inner.MaxRedirs = value;
        }

        /// <summary>
        /// Автоматическая декомпрессия ответа (gzip, deflate).
        /// </summary>
        public DecompressionMethods AutomaticDecompression
        {
            get => _inner.AutomaticDecompression;
            set => _inner.AutomaticDecompression = value;
        }

        /// <summary>
        /// Использовать прокси. По умолчанию <see langword="false"/>.
        /// </summary>
        public bool UseProxy
        {
            get => _inner.UseProxy;
            set => _inner.UseProxy = value;
        }

        /// <summary>
        /// Прокси-сервер. Может быть <see langword="null"/>.
        /// </summary>
        public IWebProxy? Proxy
        {
            get => _inner.Proxy;
            set => _inner.Proxy = value;
        }

        /// <summary>
        /// Использовать учётные данные текущего пользователя Windows
        /// (Negotiate / SPNEGO / Kerberos).
        /// </summary>
        /// <remarks>
        /// При установке <see langword="true"/> <see cref="Credentials"/>
        /// автоматически принимает значение <see cref="CredentialCache.DefaultCredentials"/>,
        /// как в BCL.
        /// </remarks>
        public bool UseDefaultCredentials
        {
            get => _inner.UseDefaultCredentials;
            set
            {
                _inner.UseDefaultCredentials = value;
                if (value)
                {
                    _inner.Credentials = CredentialCache.DefaultCredentials;
                }
            }
        }

        /// <summary>
        /// Учётные данные для аутентификации на сервере.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Попытка чтения после установки <see cref="UseDefaultCredentials"/> = <see langword="true"/>.
        /// </exception>
        public ICredentials? Credentials
        {
            get
            {
                if (_inner.UseDefaultCredentials)
                {
                    throw new InvalidOperationException(
                        "Credentials нельзя читать, когда UseDefaultCredentials = true.");
                }

                return _inner.Credentials;
            }
            set
            {
                _inner.UseDefaultCredentials = false;
                _inner.Credentials = value;
            }
        }

        /// <summary>
        /// Максимальное количество одновременных соединений на сервер.
        /// </summary>
        public int MaxConnectionsPerServer
        {
            get => _inner.MaxConnectionsPerServer;
            set => _inner.MaxConnectionsPerServer = value;
        }

        /// <summary>
        /// Максимальный размер буфера тела запроса.
        /// </summary>
        public int MaxRequestContentBufferSize
        {
            get => _inner.MaxRequestContentBufferSize;
            set => _inner.MaxRequestContentBufferSize = value;
        }

        /// <summary>
        /// Опции TLS: протоколы, сертификаты клиента, политики.
        /// </summary>
        public SslClientAuthenticationOptions? SslOptions
        {
            get => _inner.SslOptions;
            set => _inner.SslOptions = value;
        }

        /// <summary>
        /// Callback для валидации сертификата сервера.
        /// </summary>
        /// <remarks>
        /// Сигнатура совпадает с <c>System.Net.Http.HttpClientHandler.ServerCertificateCustomValidationCallback</c>.
        /// Возврат <see langword="true"/> — принять сертификат; <see langword="false"/> — отклонить.
        /// </remarks>
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