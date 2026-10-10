namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Методы аутентификации для <c>CURLOPT_PROXYAUTH</c>, <c>CURLOPT_HTTPAUTH</c>
    /// и <c>CURLOPT_SOCKS5_AUTH</c>. Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Значения — битовая маска. Можно комбинировать через <c>|</c>.
    /// </para>
    /// <para>
    /// <c>CURLAUTH_ONLY</c> используется вместе с одним другим типом,
    /// чтобы заставить libcurl использовать только указанный метод.
    /// </para>
    /// </remarks>
    internal static class CURLAUTH
    {
        /// <summary><c>CURLAUTH_NONE</c> — без аутентификации.</summary>
        public const long NONE = 0L;

        /// <summary><c>CURLAUTH_BASIC</c> — HTTP Basic.</summary>
        public const long BASIC = 1L << 0;

        /// <summary><c>CURLAUTH_DIGEST</c> — HTTP Digest.</summary>
        public const long DIGEST = 1L << 1;

        /// <summary><c>CURLAUTH_NEGOTIATE</c> — SPNEGO / Negotiate.</summary>
        public const long NEGOTIATE = 1L << 2;

        /// <summary><c>CURLAUTH_GSSNEGOTIATE</c> — алиас <see cref="NEGOTIATE"/> (устаревшее имя).</summary>
        public const long GSSNEGOTIATE = NEGOTIATE;

        /// <summary><c>CURLAUTH_GSSAPI</c> — алиас <see cref="NEGOTIATE"/>.</summary>
        public const long GSSAPI = NEGOTIATE;

        /// <summary><c>CURLAUTH_NTLM</c> — HTTP NTLM.</summary>
        public const long NTLM = 1L << 3;

        /// <summary><c>CURLAUTH_DIGEST_IE</c> — Digest с IE-совместимостью.</summary>
        public const long DIGEST_IE = 1L << 4;

        /// <summary><c>CURLAUTH_NTLM_WB</c> — NTLM через winbind helper. Удалён в 8.8.0.</summary>
        public const long NTLM_WB = 1L << 5;

        /// <summary><c>CURLAUTH_BEARER</c> — Bearer-токен.</summary>
        public const long BEARER = 1L << 6;

        /// <summary><c>CURLAUTH_AWS_SIGV4</c> — AWS Signature v4.</summary>
        public const long AWS_SIGV4 = 1L << 7;

        /// <summary><c>CURLAUTH_ONLY</c> — использовать только указанный метод.</summary>
        public const long ONLY = 1L << 31;

        /// <summary><c>CURLAUTH_ANY</c> — все методы, кроме Digest-IE.</summary>
        public const long ANY = ~DIGEST_IE;

        /// <summary><c>CURLAUTH_ANYSAFE</c> — все методы, кроме Basic и Digest-IE.</summary>
        public const long ANYSAFE = ~(BASIC | DIGEST_IE);
    }
}