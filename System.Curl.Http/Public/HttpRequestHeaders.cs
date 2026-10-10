#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace System.Curl.Http
{
    /// <summary>
    /// Коллекция заголовков HTTP-запроса. Предоставляет типизированный доступ
    /// к стандартным заголовкам запроса и хранит нестандартные заголовки.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Типы значений заголовков — BCL-овские (<c>System.Net.Http.Headers.*</c>).
    /// Это осознанное решение: парсеры заголовков не привязаны к транспортному
    /// стеку и не зависят от TLS или сокетов.
    /// </para>
    /// <para>
    /// Пользователь, создающий заголовки вручную, должен добавить
    /// <c>using System.Net.Http.Headers;</c> в дополнение к <c>using System.Curl.Http;</c>.
    /// </para>
    /// </remarks>
    public sealed class HttpRequestHeaders : HttpHeaders
    {
        private const string AcceptName = "Accept";
        private const string AcceptCharsetName = "Accept-Charset";
        private const string AcceptEncodingName = "Accept-Encoding";
        private const string AcceptLanguageName = "Accept-Language";
        private const string AuthorizationName = "Authorization";
        private const string CacheControlName = "Cache-Control";
        private const string ConnectionName = "Connection";
        private const string DateName = "Date";
        private const string ExpectName = "Expect";
        private const string ExpectContinueName = "Expect-Continue";
        private const string FromName = "From";
        private const string HostName = "Host";
        private const string IfMatchName = "If-Match";
        private const string IfModifiedSinceName = "If-Modified-Since";
        private const string IfNoneMatchName = "If-None-Match";
        private const string IfRangeName = "If-Range";
        private const string IfUnmodifiedSinceName = "If-Unmodified-Since";
        private const string MaxForwardsName = "Max-Forwards";
        private const string PragmaName = "Pragma";
        private const string ProxyAuthorizationName = "Proxy-Authorization";
        private const string RangeName = "Range";
        private const string RefererName = "Referer";
        private const string TEName = "TE";
        private const string TrailerName = "Trailer";
        private const string TransferEncodingName = "Transfer-Encoding";
        private const string UpgradeName = "Upgrade";
        private const string UserAgentName = "User-Agent";
        private const string ViaName = "Via";
        private const string WarningName = "Warning";

        /// <summary>
        /// Создаёт пустую коллекцию заголовков запроса.
        /// </summary>
        public HttpRequestHeaders()
        {
            Accept = new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(this, AcceptName);
            AcceptCharset = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(this, AcceptCharsetName);
            AcceptEncoding = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(this, AcceptEncodingName);
            AcceptLanguage = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(this, AcceptLanguageName);
            Connection = new HttpHeaderValueCollection<string>(this, ConnectionName);
            Expect = new HttpHeaderValueCollection<string>(this, ExpectName);
            IfMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>(this, IfMatchName);
            IfNoneMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>(this, IfNoneMatchName);
            Pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(this, PragmaName);
            TE = new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(this, TEName);
            Trailer = new HttpHeaderValueCollection<string>(this, TrailerName);
            TransferEncoding = new HttpHeaderValueCollection<TransferCodingHeaderValue>(this, TransferEncodingName);
            Upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(this, UpgradeName);
            UserAgent = new HttpHeaderValueCollection<ProductInfoHeaderValue>(this, UserAgentName);
            Via = new HttpHeaderValueCollection<ViaHeaderValue>(this, ViaName);
            Warning = new HttpHeaderValueCollection<WarningHeaderValue>(this, WarningName);
        }

        // ================================================================
        //  Коллекции значений (множественные заголовки)
        // ================================================================

        /// <summary>Accept.</summary>
        public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept { get; }

        /// <summary>Accept-Charset.</summary>
        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset { get; }

        /// <summary>Accept-Encoding.</summary>
        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding { get; }

        /// <summary>Accept-Language.</summary>
        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage { get; }

        /// <summary>Connection.</summary>
        public HttpHeaderValueCollection<string> Connection { get; }

        /// <summary>Expect.</summary>
        public HttpHeaderValueCollection<string> Expect { get; }

        /// <summary>If-Match.</summary>
        public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch { get; }

        /// <summary>If-None-Match.</summary>
        public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch { get; }

        /// <summary>Pragma.</summary>
        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma { get; }

        /// <summary>TE.</summary>
        public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE { get; }

        /// <summary>Trailer.</summary>
        public HttpHeaderValueCollection<string> Trailer { get; }

        /// <summary>Transfer-Encoding.</summary>
        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding { get; }

        /// <summary>Upgrade.</summary>
        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade { get; }

        /// <summary>User-Agent.</summary>
        public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent { get; }

        /// <summary>Via.</summary>
        public HttpHeaderValueCollection<ViaHeaderValue> Via { get; }

        /// <summary>Warning.</summary>
        public HttpHeaderValueCollection<WarningHeaderValue> Warning { get; }

        // ================================================================
        //  Одиночные значения (типизированные свойства)
        // ================================================================

        /// <summary>Authorization.</summary>
        public AuthenticationHeaderValue? Authorization
        {
            get => ParseAuthentication(GetSingleValue(AuthorizationName));
            set => SetSingleValue(AuthorizationName, value?.ToString());
        }

        /// <summary>Cache-Control.</summary>
        public CacheControlHeaderValue? CacheControl
        {
            get => ParseCacheControl(GetSingleValue(CacheControlName));
            set => SetSingleValue(CacheControlName, value?.ToString());
        }

        /// <summary>Date.</summary>
        public DateTimeOffset? Date
        {
            get => ParseDate(GetSingleValue(DateName));
            set => SetSingleValue(DateName, value?.ToString("R"));
        }

        /// <summary>Expect-Continue (специфичный для .NET Framework).</summary>
        public bool? ExpectContinue
        {
            get
            {
                var value = GetSingleValue(ExpectContinueName);
                if (value == null)
                {
                    return null;
                }

                if (bool.TryParse(value, out var result))
                {
                    return result;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    SetSingleValue(ExpectContinueName, value.Value ? "true" : "false");
                }
                else
                {
                    SetSingleValue(ExpectContinueName, null);
                }
            }
        }

        /// <summary>From.</summary>
        public string? From
        {
            get => GetSingleValue(FromName);
            set => SetSingleValue(FromName, value);
        }

        /// <summary>Host.</summary>
        public string? Host
        {
            get => GetSingleValue(HostName);
            set => SetSingleValue(HostName, value);
        }

        /// <summary>If-Modified-Since.</summary>
        public DateTimeOffset? IfModifiedSince
        {
            get => ParseDate(GetSingleValue(IfModifiedSinceName));
            set => SetSingleValue(IfModifiedSinceName, value?.ToString("R"));
        }

        /// <summary>If-Range.</summary>
        public RangeConditionHeaderValue? IfRange
        {
            get => ParseRangeCondition(GetSingleValue(IfRangeName));
            set => SetSingleValue(IfRangeName, value?.ToString());
        }

        /// <summary>If-Unmodified-Since.</summary>
        public DateTimeOffset? IfUnmodifiedSince
        {
            get => ParseDate(GetSingleValue(IfUnmodifiedSinceName));
            set => SetSingleValue(IfUnmodifiedSinceName, value?.ToString("R"));
        }

        /// <summary>Max-Forwards.</summary>
        public int? MaxForwards
        {
            get
            {
                var value = GetSingleValue(MaxForwardsName);
                if (value == null)
                {
                    return null;
                }

                return int.TryParse(value, out var result) ? result : (int?)null;
            }
            set => SetSingleValue(MaxForwardsName, value?.ToString());
        }

        /// <summary>Proxy-Authorization.</summary>
        public AuthenticationHeaderValue? ProxyAuthorization
        {
            get => ParseAuthentication(GetSingleValue(ProxyAuthorizationName));
            set => SetSingleValue(ProxyAuthorizationName, value?.ToString());
        }

        /// <summary>Range.</summary>
        public RangeHeaderValue? Range
        {
            get => ParseRange(GetSingleValue(RangeName));
            set => SetSingleValue(RangeName, value?.ToString());
        }

        /// <summary>Referer.</summary>
        public Uri? Referer
        {
            get
            {
                var value = GetSingleValue(RefererName);
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
            }
            set => SetSingleValue(RefererName, value?.ToString());
        }

        // ================================================================
        //  Парсеры BCL-овских типов
        // ================================================================

        private static AuthenticationHeaderValue? ParseAuthentication(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return AuthenticationHeaderValue.TryParse(value, out var parsed) ? parsed : null;
        }

        private static CacheControlHeaderValue? ParseCacheControl(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return CacheControlHeaderValue.TryParse(value, out var parsed) ? parsed : null;
        }

        private static RangeConditionHeaderValue? ParseRangeCondition(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return RangeConditionHeaderValue.TryParse(value, out var parsed) ? parsed : null;
        }

        private static RangeHeaderValue? ParseRange(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return RangeHeaderValue.TryParse(value, out var parsed) ? parsed : null;
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