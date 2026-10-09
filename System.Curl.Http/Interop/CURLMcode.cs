namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Коды возврата multi-интерфейса libcurl (<c>CURLMcode</c>).
    /// Полное соответствие <c>multi.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В отличие от <see cref="CURLcode"/>, здесь есть отрицательное значение
    /// <see cref="CURLM_CALL_MULTI_PERFORM"/> (устаревшее). Поэтому базовый тип —
    /// <see cref="int"/> (по умолчанию), и при сравнении нужно учитывать знак.
    /// </para>
    /// <para>
    /// Всегда добавляйте новые коды в конец. Никогда не удаляйте существующие.
    /// </para>
    /// </remarks>
    internal enum CURLMcode
    {
        /// <summary>
        /// Устаревшее значение. В современных версиях libcurl не возвращается.
        /// Оставлено для совместимости с ABI.
        /// </summary>
        CURLM_CALL_MULTI_PERFORM = -1,

        CURLM_OK = 0,
        CURLM_BAD_HANDLE = 1,
        CURLM_BAD_EASY_HANDLE = 2,
        CURLM_OUT_OF_MEMORY = 3,
        CURLM_INTERNAL_ERROR = 4,
        CURLM_BAD_SOCKET = 5,
        CURLM_UNKNOWN_OPTION = 6,
        CURLM_ADDED_ALREADY = 7,
        CURLM_RECURSIVE_API_CALL = 8,
        CURLM_WAKEUP_FAILURE = 9,
        CURLM_BAD_FUNCTION_ARGUMENT = 10,
        CURLM_ABORTED_BY_CALLBACK = 11,
        CURLM_UNRECOVERABLE_POLL = 12,

        /// <summary>
        /// Никогда не использовать. Маркер последнего значения в <c>multi.h</c>.
        /// </summary>
        CURLM_LAST = 13,
    }
}