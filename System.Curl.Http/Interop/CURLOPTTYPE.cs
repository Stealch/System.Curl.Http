namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Базовые типы опций <c>CURLOPT_*</c>. Числовое значение опции вычисляется
    /// как <c>тип + номер</c> (см. макрос <c>CURLOPT(na, t, nu)</c> в <c>curl.h</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Соответствие макросам из <c>curl.h</c> версии 8.18:
    /// <code>
    /// #define CURLOPTTYPE_LONG          0
    /// #define CURLOPTTYPE_OBJECTPOINT   10000
    /// #define CURLOPTTYPE_FUNCTIONPOINT 20000
    /// #define CURLOPTTYPE_OFF_T         30000
    /// #define CURLOPTTYPE_BLOB          40000
    /// </code>
    /// </para>
    /// <para>
    /// <c>STRINGPOINT</c>, <c>SLISTPOINT</c> и <c>CBPOINT</c> — алиасы <c>OBJECTPOINT</c>.
    /// </para>
    /// </remarks>
    internal static class CURLOPTTYPE
    {
        /// <summary><c>CURLOPTTYPE_LONG</c> — простой <c>long</c>.</summary>
        public const int LONG = 0;

        /// <summary><c>CURLOPTTYPE_OBJECTPOINT</c> — указатель на объект.</summary>
        public const int OBJECTPOINT = 10000;

        /// <summary><c>CURLOPTTYPE_STRINGPOINT</c> — указатель на строку (алиас <c>OBJECTPOINT</c>).</summary>
        public const int STRINGPOINT = OBJECTPOINT;

        /// <summary><c>CURLOPTTYPE_SLISTPOINT</c> — указатель на <c>curl_slist</c> (алиас <c>OBJECTPOINT</c>).</summary>
        public const int SLISTPOINT = OBJECTPOINT;

        /// <summary><c>CURLOPTTYPE_CBPOINT</c> — указатель, передаваемый в callback без изменений (алиас <c>OBJECTPOINT</c>).</summary>
        public const int CBPOINT = OBJECTPOINT;

        /// <summary><c>CURLOPTTYPE_FUNCTIONPOINT</c> — указатель на функцию (callback).</summary>
        public const int FUNCTIONPOINT = 20000;

        /// <summary><c>CURLOPTTYPE_OFF_T</c> — <c>curl_off_t</c> (64-битное целое).</summary>
        public const int OFF_T = 30000;

        /// <summary><c>CURLOPTTYPE_BLOB</c> — указатель на <c>struct curl_blob</c>.</summary>
        public const int BLOB = 40000;
    }
}