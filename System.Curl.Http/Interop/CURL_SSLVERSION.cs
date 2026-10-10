namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Версии TLS для <c>CURLOPT_SSLVERSION</c>, <c>CURLOPT_PROXY_SSLVERSION</c>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Младшие 16 бит — минимальная версия. Старшие 16 бит — максимальная
    /// (через <c>CURL_SSLVERSION_MAX_*</c>).
    /// </para>
    /// </remarks>
    internal static class CURL_SSLVERSION
    {
        /// <summary><c>CURL_SSLVERSION_DEFAULT</c> — libcurl выберет сам.</summary>
        public const long DEFAULT = 0L;

        /// <summary><c>CURL_SSLVERSION_TLSv1</c> — TLS 1.x.</summary>
        public const long TLSv1 = 1L;

        /// <summary><c>CURL_SSLVERSION_SSLv2</c> — SSL 2 (устарело, небезопасно).</summary>
        public const long SSLv2 = 2L;

        /// <summary><c>CURL_SSLVERSION_SSLv3</c> — SSL 3 (устарело, небезопасно).</summary>
        public const long SSLv3 = 3L;

        /// <summary><c>CURL_SSLVERSION_TLSv1_0</c> — TLS 1.0.</summary>
        public const long TLSv1_0 = 4L;

        /// <summary><c>CURL_SSLVERSION_TLSv1_1</c> — TLS 1.1.</summary>
        public const long TLSv1_1 = 5L;

        /// <summary><c>CURL_SSLVERSION_TLSv1_2</c> — TLS 1.2.</summary>
        public const long TLSv1_2 = 6L;

        /// <summary><c>CURL_SSLVERSION_TLSv1_3</c> — TLS 1.3.</summary>
        public const long TLSv1_3 = 7L;

        // ================================================================
        //  Максимальные версии (старшие 16 бит)
        // ================================================================

        /// <summary><c>CURL_SSLVERSION_MAX_NONE</c> — без максимума.</summary>
        public const long MAX_NONE = 0L;

        /// <summary><c>CURL_SSLVERSION_MAX_DEFAULT</c> — максимум по умолчанию (TLS 1.x).</summary>
        public const long MAX_DEFAULT = TLSv1 << 16;

        /// <summary><c>CURL_SSLVERSION_MAX_TLSv1_0</c> — максимум TLS 1.0.</summary>
        public const long MAX_TLSv1_0 = TLSv1_0 << 16;

        /// <summary><c>CURL_SSLVERSION_MAX_TLSv1_1</c> — максимум TLS 1.1.</summary>
        public const long MAX_TLSv1_1 = TLSv1_1 << 16;

        /// <summary><c>CURL_SSLVERSION_MAX_TLSv1_2</c> — максимум TLS 1.2.</summary>
        public const long MAX_TLSv1_2 = TLSv1_2 << 16;

        /// <summary><c>CURL_SSLVERSION_MAX_TLSv1_3</c> — максимум TLS 1.3.</summary>
        public const long MAX_TLSv1_3 = TLSv1_3 << 16;
    }
}