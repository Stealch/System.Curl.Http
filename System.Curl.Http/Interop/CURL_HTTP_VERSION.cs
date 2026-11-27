namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Версии HTTP для опции <c>CURLOPT_HTTP_VERSION</c>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    internal static class CURL_HTTP_VERSION
    {
        /// <summary><c>CURL_HTTP_VERSION_NONE</c> — libcurl выберет сам.</summary>
        public const long NONE = 0L;

        /// <summary><c>CURL_HTTP_VERSION_1_0</c> — HTTP/1.0.</summary>
        public const long V1_0 = 1L;

        /// <summary><c>CURL_HTTP_VERSION_1_1</c> — HTTP/1.1.</summary>
        public const long V1_1 = 2L;

        /// <summary><c>CURL_HTTP_VERSION_2_0</c> — HTTP/2 (prior knowledge, без ALPN).</summary>
        public const long V2_0 = 3L;

        /// <summary><c>CURL_HTTP_VERSION_2TLS</c> — HTTP/2 через ALPN для HTTPS, иначе HTTP/1.1.</summary>
        public const long V2TLS = 4L;

        /// <summary><c>CURL_HTTP_VERSION_2_PRIOR_KNOWLEDGE</c> — только HTTP/2, без отката.</summary>
        public const long V2_PRIOR_KNOWLEDGE = 5L;

        /// <summary><c>CURL_HTTP_VERSION_3</c> — HTTP/3 с откатом.</summary>
        public const long V3 = 30L;

        /// <summary><c>CURL_HTTP_VERSION_3ONLY</c> — только HTTP/3, без отката.</summary>
        public const long V3ONLY = 31L;
    }
}