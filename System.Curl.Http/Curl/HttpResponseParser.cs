#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Curl.Http.Internal;
using System.Curl.Http.Interop;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Промежуточный результат парсинга HTTP-ответа. Не зависит от публичных типов,
    /// чтобы парсер можно было использовать до появления <c>HttpResponseMessage</c>.
    /// </summary>
    internal sealed class ParsedHttpResponse
    {
        public int StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public string HttpVersion { get; set; } = "1.1";

        /// <summary>
        /// Заголовки ответа. Ключ — имя заголовка (как пришло от сервера),
        /// значение — список значений (один заголовок может повторяться).
        /// </summary>
        public Dictionary<string, List<string>> Headers { get; } =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Тело ответа.
        /// </summary>
        public byte[] Body { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Кодировка тела, если удалось определить из <c>Content-Type</c>.
        /// </summary>
        public Encoding? BodyEncoding { get; set; }

        /// <summary>
        /// Тело как строка с учётом <see cref="BodyEncoding"/>.
        /// </summary>
        public string GetBodyAsString()
        {
            var encoding = BodyEncoding ?? Encoding.UTF8;
            return encoding.GetString(Body);
        }
    }

    /// <summary>
    /// Парсер HTTP-ответа. Устанавливается в <see cref="CurlEasySession"/> как
    /// пара callback'ов (<c>WRITEFUNCTION</c> + <c>HEADERFUNCTION</c>) через
    /// <see cref="GetWriteCallback"/> и <see cref="GetHeaderCallback"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Жизненный цикл.</b> Один экземпляр — один запрос. Callback'и держат
    /// <c>GCHandle</c> на экземпляр, поэтому экземпляр должен жить до окончания
    /// <c>Perform()</c> и быть освобождён через <see cref="Dispose"/>.
    /// </para>
    /// <para>
    /// <b>Состояние.</b> Заголовки приходят до тела. Строки заголовков разделяются
    /// <c>\r\n</c>; пустая строка означает конец блока заголовков и начало тела.
    /// <c>100 Continue</c> и подобные промежуточные ответы (1xx) отбрасываются
    /// с сохранением последнего финального ответа.
    /// </para>
    /// </remarks>
    internal sealed class HttpResponseParser : IDisposable
    {
        private const string Category = "HttpResponseParser";

        private readonly MemoryStream _bodyStream = new MemoryStream();
        private readonly StringBuilder _headerLine = new StringBuilder(256);
        private readonly ParsedHttpResponse _response = new ParsedHttpResponse();

        private GCHandleScope _writeCallbackHandle;
        private GCHandleScope _headerCallbackHandle;
        private GCHandleScope _selfHandle;

        private readonly Delegates.curl_write_callback _writeCallback;
        private readonly Delegates.curl_header_callback _headerCallback;

        private bool _headersComplete;
        private bool _disposed;

        public HttpResponseParser()
        {
            _writeCallback = OnWriteData;
            _headerCallback = OnHeaderData;

            _writeCallbackHandle = GCHandleScope.Alloc(_writeCallback);
            _headerCallbackHandle = GCHandleScope.Alloc(_headerCallback);
            _selfHandle = GCHandleScope.Alloc(this);
        }

        /// <summary>
        /// Промежуточный результат парсинга. Заполняется по мере поступления данных.
        /// </summary>
        public ParsedHttpResponse Response => _response;

        /// <summary>
        /// Пользовательский указатель для <c>CURLOPT_WRITEDATA</c>.
        /// </summary>
        public IntPtr WriteUserData => _selfHandle.Handle;

        /// <summary>
        /// Пользовательский указатель для <c>CURLOPT_HEADERDATA</c>.
        /// </summary>
        public IntPtr HeaderUserData => _selfHandle.Handle;

        /// <summary>
        /// Callback для <c>CURLOPT_WRITEFUNCTION</c>.
        /// </summary>
        public Delegates.curl_write_callback WriteCallback => _writeCallback;

        /// <summary>
        /// Callback для <c>CURLOPT_HEADERFUNCTION</c>.
        /// </summary>
        public Delegates.curl_header_callback HeaderCallback => _headerCallback;

        /// <summary>
        /// Финализирует парсинг: определяет кодировку тела по <c>Content-Type</c>
        /// и сохраняет накопленное тело.
        /// </summary>
        public ParsedHttpResponse Complete()
        {
            ThrowIfDisposed();

            _response.Body = _bodyStream.ToArray();
            _response.BodyEncoding = DetectEncoding(_response.Headers);

            Logger.Trace(Category,
                $"Завершено: status={_response.StatusCode}, " +
                $"headers={_response.Headers.Count}, " +
                $"body={_response.Body.Length} байт.");

            return _response;
        }

        // ================================================================
        //  Callback'и libcurl
        // ================================================================

        private UIntPtr OnWriteData(IntPtr buffer, UIntPtr size, UIntPtr nitems, IntPtr userdata)
        {
            var total = (int)(size.ToUInt64() * nitems.ToUInt64());
            if (total <= 0 || buffer == IntPtr.Zero)
            {
                return new UIntPtr((ulong)total);
            }

            try
            {
                var chunk = new byte[total];
                System.Runtime.InteropServices.Marshal.Copy(buffer, chunk, 0, total);
                _bodyStream.Write(chunk, 0, total);

                return new UIntPtr((ulong)total);
            }
            catch (Exception ex)
            {
                Logger.Error(Category, "Ошибка в OnWriteData.", ex);

                // Возврат нуля сигнализирует libcurl прервать передачу
                // с CURLE_WRITE_ERROR.
                return UIntPtr.Zero;
            }
        }

        private UIntPtr OnHeaderData(IntPtr buffer, UIntPtr size, UIntPtr nitems, IntPtr userdata)
        {
            var total = (int)(size.ToUInt64() * nitems.ToUInt64());
            if (total <= 0 || buffer == IntPtr.Zero)
            {
                return new UIntPtr((ulong)total);
            }

            try
            {
                var chunk = new byte[total];
                System.Runtime.InteropServices.Marshal.Copy(buffer, chunk, 0, total);

                // Заголовки приходят ASCII (по RFC 7230), но допускается
                // ISO-8859-1. Декодируем как Latin-1, чтобы не терять байты.
                var text = Encoding.GetEncoding("ISO-8859-1").GetString(chunk);
                AppendHeaderText(text);

                return new UIntPtr((ulong)total);
            }
            catch (Exception ex)
            {
                Logger.Error(Category, "Ошибка в OnHeaderData.", ex);
                return UIntPtr.Zero;
            }
        }

        // ================================================================
        //  Разбор заголовков
        // ================================================================

        private void AppendHeaderText(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\r')
                {
                    continue;
                }

                if (c == '\n')
                {
                    ProcessHeaderLine(_headerLine.ToString());
                    _headerLine.Clear();
                    continue;
                }

                _headerLine.Append(c);
            }
        }

        private void ProcessHeaderLine(string line)
        {
            if (_headersComplete)
            {
                // После пустой строки libcurl снова может прислать заголовки,
                // если это промежуточный ответ (1xx) или trailer после chunked.
                // Новый статус-код сбрасывает состояние.
                if (line.StartsWith("HTTP/", StringComparison.Ordinal))
                {
                    StartNewResponse();
                    ParseStatusLine(line);
                }

                return;
            }

            if (line.Length == 0)
            {
                // Пустая строка — конец блока заголовков.
                // Если статус 1xx — это промежуточный ответ, продолжаем ждать финальный.
                if (_response.StatusCode >= 100 && _response.StatusCode < 200)
                {
                    Logger.Trace(Category, $"Промежуточный ответ {_response.StatusCode}, ожидаем финальный.");
                    _response.Headers.Clear();
                    _response.StatusCode = 0;
                    _response.ReasonPhrase = null;
                    return;
                }

                _headersComplete = true;
                Logger.Trace(Category, $"Заголовки получены: status={_response.StatusCode}, count={_response.Headers.Count}.");
                return;
            }

            if (line.StartsWith("HTTP/", StringComparison.Ordinal))
            {
                StartNewResponse();
                ParseStatusLine(line);
                return;
            }

            ParseHeaderLine(line);
        }

        private void StartNewResponse()
        {
            _response.StatusCode = 0;
            _response.ReasonPhrase = null;
            _response.Headers.Clear();
            _headersComplete = false;
        }

        private void ParseStatusLine(string line)
        {
            // Формат: HTTP/1.1 200 OK
            var firstSpace = line.IndexOf(' ');
            if (firstSpace < 0)
            {
                Logger.Warning(Category, $"Некорректная статус-строка: '{line}'.");
                return;
            }

            var version = line.Substring(0, firstSpace);
            var rest = line.Substring(firstSpace + 1).TrimStart();

            var secondSpace = rest.IndexOf(' ');
            string codeStr;
            string? reason;

            if (secondSpace < 0)
            {
                codeStr = rest;
                reason = null;
            }
            else
            {
                codeStr = rest.Substring(0, secondSpace);
                reason = rest.Substring(secondSpace + 1).TrimStart();
            }

            if (!int.TryParse(codeStr, out var code))
            {
                Logger.Warning(Category, $"Не удалось разобрать статус-код: '{codeStr}'.");
                return;
            }

            _response.HttpVersion = version.StartsWith("HTTP/", StringComparison.Ordinal)
                ? version.Substring(5)
                : version;
            _response.StatusCode = code;
            _response.ReasonPhrase = string.IsNullOrEmpty(reason) ? null : reason;
        }

        private void ParseHeaderLine(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                Logger.Warning(Category, $"Некорректная строка заголовка: '{line}'.");
                return;
            }

            var name = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).TrimStart();

            if (name.Length == 0)
            {
                return;
            }

            if (!_response.Headers.TryGetValue(name, out var values))
            {
                values = new List<string>(1);
                _response.Headers[name] = values;
            }

            values.Add(value);
        }

        // ================================================================
        //  Кодировка тела
        // ================================================================

        private static Encoding? DetectEncoding(Dictionary<string, List<string>> headers)
        {
            if (!headers.TryGetValue("Content-Type", out var values) || values.Count == 0)
            {
                return null;
            }

            foreach (var contentType in values)
            {
                var encoding = TryExtractCharset(contentType);
                if (encoding != null)
                {
                    return encoding;
                }
            }

            return null;
        }

        private static Encoding? TryExtractCharset(string contentType)
        {
            const string charsetToken = "charset=";
            var index = contentType.IndexOf(charsetToken, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var start = index + charsetToken.Length;
            var end = contentType.IndexOf(';', start);
            var charset = (end < 0 ? contentType.Substring(start) : contentType.Substring(start, end - start)).Trim().Trim('"', '\'');

            if (charset.Length == 0)
            {
                return null;
            }

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                Logger.Warning(Category, $"Неизвестная кодировка: '{charset}'.");
                return null;
            }
        }

        // ================================================================
        //  Освобождение
        // ================================================================

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseParser));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _selfHandle.Dispose();
            _headerCallbackHandle.Dispose();
            _writeCallbackHandle.Dispose();
            _bodyStream.Dispose();
        }
    }
}