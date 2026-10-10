#nullable enable

using System;
using System.Net.Http.Headers;
using System.Text;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-сообщения на основе строки.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.StringContent</c>.
    /// </para>
    /// <para>
    /// Строка конвертируется в байты указанной кодировкой при создании.
    /// <c>Content-Type</c> устанавливается в <c>text/plain</c> с указанием
    /// кодировки, если не передан явный media type.
    /// </para>
    /// </remarks>
    public class StringContent : ByteArrayContent
    {
        private const string DefaultMediaType = "text/plain";

        /// <summary>
        /// Создаёт тело из строки в кодировке UTF-8 с типом <c>text/plain</c>.
        /// </summary>
        public StringContent(string content)
            : this(content, Encoding.UTF8, DefaultMediaType)
        {
        }

        /// <summary>
        /// Создаёт тело из строки в указанной кодировке с типом <c>text/plain</c>.
        /// </summary>
        public StringContent(string content, Encoding encoding)
            : this(content, encoding, DefaultMediaType)
        {
        }

        /// <summary>
        /// Создаёт тело из строки в кодировке UTF-8 с указанным media type.
        /// </summary>
        public StringContent(string content, string mediaType)
            : this(content, Encoding.UTF8, mediaType)
        {
        }

        /// <summary>
        /// Создаёт тело из строки в указанной кодировке с указанным media type.
        /// </summary>
        /// <param name="content">Тело сообщения.</param>
        /// <param name="encoding">Кодировка для конвертации строки в байты.</param>
        /// <param name="mediaType">Значение <c>Content-Type</c> без параметра <c>charset</c>.</param>
        public StringContent(string content, Encoding encoding, string mediaType)
            : base(GetContentByteArray(content, encoding))
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            SetContentTypeHeader(mediaType, encoding);
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private static byte[] GetContentByteArray(string content, Encoding encoding)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return encoding.GetBytes(content);
        }

        private void SetContentTypeHeader(string mediaType, Encoding encoding)
        {
            var contentType = new MediaTypeHeaderValue(mediaType)
            {
                CharSet = $"\"{encoding.WebName}\"",
            };

            Headers.ContentType = contentType;
        }
    }
}