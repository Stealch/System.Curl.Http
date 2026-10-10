#nullable enable

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// Тело HTTP-сообщения на основе массива байт.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.ByteArrayContent</c>.
    /// </para>
    /// <para>
    /// Массив байт не копируется при создании. Изменение исходного массива
    /// после создания <see cref="ByteArrayContent"/> приведёт к изменению тела.
    /// Если это нежелательно — передайте копию.
    /// </para>
    /// </remarks>
    public class ByteArrayContent : HttpContent
    {
        private readonly byte[] _content;
        private readonly int _offset;
        private readonly int _count;

        /// <summary>
        /// Создаёт тело из массива байт целиком.
        /// </summary>
        public ByteArrayContent(byte[] content)
            : this(content, 0, content?.Length ?? 0)
        {
        }

        /// <summary>
        /// Создаёт тело из части массива байт.
        /// </summary>
        /// <param name="content">Исходный массив.</param>
        /// <param name="offset">Смещение в массиве.</param>
        /// <param name="count">Количество байт.</param>
        public ByteArrayContent(byte[] content, int offset, int count)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (offset + count > content.Length)
            {
                throw new ArgumentException("offset + count выходит за границы массива.");
            }

            _content = content;
            _offset = offset;
            _count = count;
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return stream.WriteAsync(_content, _offset, _count);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _count;
            return true;
        }
    }
}