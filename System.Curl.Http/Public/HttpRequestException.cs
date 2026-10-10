#nullable enable

using System;
using System.Runtime.Serialization;

namespace System.Curl.Http
{
    /// <summary>
    /// Исключение, возникающее при ошибке HTTP-запроса: сетевые ошибки,
    /// ошибки TLS-рукопожатия, недоступность сервера, таймауты и т.п.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpRequestException</c>,
    /// дополненная свойством <see cref="StatusCode"/> (в BCL .NET Framework 4.8
    /// его нет; оно появилось в .NET 5+).
    /// </para>
    /// </remarks>
    [Serializable]
    public class HttpRequestException : Exception
    {
        /// <summary>
        /// HTTP-код состояния, если он известен на момент возникновения ошибки.
        /// <see langword="null"/>, если ошибка произошла до получения ответа
        /// (например, ошибка DNS или TLS).
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// Создаёт исключение с сообщением по умолчанию.
        /// </summary>
        public HttpRequestException()
        {
        }

        /// <summary>
        /// Создаёт исключение с указанным сообщением.
        /// </summary>
        public HttpRequestException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Создаёт исключение с указанным сообщением и внутренним исключением.
        /// </summary>
        public HttpRequestException(string? message, Exception? inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Создаёт исключение с указанным сообщением и HTTP-кодом состояния.
        /// </summary>
        public HttpRequestException(string? message, HttpStatusCode? statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Создаёт исключение с указанным сообщением, внутренним исключением
        /// и HTTP-кодом состояния.
        /// </summary>
        public HttpRequestException(string? message, Exception? inner, HttpStatusCode? statusCode)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Конструктор для десериализации.
        /// </summary>
        protected HttpRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            var statusCodeValue = info.GetInt32(nameof(StatusCode));
            StatusCode = statusCodeValue < 0 ? (HttpStatusCode?)null : (HttpStatusCode)statusCodeValue;
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);

            // -1 означает «StatusCode отсутствует».
            info.AddValue(nameof(StatusCode), StatusCode.HasValue ? (int)StatusCode.Value : -1);
        }
    }
}