#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Поток для чтения тела HTTP-ответа по мере поступления данных из libcurl.
    /// Заполняется из нативного <c>WRITEFUNCTION</c> (поток multi-цикла),
    /// читается пользователем через <see cref="ReadAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Модель.</b> Producer (callback) кладёт чанки в очередь и сигналит
    /// <see cref="SemaphoreSlim"/>; consumer (читатель) ждёт сигнала и забирает.
    /// Потокобезопасен: один producer, один consumer.
    /// </para>
    /// <para>
    /// <b>Завершение.</b> Producer вызывает <see cref="Complete"/> (успех) или
    /// <see cref="SetException"/> (ошибка). После этого <see cref="ReadAsync"/>
    /// возвращает 0 (EOF) или бросает сохранённое исключение.
    /// </para>
    /// <para>
    /// <b>Владение.</b> Поток не владеет multi-хэндлом — освобождение
    /// ответственность вызывающего (<c>CurlHttpMessageHandler</c>).
    /// </para>
    /// </remarks>
    internal sealed class CurlResponseStream : Stream
    {
        private readonly ConcurrentQueue<byte[]> _chunks = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        private byte[]? _currentChunk;
        private int _currentOffset;
        private bool _completed;
        private Exception? _exception;
        private bool _disposed;

        /// <summary>
        /// Добавляет чанк данных. Вызывается из нативного callback'а (producer).
        /// </summary>
        /// <param name="buffer">Буфер с данными.</param>
        /// <param name="count">Количество байт.</param>
        /// <exception cref="ObjectDisposedException">Поток освобождён.</exception>
        public void Write(byte[] buffer, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CurlResponseStream));
            }

            var chunk = new byte[count];
            Buffer.BlockCopy(buffer, 0, chunk, 0, count);
            _chunks.Enqueue(chunk);
            _signal.Release();
        }

        /// <summary>
        /// Сигнализирует успешное завершение передачи. Вызывается после
        /// <c>CURLE_OK</c> из multi-цикла.
        /// </summary>
        public void Complete()
        {
            _completed = true;
            _signal.Release();
        }

        /// <summary>
        /// Сигнализирует завершение с ошибкой. Вызывается при <c>CURLcode != CURLE_OK</c>.
        /// </summary>
        /// <param name="exception">Исключение, которое будет брошено читателю.</param>
        public void SetException(Exception exception)
        {
            _exception = exception;
            _completed = true;
            _signal.Release();
        }

        // ================================================================
        //  Stream
        // ================================================================

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count == 0)
            {
                return 0;
            }

            ThrowIfDisposed();

            while (true)
            {
                // 1. Есть незавершённый текущий чанк — дочитываем.
                if (_currentChunk != null)
                {
                    var available = _currentChunk.Length - _currentOffset;
                    var toCopy = Math.Min(available, count);
                    Buffer.BlockCopy(_currentChunk, _currentOffset, buffer, offset, toCopy);
                    _currentOffset += toCopy;

                    if (_currentOffset >= _currentChunk.Length)
                    {
                        _currentChunk = null;
                        _currentOffset = 0;
                    }

                    return toCopy;
                }

                // 2. Пробуем взять следующий чанк из очереди.
                if (_chunks.TryDequeue(out var chunk))
                {
                    _currentChunk = chunk;
                    _currentOffset = 0;
                    continue;
                }

                // 3. Очередь пуста — если передача завершена, это EOF/ошибка.
                if (_completed)
                {
                    if (_exception != null)
                    {
                        throw _exception;
                    }

                    return 0;
                }

                // 4. Ждём сигнала от producer'а.
                await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        // ================================================================
        //  Освобождение
        // ================================================================

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CurlResponseStream));
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                _signal.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}