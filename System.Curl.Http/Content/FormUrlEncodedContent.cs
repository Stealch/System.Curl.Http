#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-сообщения в формате <c>application/x-www-form-urlencoded</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.FormUrlEncodedContent</c>.
    /// </para>
    /// <para>
    /// Пары «ключ-значение» URL-кодируются и соединяются через <c>&amp;</c>.
    /// Пробелы кодируются как <c>+</c> (как требует формат), остальные
    /// не-ASCII символы — как <c>%XX</c> в UTF-8.
    /// </para>
    /// </remarks>
    public class FormUrlEncodedContent : ByteArrayContent
    {
        private const string MediaType = "application/x-www-form-urlencoded";

        /// <summary>
        /// Создаёт тело из коллекции пар «ключ-значение».
        /// </summary>
        /// <param name="nameValueCollection">Пары для кодирования.</param>
        public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
            : base(EncodeContent(nameValueCollection))
        {
            Headers.ContentType = new MediaTypeHeaderValue(MediaType);
        }

        // ================================================================
        //  Кодирование
        // ================================================================

        private static byte[] EncodeContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            if (nameValueCollection == null)
            {
                throw new ArgumentNullException(nameof(nameValueCollection));
            }

            var sb = new StringBuilder();
            var first = true;

            foreach (var pair in nameValueCollection)
            {
                if (pair.Key == null)
                {
                    throw new ArgumentException("Ключ не может быть null.", nameof(nameValueCollection));
                }

                if (!first)
                {
                    sb.Append('&');
                }

                first = false;

                sb.Append(Uri.EscapeDataString(pair.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(pair.Value ?? string.Empty));
            }

            // URL-encoded строка содержит только ASCII, поэтому кодировка не важна.
            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}