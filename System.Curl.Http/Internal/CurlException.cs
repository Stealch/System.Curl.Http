#nullable enable

using System;
using System.Curl.Http.Interop;
using System.Runtime.Serialization;

namespace System.Curl.Http.Internal
{
    /// <summary>
    /// Исключение, возникающее при ошибке в native-вызове libcurl.
    /// Несёт код возврата <see cref="CURLcode"/>, текстовое описание из
    /// <c>curl_easy_strerror</c> и, если был установлен <c>CURLOPT_ERRORBUFFER</c>,
    /// детальное сообщение (например, точную причину ошибки TLS-хендшейка).
    /// </summary>
    [Serializable]
    internal sealed class CurlException : Exception
    {
        /// <summary>
        /// Код возврата libcurl (<see cref="CURLcode"/>).
        /// </summary>
        public CURLcode Code { get; }

        /// <summary>
        /// Название операции, при выполнении которой произошла ошибка
        /// (например, <c>"SetUrl"</c>, <c>"Perform"</c>, <c>"GetResponseCode"</c>).
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Текстовое описание из <c>curl_easy_strerror</c>.
        /// </summary>
        public string CurlErrorText { get; }

        /// <summary>
        /// Содержимое <c>CURLOPT_ERRORBUFFER</c> на момент ошибки, если буфер был установлен.
        /// Содержит детали, недоступные в <c>curl_easy_strerror</c>
        /// (например, <c>"SSL certificate problem: unable to get local issuer certificate"</c>).
        /// </summary>
        public string? ErrorBuffer { get; }

        public CurlException(
            string operation,
            CURLcode code,
            string curlErrorText,
            string? errorBuffer = null,
            Exception? innerException = null)
            : base(BuildMessage(operation, code, curlErrorText, errorBuffer), innerException)
        {
            Operation = operation;
            Code = code;
            CurlErrorText = curlErrorText;
            ErrorBuffer = errorBuffer;
        }

        private CurlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Operation = info.GetString(nameof(Operation)) ?? string.Empty;
            Code = (CURLcode)info.GetInt32(nameof(Code));
            CurlErrorText = info.GetString(nameof(CurlErrorText)) ?? string.Empty;
            ErrorBuffer = info.GetString(nameof(ErrorBuffer));
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);

            info.AddValue(nameof(Operation), Operation);
            info.AddValue(nameof(Code), (int)Code);
            info.AddValue(nameof(CurlErrorText), CurlErrorText);
            info.AddValue(nameof(ErrorBuffer), ErrorBuffer);
        }

        private static string BuildMessage(
            string operation,
            CURLcode code,
            string curlErrorText,
            string? errorBuffer)
        {
            var message = $"libcurl error in '{operation}': {curlErrorText} (code {(int)code} = {code})";

            if (!string.IsNullOrWhiteSpace(errorBuffer))
            {
                message += Environment.NewLine + "Details: " + errorBuffer;
            }

            return message;
        }
    }
}