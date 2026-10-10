#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Curl.Http
{
    /// <summary>
    /// Базовый класс для обработчиков HTTP-сообщений. Наследники реализуют
    /// транспорт: отправку запроса и получение ответа.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpMessageHandler</c>.
    /// </para>
    /// <para>
    /// <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/> —
    /// <see langword="protected abstract"/>: наследники обязаны реализовать.
    /// <see cref="HttpClient"/> вызывает его через <see cref="SendAsyncInternal"/>.
    /// </para>
    /// <para>
    /// После <see cref="Dispose"/> вызов <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>
    /// должен бросать <see cref="ObjectDisposedException"/>. Наследники
    /// отвечают за реализацию этой проверки.
    /// </para>
    /// </remarks>
    public abstract class HttpMessageHandler : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Отправляет HTTP-запрос и возвращает ответ.
        /// </summary>
        /// <param name="request">Запрос.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Ответ.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> равен <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Запрос уже был отправлен или его URI не задан.
        /// </exception>
        /// <exception cref="HttpRequestException">Ошибка транспорта.</exception>
        /// <exception cref="OperationCanceledException">Запрос отменён.</exception>
        protected abstract Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Вызывает <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>
        /// от имени <see cref="HttpClient"/>. Проверяет, что обработчик не освобождён.
        /// </summary>
        internal Task<HttpResponseMessage> SendAsyncInternal(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return SendAsync(request, cancellationToken);
        }

        // ================================================================
        //  Освобождение
        // ================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освобождает ресурсы обработчика. Наследники переопределяют, если
        /// владеют неуправляемыми или потоковыми ресурсами (например,
        /// нативными хендлами libcurl), и вызывают <c>base.Dispose(disposing)</c>
        /// последним.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        /// <summary>
        /// Бросает <see cref="ObjectDisposedException"/>, если обработчик освобождён.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}