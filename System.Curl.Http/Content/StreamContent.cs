#nullable enable

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-сообщения на основе потока.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.StreamContent</c>.
    /// </para>
    /// <para>
    /// Исходный поток не буферизуется при создании. Данные читаются из него
    /// при сериализации (отправке запроса) или при чтении тела ответа.
    /// </para>
    /// <para>
    /// Поток можно прочитать только один раз. Для повторного чтения
    /// используйте <see cref="HttpContent.LoadIntoBufferAsync()"/> до первого чтения.
    /// </para>
    /// <para>
    /// <b>Владение потоком:</b> <see cref="StreamContent"/> освобождает исходный
    /// поток при <see cref="HttpContent.Dispose()"/>, как в BCL.
    /// </para>
    /// </remarks>
    public class StreamContent : HttpContent
    {
        private const int DefaultBufferSize = 81920;

        private readonly Stream _source;
        private readonly int _bufferSize;
        private bool _contentConsumed;

        /// <summary>
        /// Создаёт тело из потока с буфером по умолчанию (81920 байт).
        /// </summary>
        public StreamContent(Stream source)
            : this(source, DefaultBufferSize)
        {
        }

        /// <summary>
        /// Создаёт тело из потока с указанным размером буфера.
        /// </summary>
        /// <param name="source">Исходный поток.</param>
        /// <param name="bufferSize">Размер буфера для чтения. Должен быть больше нуля.</param>
        public StreamContent(Stream source, int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Размер буфера должен быть больше нуля.");
            }

            _source = source ?? throw new ArgumentNullException(nameof(source));
            _bufferSize = bufferSize;
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (_contentConsumed)
            {
                throw new InvalidOperationException("Поток уже был прочитан.");
            }

            _contentConsumed = true;

            var buffer = new byte[_bufferSize];
            int bytesRead;
            while ((bytesRead = await _source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            if (_source.CanSeek)
            {
                length = _source.Length - _source.Position;
                return true;
            }

            length = 0;
            return false;
        }

        /// <inheritdoc />
        protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            if (_contentConsumed)
            {
                throw new InvalidOperationException("Поток уже был прочитан.");
            }

            _contentConsumed = true;

            // Возвращаем исходный поток как есть, без копирования.
            await Task.CompletedTask;
            return _source;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _source.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}