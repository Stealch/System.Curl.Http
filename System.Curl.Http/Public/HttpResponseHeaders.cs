#nullable enable

using System;
using System.Net.Http.Headers;

namespace System.Curl.Http
{
    /// <summary>
    /// Коллекция заголовков HTTP-ответа. Предоставляет типизированный доступ
    /// к стандартным заголовкам ответа и хранит нестандартные заголовки.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Типы значений заголовков — BCL-овские (<c>System.Net.Http.Headers.*</c>).
    /// </para>
    /// <para>
    /// Заголовки, относящиеся к телу ответа (<c>Content-Type</c>,
    /// <c>Content-Length</c>, <c>Content-Encoding</c> и т.п.), хранятся
    /// не здесь, а в <see cref="HttpContentHeaders"/> — доступны через
    /// <c>HttpResponseMessage.Content.Headers</c>.
    /// </para>
    /// </remarks>
    public sealed class HttpResponseHeaders : HttpHeaders
    {
        private const string AgeName = "Age";
        private const string CacheControlName = "Cache-Control";
        private const string ConnectionName = "Connection";
        private const string DateName = "Date";
        private const string ETagName = "ETag";
        private const string LocationName = "Location";
        private const string PragmaName = "Pragma";
        private const string ProxyAuthenticateName = "Proxy-Authenticate";
        private const string RetryAfterName = "Retry-After";
        private const string ServerName = "Server";
        private const string TrailerName = "Trailer";
        private const string TransferEncodingName = "Transfer-Encoding";
        private const string UpgradeName = "Upgrade";
        private const string VaryName = "Vary";
        private const string ViaName = "Via";
        private const string WarningName = "Warning";
        private const string WwwAuthenticateName = "WWW-Authenticate";

        private readonly HttpHeaderValueCollection<string> _connection;
        private readonly HttpHeaderValueCollection<NameValueHeaderValue> _pragma;
        private readonly HttpHeaderValueCollection<AuthenticationHeaderValue> _proxyAuthenticate;
        private readonly HttpHeaderValueCollection<ProductInfoHeaderValue> _server;
        private readonly HttpHeaderValueCollection<string> _trailer;
        private readonly HttpHeaderValueCollection<TransferCodingHeaderValue> _transferEncoding;
        private readonly HttpHeaderValueCollection<ProductHeaderValue> _upgrade;
        private readonly HttpHeaderValueCollection<string> _vary;
        private readonly HttpHeaderValueCollection<ViaHeaderValue> _via;
        private readonly HttpHeaderValueCollection<WarningHeaderValue> _warning;
        private readonly HttpHeaderValueCollection<AuthenticationHeaderValue> _wwwAuthenticate;

        /// <summary>
        /// Создаёт пустую коллекцию заголовков ответа.
        /// </summary>
        public HttpResponseHeaders()
        {
            _connection = new HttpHeaderValueCollection<string>(this, ConnectionName);
            _pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(this, PragmaName);
            _proxyAuthenticate = new HttpHeaderValueCollection<AuthenticationHeaderValue>(this, ProxyAuthenticateName);
            _server = new HttpHeaderValueCollection<ProductInfoHeaderValue>(this, ServerName);
            _trailer = new HttpHeaderValueCollection<string>(this, TrailerName);
            _transferEncoding = new HttpHeaderValueCollection<TransferCodingHeaderValue>(this, TransferEncodingName);
            _upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(this, UpgradeName);
            _vary = new HttpHeaderValueCollection<string>(this, VaryName);
            _via = new HttpHeaderValueCollection<ViaHeaderValue>(this, ViaName);
            _warning = new HttpHeaderValueCollection<WarningHeaderValue>(this, WarningName);
            _wwwAuthenticate = new HttpHeaderValueCollection<AuthenticationHeaderValue>(this, WwwAuthenticateName);
        }

        // ================================================================
        //  Коллекции значений (множественные заголовки)
        // ================================================================

        /// <summary>Connection.</summary>
        public HttpHeaderValueCollection<string> Connection => _connection;

        /// <summary>Pragma.</summary>
        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma => _pragma;

        /// <summary>Proxy-Authenticate.</summary>
        public HttpHeaderValueCollection<AuthenticationHeaderValue> ProxyAuthenticate => _proxyAuthenticate;

        /// <summary>Server.</summary>
        public HttpHeaderValueCollection<ProductInfoHeaderValue> Server => _server;

        /// <summary>Trailer.</summary>
        public HttpHeaderValueCollection<string> Trailer => _trailer;

        /// <summary>Transfer-Encoding.</summary>
        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding => _transferEncoding;

        /// <summary>Upgrade.</summary>
        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade => _upgrade;

        /// <summary>Vary.</summary>
        public HttpHeaderValueCollection<string> Vary => _vary;

        /// <summary>Via.</summary>
        public HttpHeaderValueCollection<ViaHeaderValue> Via => _via;

        /// <summary>Warning.</summary>
        public HttpHeaderValueCollection<WarningHeaderValue> Warning => _warning;

        /// <summary>WWW-Authenticate.</summary>
        public HttpHeaderValueCollection<AuthenticationHeaderValue> WwwAuthenticate => _wwwAuthenticate;

        // ================================================================
        //  Одиночные значения (типизированные свойства)
        // ================================================================

        /// <summary>Age.</summary>
        public TimeSpan? Age
        {
            get
            {
                var value = GetSingleValue(AgeName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return long.TryParse(value, out var seconds)
                    ? TimeSpan.FromSeconds(seconds)
                    : (TimeSpan?)null;
            }
            set => SetSingleValue(AgeName, value.HasValue ? ((long)value.Value.TotalSeconds).ToString() : null);
        }

        /// <summary>Cache-Control.</summary>
        public CacheControlHeaderValue? CacheControl
        {
            get => ParseCacheControl(GetSingleValue(CacheControlName));
            set => SetSingleValue(CacheControlName, value?.ToString());
        }

        /// <summary>
        /// Connection: close. <see langword="true"/>, если сервер запросил закрытие соединения.
        /// </summary>
        public bool? ConnectionClose
        {
            get
            {
                var value = GetSingleValue(ConnectionName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return string.Equals(value, "close", StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                if (value == true)
                {
                    SetSingleValue(ConnectionName, "close");
                }
                else
                {
                    SetSingleValue(ConnectionName, null);
                }
            }
        }

        /// <summary>Date.</summary>
        public DateTimeOffset? Date
        {
            get => ParseDate(GetSingleValue(DateName));
            set => SetSingleValue(DateName, value?.ToString("R"));
        }

        /// <summary>ETag.</summary>
        public EntityTagHeaderValue? ETag
        {
            get
            {
                var value = GetSingleValue(ETagName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return EntityTagHeaderValue.TryParse(value, out var parsed) ? parsed : null;
            }
            set => SetSingleValue(ETagName, value?.ToString());
        }

        /// <summary>Location.</summary>
        public Uri? Location
        {
            get
            {
                var value = GetSingleValue(LocationName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
            }
            set => SetSingleValue(LocationName, value?.ToString());
        }

        /// <summary>Retry-After.</summary>
        public RetryConditionHeaderValue? RetryAfter
        {
            get
            {
                var value = GetSingleValue(RetryAfterName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return RetryConditionHeaderValue.TryParse(value, out var parsed) ? parsed : null;
            }
            set => SetSingleValue(RetryAfterName, value?.ToString());
        }

        // ================================================================
        //  Парсеры BCL-овских типов
        // ================================================================

        private static CacheControlHeaderValue? ParseCacheControl(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return CacheControlHeaderValue.TryParse(value, out var parsed) ? parsed : null;
        }

        private static DateTimeOffset? ParseDate(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return DateTimeOffset.TryParse(value, out var result) ? result : (DateTimeOffset?)null;
        }
    }
}