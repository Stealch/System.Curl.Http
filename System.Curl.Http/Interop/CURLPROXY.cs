namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Типы прокси-серверов для опции <c>CURLOPT_PROXYTYPE</c>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Обычно <c>CURLOPT_PROXYTYPE</c> не требуется: libcurl определяет тип
    /// из схемы URL прокси (<c>http://</c>, <c>socks5://</c> и т.д.).
    /// Опция нужна только для нестандартных случаев.
    /// </para>
    /// </remarks>
    internal static class CURLPROXY
    {
        /// <summary><c>CURLPROXY_HTTP</c> — HTTP-прокси (по умолчанию).</summary>
        public const long HTTP = 0L;

        /// <summary><c>CURLPROXY_HTTP_1_0</c> — HTTP/1.0 CONNECT.</summary>
        public const long HTTP_1_0 = 1L;

        /// <summary><c>CURLPROXY_HTTPS</c> — HTTPS-прокси с HTTP/1.</summary>
        public const long HTTPS = 2L;

        /// <summary><c>CURLPROXY_HTTPS2</c> — HTTPS-прокси с попыткой HTTP/2. Добавлен в 8.2.0.</summary>
        public const long HTTPS2 = 3L;

        /// <summary><c>CURLPROXY_SOCKS4</c> — SOCKS4.</summary>
        public const long SOCKS4 = 4L;

        /// <summary><c>CURLPROXY_SOCKS5</c> — SOCKS5.</summary>
        public const long SOCKS5 = 5L;

        /// <summary><c>CURLPROXY_SOCKS4A</c> — SOCKS4a (с разрешением DNS на прокси).</summary>
        public const long SOCKS4A = 6L;

        /// <summary><c>CURLPROXY_SOCKS5_HOSTNAME</c> — SOCKS5 с разрешением DNS на прокси.</summary>
        public const long SOCKS5_HOSTNAME = 7L;
    }
}