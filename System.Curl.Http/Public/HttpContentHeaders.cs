#nullable enable

using System;
using System.Net.Http.Headers;

namespace System.Curl.Http
{
    /// <summary>
    /// Коллекция заголовков тела HTTP-сообщения. Доступна через
    /// <c>HttpContent.Headers</c> как для запросов, так и для ответов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Типы значений заголовков — BCL-овские (<c>System.Net.Http.Headers.*</c>).
    /// </para>
    /// <para>
    /// Эти заголовки описывают тело сообщения: тип содержимого, длину,
    /// кодировку, язык, диапазон, хэш и т.п. Они не относятся ни к запросу,
    /// ни к ответу в отдельности — поэтому вынесены в отдельный класс.
    /// </para>
    /// </remarks>
    public sealed class HttpContentHeaders : HttpHeaders
    {
        private const string AllowName = "Allow";
        private const string ContentDispositionName = "Content-Disposition";
        private const string ContentEncodingName = "Content-Encoding";
        private const string ContentLanguageName = "Content-Language";
        private const string ContentLengthName = "Content-Length";
        private const string ContentLocationName = "Content-Location";
        private const string ContentMD5Name = "Content-MD5";
        private const string ContentRangeName = "Content-Range";
        private const string ContentTypeName = "Content-Type";
        private const string ExpiresName = "Expires";
        private const string LastModifiedName = "Last-Modified";

        private readonly HttpHeaderValueCollection<string> _allow;
        private readonly HttpHeaderValueCollection<string> _contentEncoding;
        private readonly HttpHeaderValueCollection<string> _contentLanguage;

        /// <summary>
        /// Создаёт пустую коллекцию заголовков тела.
        /// </summary>
        public HttpContentHeaders()
        {
            _allow = new HttpHeaderValueCollection<string>(this, AllowName);
            _contentEncoding = new HttpHeaderValueCollection<string>(this, ContentEncodingName);
            _contentLanguage = new HttpHeaderValueCollection<string>(this, ContentLanguageName);
        }

        // ================================================================
        //  Коллекции значений (множественные заголовки)
        // ================================================================

        /// <summary>Allow.</summary>
        public HttpHeaderValueCollection<string> Allow => _allow;

        /// <summary>Content-Encoding.</summary>
        public HttpHeaderValueCollection<string> ContentEncoding => _contentEncoding;

        /// <summary>Content-Language.</summary>
        public HttpHeaderValueCollection<string> ContentLanguage => _contentLanguage;

        // ================================================================
        //  Одиночные значения (типизированные свойства)
        // ================================================================

        /// <summary>Content-Disposition.</summary>
        public ContentDispositionHeaderValue? ContentDisposition
        {
            get
            {
                var value = GetSingleValue(ContentDispositionName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return ContentDispositionHeaderValue.TryParse(value, out var parsed) ? parsed : null;
            }
            set => SetSingleValue(ContentDispositionName, value?.ToString());
        }

        /// <summary>Content-Length.</summary>
        public long? ContentLength
        {
            get
            {
                var value = GetSingleValue(ContentLengthName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return long.TryParse(value, out var result) ? result : (long?)null;
            }
            set
            {
                if (value.HasValue && value.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Content-Length не может быть отрицательным.");
                }

                SetSingleValue(ContentLengthName, value?.ToString());
            }
        }

        /// <summary>Content-Location.</summary>
        public Uri? ContentLocation
        {
            get
            {
                var value = GetSingleValue(ContentLocationName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
            }
            set => SetSingleValue(ContentLocationName, value?.ToString());
        }

        /// <summary>
        /// Content-MD5. Возвращает декодированный base64-массив байт.
        /// </summary>
        public byte[]? ContentMD5
        {
            get
            {
                var value = GetSingleValue(ContentMD5Name);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                try
                {
                    return Convert.FromBase64String(value);
                }
                catch (FormatException)
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    SetSingleValue(ContentMD5Name, null);
                }
                else
                {
                    SetSingleValue(ContentMD5Name, Convert.ToBase64String(value));
                }
            }
        }

        /// <summary>Content-Range.</summary>
        public ContentRangeHeaderValue? ContentRange
        {
            get
            {
                var value = GetSingleValue(ContentRangeName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return ContentRangeHeaderValue.TryParse(value, out var parsed) ? parsed : null;
            }
            set => SetSingleValue(ContentRangeName, value?.ToString());
        }

        /// <summary>Content-Type.</summary>
        public MediaTypeHeaderValue? ContentType
        {
            get
            {
                var value = GetSingleValue(ContentTypeName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return MediaTypeHeaderValue.TryParse(value, out var parsed) ? parsed : null;
            }
            set => SetSingleValue(ContentTypeName, value?.ToString());
        }

        /// <summary>Expires.</summary>
        public DateTimeOffset? Expires
        {
            get
            {
                var value = GetSingleValue(ExpiresName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return DateTimeOffset.TryParse(value, out var result) ? result : (DateTimeOffset?)null;
            }
            set => SetSingleValue(ExpiresName, value?.ToString("R"));
        }

        /// <summary>Last-Modified.</summary>
        public DateTimeOffset? LastModified
        {
            get
            {
                var value = GetSingleValue(LastModifiedName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return DateTimeOffset.TryParse(value, out var result) ? result : (DateTimeOffset?)null;
            }
            set => SetSingleValue(LastModifiedName, value?.ToString("R"));
        }
    }
}