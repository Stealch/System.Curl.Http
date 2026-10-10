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
using System.Curl.Http;

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
        /// совместимости с BCL, но фактически не вызывается. Для реальной
        /// валидации через libcurl нужен отдельный механизм (CURLOPT_SSL_CTX_FUNCTION).
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

            using var session = new CurlEasySession();
            using var parser = new HttpResponseParser();

            try
            {
                // 1. Маппинг запроса в сессию.
                await HttpRequestMapper.MapAsync(request, session, cancellationToken).ConfigureAwait(false);

                // 2. Применение конфигурации транспорта.
                ApplyTransportOptions(session);

                // 3. Установка CA bundle из ресурсов.
                ApplyCaBundle(session);

                // 4. Установка callback'ов ответа.
                session.SetWriteCallback(parser.WriteCallback, parser.WriteUserData);
                session.SetHeaderCallback(parser.HeaderCallback, parser.HeaderUserData);

                // 5. Выполнение запроса.
                cancellationToken.ThrowIfCancellationRequested();
                session.Perform();

                // 6. Чтение статус-кода.
                var statusCode = session.GetResponseCode();

                // 7. Финализация парсера.
                var parsed = parser.Complete();

                // 8. Сборка HttpResponseMessage.
                var response = BuildResponse(request, session, parsed, statusCode);

                Logger.Trace(Category,
                    $"Ответ получен: {statusCode} {parsed.ReasonPhrase}, " +
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

        // ================================================================
        //  Применение конфигурации
        // ================================================================

        private void ApplyTransportOptions(CurlEasySession session)
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
                // Пустая строка означает «не запрашивать сжатие».
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
            CurlEasySession session,
            ParsedHttpResponse parsed,
            long statusCode)
        {
            var response = new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                ReasonPhrase = parsed.ReasonPhrase,
                RequestMessage = request,
                Version = ParseHttpVersion(parsed.HttpVersion),
            };

            // Заголовки.
            foreach (var header in parsed.Headers)
            {
                foreach (var value in header.Value)
                {
                    response.Headers.Add(header.Key, value);
                }
            }

            // Тело.
            if (parsed.Body.Length > 0)
            {
                var content = new ByteArrayContent(parsed.Body);

                // Переносим Content-* заголовки из ответа в тело.
                MoveContentHeaders(response.Headers, content.Headers);

                // Если Content-Type не был установлен, но тело есть — считаем, что это бинарь.
                if (content.Headers.ContentType == null)
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                }

                response.Content = content;
            }

            return response;
        }

        private static void MoveContentHeaders(HttpResponseHeaders source, HttpContentHeaders target)
        {
            // Заголовки, которые относятся к телу, а не к ответу.
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

        // ================================================================
        //  Освобождение
        // ================================================================

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // Ресурсов, требующих освобождения, нет: сессия и парсер
            // освобождаются через using в SendAsync.
            // Базовая реализация установит флаг _disposed и защитит
            // от повторного вызова.
            base.Dispose(disposing);
        }
    }
}