#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Curl.Http.Internal;
using System.Curl.Http.Interop;
using System.Curl.Http;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Обработчик HTTP-сообщений на основе libcurl. Единственный транспорт
    /// в <c>System.Curl.Http</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель — <c>System.Net.Http.SocketsHttpHandler</c> с той разницей, что
    /// транспорт — libcurl, а не сокеты .NET. Это позволяет использовать
    /// современный TLS на Windows 7 и выше независимо от Schannel.
    /// </para>
    /// <para>
    /// Свойства конфигурации прокидываются в <see cref="CurlEasySession"/>
    /// при каждом запросе.
    /// </para>
    /// </remarks>
    internal sealed class CurlHttpMessageHandler : HttpMessageHandler
    {
        private const string Category = "CurlHttpMessageHandler";
        private const int DefaultMaxRedirs = 50;
        private GCHandle _clientCertBlobHandle;
        private IntPtr _clientCertBlobPtr = IntPtr.Zero;

        // ================================================================
        //  Конфигурация транспорта
        // ================================================================

        /// <summary>
        /// Автоматически следовать редиректам. По умолчанию <see langword="true"/>.
        /// </summary>
        public bool FollowLocation { get; set; } = true;

        /// <summary>
        /// Максимальное число автоматических редиректов. По умолчанию 50.
        /// </summary>
        public int MaxRedirs { get; set; } = DefaultMaxRedirs;

        /// <summary>
        /// Автоматическая декомпрессия ответа.
        /// </summary>
        public DecompressionMethods AutomaticDecompression { get; set; } = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        /// <summary>
        /// Использовать прокси.
        /// </summary>
        public bool UseProxy { get; set; }

        /// <summary>
        /// Прокси-сервер. Может быть <see langword="null"/>.
        /// </summary>
        public IWebProxy? Proxy { get; set; }

        /// <summary>
        /// Список хостов, для которых прокси обходится. Через запятую.
        /// </summary>
        public string? NoProxyList { get; set; }

        /// <summary>
        /// Тип прокси (HTTP, HTTPS, SOCKS4, SOCKS5 и т.д.).
        /// <see langword="null"/> — libcurl определит из схемы URL.
        /// </summary>
        public long? ProxyType { get; set; }

        /// <summary>
        /// Метод аутентификации на прокси (битовая маска из <see cref="CURLAUTH"/>).
        /// <see langword="null"/> — libcurl использует значение по умолчанию.
        /// </summary>
        public long? ProxyAuth { get; set; }

        /// <summary>
        /// Pre-proxy (SOCKS-прокси перед HTTP-прокси).
        /// </summary>
        public string? PreProxy { get; set; }

        /// <summary>
        /// Версия TLS для HTTPS-прокси (см. <see cref="CURL_SSLVERSION"/>).
        /// </summary>
        public long? ProxySslVersion { get; set; }

        /// <summary>
        /// Проверять сертификат HTTPS-прокси. По умолчанию <see langword="true"/>.
        /// </summary>
        public bool ProxySslVerifyPeer { get; set; } = true;

        /// <summary>
        /// Проверять имя хоста в сертификате HTTPS-прокси. По умолчанию <see langword="true"/>.
        /// </summary>
        public bool ProxySslVerifyHost { get; set; } = true;

        /// <summary>
        /// Путь к CA bundle для проверки HTTPS-прокси.
        /// </summary>
        public string? ProxyCaBundle { get; set; }

        /// <summary>
        /// Директория с CA для проверки HTTPS-прокси.
        /// </summary>
        public string? ProxyCaPath { get; set; }

        /// <summary>
        /// Путь к клиентскому сертификату для mutual TLS на прокси.
        /// </summary>
        public string? ProxyClientCert { get; set; }

        /// <summary>
        /// Тип клиентского сертификата прокси (PEM, DER).
        /// </summary>
        public string? ProxyClientCertType { get; set; }

        /// <summary>
        /// Путь к приватному ключу клиентского сертификата прокси.
        /// </summary>
        public string? ProxyClientKey { get; set; }

        /// <summary>
        /// Тип приватного ключа прокси (PEM, DER).
        /// </summary>
        public string? ProxyClientKeyType { get; set; }

        /// <summary>
        /// Пароль к приватному ключу прокси.
        /// </summary>
        public string? ProxyKeyPassword { get; set; }

        /// <summary>
        /// Порт прокси (если не указан в URL).
        /// </summary>
        public long? ProxyPort { get; set; }

        /// <summary>
        /// Учётные данные для аутентификации на прокси.
        /// </summary>
        public ICredentials? ProxyCredentials { get; set; }

        // ================================================================
        //  Общая конфигурация
        // ================================================================

        /// <summary>
        /// Использовать учётные данные по умолчанию. Пока не поддерживается.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Учётные данные. Пока не поддерживается.
        /// </summary>
        public ICredentials? Credentials { get; set; }

        /// <summary>
        /// Максимальное количество одновременных соединений на сервер.
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = int.MaxValue;

        /// <summary>
        /// Максимальный размер буфера тела запроса.
        /// </summary>
        public int MaxRequestContentBufferSize { get; set; } = int.MaxValue;

        /// <summary>
        /// Опции TLS.
        /// </summary>
        public SslClientAuthenticationOptions? SslOptions { get; set; }

        /// <summary>
        /// Callback для валидации сертификата сервера.
        /// </summary>
        /// <remarks>
        /// <b>Пока не подключён к libcurl.</b> Свойство сохраняется для
        /// совместимости с BCL, но фактически не вызывается.
        /// </remarks>
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback { get; set; }

        // ================================================================
        //  SendAsync
        // ================================================================

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Решение о режиме принимает HttpClient через HttpCompletionOption,
            // транспорт читает его из Options запроса.
            var completionOption = GetCompletionOption(request);

            if (completionOption == HttpCompletionOption.ResponseHeadersRead)
            {
                return await SendStreamingAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return await SendBufferedAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Буферизующий путь (ResponseContentRead): тело целиком в памяти.
        /// </summary>
        private async Task<HttpResponseMessage> SendBufferedAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var session = new CurlEasySession();
            using var parser = new HttpResponseParser();

            try
            {
                await HttpRequestMapper.MapAsync(request, session, cancellationToken).ConfigureAwait(false);
                ApplyTransportOptions(session, request.RequestUri);
                ApplyCaBundle(session);

                session.SetWriteCallback(parser.WriteCallback, parser.WriteUserData);
                session.SetHeaderCallback(parser.HeaderCallback, parser.HeaderUserData);

                cancellationToken.ThrowIfCancellationRequested();
                session.Perform();

                var statusCode = session.GetResponseCode();
                var parsed = parser.Complete();
                var response = BuildResponse(request, parsed, statusCode);

                Logger.Trace(Category,
                    $"Ответ получен (buffered): {statusCode} {parsed.ReasonPhrase}, " +
                    $"тело {parsed.Body.Length} байт.");

                return response;
            }
            catch (CurlException ex)
            {
                Logger.Error(Category, "Ошибка libcurl при выполнении запроса.", ex);
                throw new HttpRequestException(
                    $"Ошибка HTTP-запроса: {ex.Message}",
                    ex,
                    null);
            }
        }

        /// <summary>
        /// Стриминговый путь (ResponseHeadersRead): возвращает ответ после
        /// заголовков, тело читается потоком по мере поступления через multi.
        /// </summary>
        private async Task<HttpResponseMessage> SendStreamingAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var session = new CurlEasySession();
            var stream = new CurlResponseStream();
            var parser = new HttpResponseParser(stream);
            var multi = new CurlMultiSession();

            try
            {
                await HttpRequestMapper.MapAsync(request, session, cancellationToken).ConfigureAwait(false);
                ApplyTransportOptions(session, request.RequestUri);
                ApplyCaBundle(session);

                session.SetWriteCallback(parser.WriteCallback, parser.WriteUserData);
                session.SetHeaderCallback(parser.HeaderCallback, parser.HeaderUserData);

                cancellationToken.ThrowIfCancellationRequested();

                var performTask = Task.Run(
                    () => multi.PerformAsync(session.Handle, cancellationToken),
                    cancellationToken);

                var completed = await Task.WhenAny(parser.HeadersReady, performTask).ConfigureAwait(false);

                if (completed == performTask && !parser.HeadersReady.IsCompleted)
                {
                    var code = await performTask.ConfigureAwait(false);
                    if (code != CURLcode.CURLE_OK)
                    {
                        throw BuildCurlException("StreamingPerform", code);
                    }

                    var parsedEmpty = parser.Complete();
                    var responseEmpty = BuildResponse(request, parsedEmpty, parsedEmpty.StatusCode);
                    CleanupStreaming(session, parser, multi);
                    return responseEmpty;
                }

                var statusCode = parser.Response.StatusCode;
                var content = new StreamingHttpContent(stream);

                if (!performTask.IsCompleted)
                {
                    _ = performTask.ContinueWith(t =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                var ex = t.Exception?.GetBaseException() ?? new HttpRequestException("Ошибка стриминга.");
                                stream.SetException(ex);
                            }
                            else if (t.IsCanceled)
                            {
                                stream.SetException(new TaskCanceledException("Стриминг отменён."));
                            }
                            else
                            {
                                var code = t.Result;
                                if (code != CURLcode.CURLE_OK)
                                {
                                    stream.SetException(BuildCurlException("StreamingPerform", code));
                                }
                                else
                                {
                                    parser.Complete();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            stream.SetException(ex);
                        }
                        finally
                        {
                            CleanupStreaming(session, parser, multi);
                        }
                    }, TaskScheduler.Default);
                }
                else
                {
                    if (performTask.IsFaulted)
                    {
                        var ex = performTask.Exception?.GetBaseException() ?? new HttpRequestException("Ошибка стриминга.");
                        stream.SetException(ex);
                    }
                    else if (performTask.IsCanceled)
                    {
                        stream.SetException(new TaskCanceledException("Стриминг отменён."));
                    }
                    else
                    {
                        var code = performTask.Result;
                        if (code != CURLcode.CURLE_OK)
                        {
                            stream.SetException(BuildCurlException("StreamingPerform", code));
                        }
                        else
                        {
                            parser.Complete();
                        }
                    }

                    CleanupStreaming(session, parser, multi);
                }

                var response = BuildResponseWithContent(request, parser.Response, statusCode, content);

                Logger.Trace(Category,
                    $"Ответ получен (streaming): {statusCode} {parser.Response.ReasonPhrase}.");

                return response;
            }
            catch (CurlException ex)
            {
                CleanupStreaming(session, parser, multi);
                Logger.Error(Category, "Ошибка libcurl при выполнении запроса (streaming).", ex);
                throw new HttpRequestException(
                    $"Ошибка HTTP-запроса: {ex.Message}",
                    ex,
                    null);
            }
            catch
            {
                CleanupStreaming(session, parser, multi);
                throw;
            }
        }

        /// <summary>
        /// Освобождает ресурсы стримингового пути. Вызывается ровно один раз.
        /// </summary>
        private static void CleanupStreaming(
            CurlEasySession session,
            HttpResponseParser parser,
            CurlMultiSession multi)
        {
            try
            {
                multi.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning(Category, "Ошибка при освобождении multi-сессии.", ex);
            }

            try
            {
                parser.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning(Category, "Ошибка при освобождении парсера.", ex);
            }

            try
            {
                session.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning(Category, "Ошибка при освобождении easy-сессии.", ex);
            }
        }

        /// <summary>
        /// Извлекает <see cref="HttpCompletionOption"/> из запроса. Значение
        /// устанавливается <see cref="HttpClient"/> через <see cref="HttpRequestOptions"/>.
        /// </summary>
        private static HttpCompletionOption GetCompletionOption(HttpRequestMessage request)
        {
            var key = new HttpRequestOptionsKey<HttpCompletionOption>("System.Curl.Http.CompletionOption");
            if (request.Options.TryGetValue(key, out var option))
            {
                return option;
            }

            return HttpCompletionOption.ResponseContentRead;
        }

        // ================================================================
        //  Применение конфигурации
        // ================================================================

        private void ApplyTransportOptions(CurlEasySession session, Uri? requestUri)
        {
            session.SetFollowLocation(FollowLocation);

            if (FollowLocation && MaxRedirs > 0)
            {
                session.SetMaxRedirs(MaxRedirs);
            }

            var encoding = BuildAcceptEncoding(AutomaticDecompression);
            if (!string.IsNullOrEmpty(encoding))
            {
                session.SetAcceptEncoding(encoding!);
            }

            ApplyProxy(session, requestUri);
            ApplyAuthentication(session, requestUri);
            ApplyClientCertificates(session);
        }

        private void ApplyProxy(CurlEasySession session, Uri? requestUri)
        {
            if (!UseProxy)
            {
                return;
            }

            if (!string.IsNullOrEmpty(PreProxy))
            {
                session.SetPreProxy(PreProxy!);
            }

            var proxyUrl = ResolveProxyUrl(requestUri);
            if (string.IsNullOrEmpty(proxyUrl))
            {
                return;
            }

            session.SetProxy(proxyUrl!);

            if (ProxyType.HasValue)
            {
                session.SetProxyType(ProxyType.Value);
            }

            if (ProxyPort.HasValue)
            {
                session.SetProxyPort(ProxyPort.Value);
            }

            session.SetHttpProxyTunnel(true);

            if (!string.IsNullOrEmpty(NoProxyList))
            {
                session.SetNoProxy(NoProxyList!);
            }

            ApplyProxyCredentials(session, requestUri);
            ApplyProxyTlsOptions(session);

            Logger.Trace(Category, $"Прокси применён: {proxyUrl}");
        }

        private void ApplyAuthentication(CurlEasySession session, Uri? requestUri)
        {
            if (UseDefaultCredentials)
            {
                ApplyUseDefaultCredentials(session);
                return;
            }

            if (Credentials != null)
            {
                ApplyCredentials(session, requestUri);
            }
        }

        private string? ResolveProxyUrl(Uri? requestUri)
        {
            if (Proxy != null && requestUri != null)
            {
                var proxyUri = Proxy.GetProxy(requestUri);
                if (proxyUri != null)
                {
                    return proxyUri.ToString();
                }
            }

            return null;
        }

        private void ApplyProxyCredentials(CurlEasySession session, Uri? requestUri)
        {
            if (ProxyCredentials == null || requestUri == null)
            {
                return;
            }

            var networkCredential = ProxyCredentials.GetCredential(requestUri, "Basic");
            if (networkCredential == null)
            {
                return;
            }

            var user = networkCredential.UserName ?? string.Empty;
            var password = networkCredential.Password ?? string.Empty;
            session.SetProxyCredentials($"{user}:{password}");

            var authMask = ProxyAuth ?? CURLAUTH.ANY;
            session.SetProxyAuth(authMask);
        }

        private void ApplyProxyTlsOptions(CurlEasySession session)
        {
            if (ProxySslVersion.HasValue)
            {
                session.SetProxySslVersion(ProxySslVersion.Value);
            }

            session.SetProxySslVerifyPeer(ProxySslVerifyPeer);
            session.SetProxySslVerifyHost(ProxySslVerifyHost);

            if (!string.IsNullOrEmpty(ProxyCaBundle))
            {
                session.SetProxyCaBundle(ProxyCaBundle!);
            }

            if (!string.IsNullOrEmpty(ProxyCaPath))
            {
                session.SetProxyCaPath(ProxyCaPath!);
            }

            if (!string.IsNullOrEmpty(ProxyClientCert))
            {
                session.SetProxySslCert(ProxyClientCert!);

                if (!string.IsNullOrEmpty(ProxyClientCertType))
                {
                    session.SetProxySslCertType(ProxyClientCertType!);
                }
            }

            if (!string.IsNullOrEmpty(ProxyClientKey))
            {
                session.SetProxySslKey(ProxyClientKey!);

                if (!string.IsNullOrEmpty(ProxyClientKeyType))
                {
                    session.SetProxySslKeyType(ProxyClientKeyType!);
                }
            }

            if (!string.IsNullOrEmpty(ProxyKeyPassword))
            {
                session.SetProxyKeyPassword(ProxyKeyPassword!);
            }
        }

        private void ApplyCredentials(CurlEasySession session, Uri? requestUri)
        {
            if (Credentials == null || requestUri == null)
            {
                return;
            }

            var networkCredential = Credentials.GetCredential(requestUri, "Basic");
            if (networkCredential == null)
            {
                return;
            }

            var user = networkCredential.UserName ?? string.Empty;
            var password = networkCredential.Password ?? string.Empty;

            session.SetUserPwd($"{user}:{password}");
            session.SetHttpAuth(CURLAUTH.BASIC);

            Logger.Trace(Category, "Учётные данные сервера применены (Basic).");
        }

        private void ApplyUseDefaultCredentials(CurlEasySession session)
        {
            if (!UseDefaultCredentials)
            {
                return;
            }

            session.SetHttpAuth(CURLAUTH.NEGOTIATE);

            Logger.Trace(Category, "Аутентификация Windows (Negotiate) включена.");
        }

        private void ApplyClientCertificates(CurlEasySession session)
        {
            var certificates = SslOptions?.ClientCertificates;
            if (certificates == null || certificates.Count == 0)
            {
                return;
            }

            X509Certificate2? cert = null;
            foreach (var c in certificates)
            {
                if (c is X509Certificate2 x509 && x509.HasPrivateKey)
                {
                    cert = x509;
                    break;
                }
            }

            if (cert == null)
            {
                Logger.Warning(Category, "ClientCertificates заданы, но ни один не имеет приватного ключа.");
                return;
            }

            var pfx = cert.Export(X509ContentType.Pkcs12);

            _clientCertBlobHandle = GCHandle.Alloc(pfx, GCHandleType.Pinned);
            var blob = new curl_blob
            {
                data = _clientCertBlobHandle.AddrOfPinnedObject(),
                len = (UIntPtr)(uint)pfx.Length,
                flags = CURL_BLOB.NOCOPY,
            };

            _clientCertBlobPtr = Marshal.AllocHGlobal(Marshal.SizeOf<curl_blob>());
            Marshal.StructureToPtr(blob, _clientCertBlobPtr, false);

            session.SetSslCertBlob(_clientCertBlobPtr);
            session.SetSslCertType("P12");

            Logger.Trace(Category, $"Клиентский сертификат применён (PKCS#12, {pfx.Length} байт).");
        }

        private static void ApplyCaBundle(CurlEasySession session)
        {
            var caBundlePath = Native.LibCurlLoader.GetCaBundlePath();
            if (!string.IsNullOrEmpty(caBundlePath) && System.IO.File.Exists(caBundlePath))
            {
                session.SetCaBundle(caBundlePath!);
            }
            else
            {
                Logger.Warning(Category, "CA bundle не найден, используется системное хранилище libcurl.");
            }
        }

        private static string? BuildAcceptEncoding(DecompressionMethods methods)
        {
            if (methods == DecompressionMethods.None)
            {
                return string.Empty;
            }

            var parts = new List<string>(2);

            if ((methods & DecompressionMethods.GZip) != 0)
            {
                parts.Add("gzip");
            }

            if ((methods & DecompressionMethods.Deflate) != 0)
            {
                parts.Add("deflate");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // ================================================================
        //  Сборка ответа
        // ================================================================

        private static HttpResponseMessage BuildResponse(
            HttpRequestMessage request,
            ParsedHttpResponse parsed,
            long statusCode)
        {
            var response = BuildResponseWithContent(request, parsed, statusCode, null);

            if (parsed.Body.Length > 0)
            {
                var content = new ByteArrayContent(parsed.Body);
                MoveContentHeaders(response.Headers, content.Headers);

                if (content.Headers.ContentType == null)
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                }

                response.Content = content;
            }

            return response;
        }

        private static HttpResponseMessage BuildResponseWithContent(
            HttpRequestMessage request,
            ParsedHttpResponse parsed,
            long statusCode,
            HttpContent? content)
        {
            var response = new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                ReasonPhrase = parsed.ReasonPhrase,
                RequestMessage = request,
                Version = ParseHttpVersion(parsed.HttpVersion),
                Content = content,
            };

            foreach (var header in parsed.Headers)
            {
                foreach (var value in header.Value)
                {
                    response.Headers.Add(header.Key, value);
                }
            }

            if (content != null)
            {
                MoveContentHeaders(response.Headers, content.Headers);
            }

            return response;
        }

        private static void MoveContentHeaders(HttpResponseHeaders source, HttpContentHeaders target)
        {
            var contentHeaderNames = new[]
            {
                "Content-Type",
                "Content-Length",
                "Content-Encoding",
                "Content-Language",
                "Content-Location",
                "Content-Range",
                "Content-MD5",
                "Content-Disposition",
                "Allow",
                "Expires",
                "Last-Modified",
            };

            foreach (var name in contentHeaderNames)
            {
                if (!source.TryGetValues(name, out var values) || values == null)
                {
                    continue;
                }

                foreach (var value in values)
                {
                    target.Add(name, value);
                }

                source.Remove(name);
            }
        }

        private static Version ParseHttpVersion(string version)
        {
            switch (version)
            {
                case "1.0":
                    return System.Net.HttpVersion.Version10;
                case "1.1":
                    return System.Net.HttpVersion.Version11;
                case "2":
                case "2.0":
                    return new Version(2, 0);
                case "3":
                case "3.0":
                    return new Version(3, 0);
                default:
                    return System.Net.HttpVersion.Version11;
            }
        }

        /// <summary>
        /// Строит <see cref="CurlException"/> по коду libcurl.
        /// </summary>
        private static CurlException BuildCurlException(string operation, CURLcode code)
        {
            return new CurlException(operation, code, $"libcurl error: {code} ({(int)code})");
        }

        // ================================================================
        //  Освобождение
        // ================================================================

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_clientCertBlobHandle.IsAllocated)
            {
                _clientCertBlobHandle.Free();
            }

            if (_clientCertBlobPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_clientCertBlobPtr);
                _clientCertBlobPtr = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }
    }
}