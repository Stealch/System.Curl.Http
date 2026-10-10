#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Curl.Http.Internal;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Переносит данные из <see cref="HttpRequestMessage"/> в <see cref="CurlEasySession"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Статический класс с единственным публичным методом <see cref="MapAsync"/>.
    /// Не хранит состояние.
    /// </para>
    /// <para>
    /// Порядок применения: URL, метод, заголовки запроса, тело, заголовки тела.
    /// Заголовки тела применяются последними, чтобы переопределить возможные
    /// конфликты с заголовками запроса.
    /// </para>
    /// </remarks>
    internal static class HttpRequestMapper
    {
        private const string Category = "HttpRequestMapper";

        private const long SslVersionTls12 = 6L;
        private const long HttpVersion2Tls = 4L;

        /// <summary>
        /// Применяет параметры запроса к сессии.
        /// </summary>
        /// <param name="request">HTTP-запрос.</param>
        /// <param name="session">Сессия libcurl.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        public static async Task MapAsync(
            HttpRequestMessage request,
            CurlEasySession session,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            // ============================================================
            //  1. URL
            // ============================================================

            if (request.RequestUri == null)
            {
                throw new InvalidOperationException(
                    "URI запроса не задан. Установите RequestUri или BaseAddress в HttpClient.");
            }

            session.SetUrl(request.RequestUri.ToString());

            // ============================================================
            //  2. Метод
            // ============================================================

            ApplyMethod(request.Method, session);

            // ============================================================
            //  3. Заголовки запроса
            // ============================================================

            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    session.AddHeader($"{header.Key}: {value}");
                }
            }

            // ============================================================
            //  4. Тело запроса
            // ============================================================

            if (request.Content != null)
            {
                await ApplyContentAsync(request.Content, session, cancellationToken).ConfigureAwait(false);
            }

            // ============================================================
            //  5. TLS / HTTP-версия
            // ============================================================

            session.SetSslVersion(SslVersionTls12);
            session.SetHttpVersion(HttpVersion2Tls);
            session.SetNoSignal(true);

            var headerCount = request.Headers is null ? 0 : CountHeaders(request.Headers);
            Logger.Trace(Category,
                $"Запрос подготовлен: {request.Method.Method} {request.RequestUri}, " +
                $"заголовков: {headerCount}");
        }

        // ================================================================
        //  Метод
        // ================================================================

        private static void ApplyMethod(HttpMethod method, CurlEasySession session)
        {
            switch (method.Method.ToUpperInvariant())
            {
                case "GET":
                    session.SetHttpGet();
                    break;

                case "POST":
                    session.SetPost();
                    break;

                case "HEAD":
                    session.SetNoBody();
                    break;

                case "PUT":
                case "DELETE":
                case "PATCH":
                case "OPTIONS":
                case "TRACE":
                case "CONNECT":
                    session.SetCustomRequest(method.Method);
                    break;

                default:
                    // Неизвестный метод — передаём как есть. libcurl отправит
                    // его в строке запроса, сервер сам решит, что делать.
                    session.SetCustomRequest(method.Method);
                    break;
            }
        }

        // ================================================================
        //  Тело запроса
        // ================================================================

        private static async Task ApplyContentAsync(
            HttpContent content,
            CurlEasySession session,
            CancellationToken cancellationToken)
        {
            // Заголовки тела (Content-Type, Content-Length, Content-Encoding и т.д.)
            foreach (var header in content.Headers)
            {
                foreach (var value in header.Value)
                {
                    session.AddHeader($"{header.Key}: {value}");
                }
            }

            // Content-Length: если известен — устанавливаем вручную,
            // чтобы libcurl не пытался вычислять его сам через chunked.
            var contentLength = content.GetContentLength();
            if (contentLength.HasValue)
            {
                session.AddHeader($"Content-Length: {contentLength.Value}");
            }

            // Тело как массив байт. Для больших тел это может быть
            // неоптимально (всё в память), но для API-запросов — норма.
            var body = await content.ReadAsByteArrayInternalAsync(cancellationToken).ConfigureAwait(false);

            if (body.Length > 0)
            {
                session.SetPostData(body);
            }
        }

        // ================================================================
        //  Утилиты
        // ================================================================

        private static int CountHeaders(HttpHeaders headers)
        {
            var count = 0;
            foreach (var header in headers)
            {
                count++;
            }

            return count;
        }
    }
}