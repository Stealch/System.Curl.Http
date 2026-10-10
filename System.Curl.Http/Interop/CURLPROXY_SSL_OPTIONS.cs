namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Флаги для <c>CURLOPT_PROXY_SSL_OPTIONS</c> и <c>CURLOPT_SSL_OPTIONS</c>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Битовая маска. Комбинируется через <c>|</c>.
    /// </para>
    /// </remarks>
    internal static class CURLSSLOPT
    {
        /// <summary><c>CURLSSLOPT_ALLOW_BEAST</c> — разрешить уязвимость BEAST.</summary>
        public const long ALLOW_BEAST = 1L << 0;

        /// <summary><c>CURLSSLOPT_NO_REVOKE</c> — отключить проверку отзыва сертификата.</summary>
        public const long NO_REVOKE = 1L << 1;

        /// <summary><c>CURLSSLOPT_NO_PARTIALCHAIN</c> — не принимать неполную цепочку.</summary>
        public const long NO_PARTIALCHAIN = 1L << 2;

        /// <summary><c>CURLSSLOPT_REVOKE_BEST_EFFORT</c> — игнорировать ошибки offline-проверки отзыва.</summary>
        public const long REVOKE_BEST_EFFORT = 1L << 3;

        /// <summary><c>CURLSSLOPT_NATIVE_CA</c> — использовать системное хранилище сертификатов (Windows).</summary>
        public const long NATIVE_CA = 1L << 4;

        /// <summary><c>CURLSSLOPT_AUTO_CLIENT_CERT</c> — автоматический поиск клиентского сертификата (Schannel).</summary>
        public const long AUTO_CLIENT_CERT = 1L << 5;

        /// <summary><c>CURLSSLOPT_EARLYDATA</c> — отправлять данные в TLS 1.3 early data.</summary>
        public const long EARLYDATA = 1L << 6;
    }
}