namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Флаги для поля <c>flags</c> структуры <see cref="curl_blob"/>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CURL_BLOB_NOCOPY</c> — libcurl использует переданный указатель
    /// без копирования. Данные должны оставаться валидными до завершения
    /// операции с blob.
    /// </para>
    /// <para>
    /// <c>CURL_BLOB_COPY</c> — libcurl копирует данные во внутреннюю память.
    /// Исходный буфер можно освободить сразу после вызова <c>setopt</c>.
    /// </para>
    /// </remarks>
    internal static class CURL_BLOB
    {
        /// <summary><c>CURL_BLOB_NOCOPY</c> — libcurl использует указатель без копирования.</summary>
        public const uint NOCOPY = 0u;

        /// <summary><c>CURL_BLOB_COPY</c> — libcurl копирует данные.</summary>
        public const uint COPY = 1u;
    }
}