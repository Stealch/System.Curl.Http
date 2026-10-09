namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Базовые типы информации <c>CURLINFO_*</c>. Числовое значение информации
    /// вычисляется как <c>тип + номер</c> (см. <c>curl.h</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Соответствие макросам из <c>curl.h</c> версии 8.18:
    /// <code>
    /// #define CURLINFO_STRING   0x100000
    /// #define CURLINFO_LONG     0x200000
    /// #define CURLINFO_DOUBLE   0x300000
    /// #define CURLINFO_SLIST    0x400000
    /// #define CURLINFO_PTR      0x400000
    /// #define CURLINFO_SOCKET   0x500000
    /// #define CURLINFO_OFF_T    0x600000
    /// </code>
    /// </para>
    /// <para>
    /// <c>CURLINFO_SLIST</c> и <c>CURLINFO_PTR</c> — одно и то же значение.
    /// </para>
    /// </remarks>
    internal static class CURLINFOtype
    {
        /// <summary><c>CURLINFO_STRING</c> — строка (<c>char*</c>).</summary>
        public const int STRING = 0x100000;

        /// <summary><c>CURLINFO_LONG</c> — <c>long</c>.</summary>
        public const int LONG = 0x200000;

        /// <summary><c>CURLINFO_DOUBLE</c> — <c>double</c>.</summary>
        public const int DOUBLE = 0x300000;

        /// <summary><c>CURLINFO_SLIST</c> — <c>struct curl_slist*</c>.</summary>
        public const int SLIST = 0x400000;

        /// <summary><c>CURLINFO_PTR</c> — указатель (алиас <c>SLIST</c>).</summary>
        public const int PTR = SLIST;

        /// <summary><c>CURLINFO_SOCKET</c> — <c>curl_socket_t</c>.</summary>
        public const int SOCKET = 0x500000;

        /// <summary><c>CURLINFO_OFF_T</c> — <c>curl_off_t</c> (64-битное целое).</summary>
        public const int OFF_T = 0x600000;
    }
}