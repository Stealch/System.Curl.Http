#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-сообщения в формате <c>multipart/form-data</c> (RFC 7578).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.MultipartFormDataContent</c>.
    /// </para>
    /// <para>
    /// Формат используется для отправки форм с файлами, а также для
    /// составных тел с бинарным содержимым.
    /// </para>
    /// </remarks>
    public class MultipartFormDataContent : MultipartContent
    {
        private const string DefaultSubtype = "form-data";
        private const string ContentDispositionName = "Content-Disposition";

        /// <summary>
        /// Создаёт тело с автоматически сгенерированной границей.
        /// </summary>
        public MultipartFormDataContent()
            : base(DefaultSubtype)
        {
        }

        /// <summary>
        /// Создаёт тело с указанной границей.
        /// </summary>
        /// <param name="boundary">Строка-граница между частями.</param>
        public MultipartFormDataContent(string boundary)
            : base(DefaultSubtype, boundary)
        {
        }

        // ================================================================
        //  Добавление частей
        // ================================================================

        /// <summary>
        /// Добавляет часть без имени.
        /// </summary>
        public override void Add(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            base.Add(content);
        }

        /// <summary>
        /// Добавляет часть с именем поля формы.
        /// </summary>
        public void Add(HttpContent content, string name)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя поля не может быть пустым.", nameof(name));
            }

            AddContentDisposition(content, name, null);
            base.Add(content);
        }

        /// <summary>
        /// Добавляет часть с именем поля и именем файла.
        /// </summary>
        public void Add(HttpContent content, string name, string fileName)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя поля не может быть пустым.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Имя файла не может быть пустым.", nameof(fileName));
            }

            AddContentDisposition(content, name, fileName);
            base.Add(content);
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private static void AddContentDisposition(HttpContent content, string name, string? fileName)
        {
            // Content-Disposition устанавливается на часть.
            // Если у части уже есть Content-Disposition — не перезаписываем.
            if (content.Headers.ContentDisposition == null)
            {
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DefaultSubtype)
                {
                    Name = QuoteIfNeeded(name),
                    FileName = fileName != null ? QuoteIfNeeded(fileName) : null,
                };
            }
        }

        private static string QuoteIfNeeded(string value)
        {
            // ContentDispositionHeaderValue.Name и .FileName ожидают строку в кавычках
            // для корректной сериализации. Если кавычек нет — добавляем.
            if (value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
            {
                return value;
            }

            return "\"" + value + "\"";
        }
    }

    /// <summary>
    /// Базовый класс для тел в формате <c>multipart/*</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.MultipartContent</c>.
    /// </para>
    /// <para>
    /// Управляет границей между частями и их сериализацией.
    /// </para>
    /// </remarks>
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        private const string DefaultSubtype = "mixed";
        private const int DefaultBoundaryLength = 32;

        private readonly List<HttpContent> _parts = new List<HttpContent>();
        private readonly string _boundary;
        private readonly string _subtype;

        /// <summary>
        /// Создаёт тело с указанным подтипом и автоматически сгенерированной границей.
        /// </summary>
        public MultipartContent(string subtype)
            : this(subtype, GenerateBoundary())
        {
        }

        /// <summary>
        /// Создаёт тело с указанным подтипом и границей.
        /// </summary>
        public MultipartContent(string subtype, string boundary)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                throw new ArgumentException("Подтип не может быть пустым.", nameof(subtype));
            }

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new ArgumentException("Граница не может быть пустой.", nameof(boundary));
            }

            _subtype = subtype;
            _boundary = boundary;

            var contentType = new MediaTypeHeaderValue("multipart/" + subtype)
            {
                Parameters = { new NameValueHeaderValue("boundary", "\"" + boundary + "\"") },
            };

            Headers.ContentType = contentType;
        }

        /// <summary>
        /// Граница между частями.
        /// </summary>
        public string Boundary => _boundary;

        /// <summary>
        /// Добавляет часть в тело.
        /// </summary>
        public virtual void Add(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            _parts.Add(content);
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "\r\n");

            foreach (var part in _parts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // --boundary\r\n
                await stream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length, cancellationToken).ConfigureAwait(false);

                // Заголовки части (Content-Disposition, Content-Type и т.д.)
                var partHeaders = SerializePartHeaders(part);
                var partHeadersBytes = Encoding.UTF8.GetBytes(partHeaders);
                await stream.WriteAsync(partHeadersBytes, 0, partHeadersBytes.Length, cancellationToken).ConfigureAwait(false);

                // \r\n (разделитель между заголовками и телом)
                await stream.WriteAsync(new byte[] { 0x0D, 0x0A }, 0, 2, cancellationToken).ConfigureAwait(false);

                // Тело части
                await part.CopyToAsync(stream, context, cancellationToken).ConfigureAwait(false);

                // \r\n после тела
                await stream.WriteAsync(new byte[] { 0x0D, 0x0A }, 0, 2, cancellationToken).ConfigureAwait(false);
            }

            // Финальная граница: --boundary--\r\n
            var closingBoundary = Encoding.ASCII.GetBytes("--" + _boundary + "--\r\n");
            await stream.WriteAsync(closingBoundary, 0, closingBoundary.Length, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            // Попробуем посчитать точную длину, если все части знают свою.
            length = 0;

            var boundaryLength = Encoding.ASCII.GetByteCount("--" + _boundary + "\r\n");
            var closingLength = Encoding.ASCII.GetByteCount("--" + _boundary + "--\r\n");

            long total = 0;

            foreach (var part in _parts)
            {
                var partHeaders = SerializePartHeaders(part);
                total += boundaryLength;
                total += Encoding.UTF8.GetByteCount(partHeaders);
                total += 2; // \r\n между заголовками и телом

                var partLength = part.Headers.ContentLength;
                if (partLength == null)
                {
                    // Не удалось определить длину части — вся длина неизвестна.
                    length = 0;
                    return false;
                }

                total += partLength.Value;
                total += 2; // \r\n после тела
            }

            total += closingLength;

            length = total;
            return true;
        }

        /// <summary>
        /// Освобождает все части.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var part in _parts)
                {
                    part.Dispose();
                }

                _parts.Clear();
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public IEnumerator<HttpContent> GetEnumerator()
        {
            return _parts.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private static string GenerateBoundary()
        {
            // GUID без дефисов, плюс префикс, чтобы граница была уникальной
            // и не встречалась в теле. Длина ~40 символов достаточна.
            var guid = Guid.NewGuid().ToString("N");
            return "----SystemCurlHttpBoundary" + guid.Substring(0, DefaultBoundaryLength);
        }

        private static string SerializePartHeaders(HttpContent part)
        {
            var sb = new StringBuilder(256);

            foreach (var header in part.Headers)
            {
                foreach (var value in header.Value)
                {
                    sb.Append(header.Key);
                    sb.Append(": ");
                    sb.Append(value);
                    sb.Append("\r\n");
                }
            }

            return sb.ToString();
        }
    }
}