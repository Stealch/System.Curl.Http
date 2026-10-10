using System;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Структура <c>curl_blob</c> для опций <c>CURLOPT_*_BLOB</c>.
    /// Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Используется для передачи бинарных данных (сертификатов, ключей,
    /// CA bundle) в libcurl без промежуточных файлов.
    /// </para>
    /// <para>
    /// Blob-опции появились в libcurl 7.71.0.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct curl_blob
    {
        /// <summary>Указатель на данные.</summary>
        public IntPtr data;

        /// <summary>Длина данных в байтах.</summary>
        public UIntPtr len;

        /// <summary>
        /// Флаги: <c>CURL_BLOB_COPY</c> (libcurl копирует данные) или
        /// <c>CURL_BLOB_NOCOPY</c> (libcurl использует указатель без копирования).
        /// </summary>
        public uint flags;
    }
}