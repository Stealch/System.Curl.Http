#nullable enable

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Curl.Http.Curl;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-ответа, читаемое потоком по мере поступления данных из libcurl.
    /// Используется при <see cref="HttpCompletionOption.ResponseHeadersRead"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Оборачивает <see cref="CurlResponseStream"/> и владеет им: при
    /// <see cref="Dispose(bool)"/> поток освобождается.
    /// </para>
    /// <para>
    /// Длина тела заранее неизвестна (<see cref="TryComputeLength"/> возвращает
    /// <see langword="false"/>), поэтому <c>Content-Length</c> не вычисляется.
    /// </para>
    /// </remarks>
    internal sealed class StreamingHttpContent : HttpContent
    {
        private readonly CurlResponseStream _stream;

        /// <summary>
        /// Создаёт тело поверх указанного потока.
        /// </summary>
        /// <param name="stream">Поток с данными ответа.</param>
        public StreamingHttpContent(CurlResponseStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Тело ответа не сериализуется обратно — это ответ, а не запрос.
            // Метод должен быть реализован, но для response-content не вызывается.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        /// <inheritdoc />
        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            // Отдаём поток как есть — без буферизации.
            return Task.FromResult<Stream>(_stream);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}