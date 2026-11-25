#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Curl.Http.Interop;
using System.Curl.Http.Internal;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Асинхронная сессия multi-интерфейса libcurl. Владеет <c>CURLM*</c>
    /// и выполняет один easy-хэндл в неблокирующем цикле.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Модель.</b> Один экземпляр — один запрос. easy-хэндл должен быть
    /// настроен до вызова <see cref="PerformAsync"/> и жить до завершения.
    /// </para>
    /// <para>
    /// <b>Отмена.</b> Проверяется между итерациями цикла. <c>curl_multi_poll</c>
    /// вызывается с коротким таймаутом (250 мс), чтобы отмена срабатывала
    /// достаточно быстро без <c>curl_multi_wakeup</c> из другого потока.
    /// </para>
    /// <para>
    /// <b>Не потокобезопасен.</b> Все вызовы <c>curl_multi_*</c> — из одного потока.
    /// </para>
    /// </remarks>
    internal sealed class CurlMultiSession : IDisposable
    {
        private const string Category = "CurlMultiSession";
        private const int PollTimeoutMs = 250;

        private IntPtr _multiHandle;
        private bool _disposed;

        /// <summary>
        /// Создаёт multi-сессию. <c>curl_multi_init</c> вызывается сразу.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <c>curl_multi_init</c> вернул нулевой handle.
        /// </exception>
        public CurlMultiSession()
        {
            _multiHandle = NativeMethods.Curl_multi_init();
            if (_multiHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("curl_multi_init вернул нулевой handle.");
            }

            Logger.Trace(Category, $"Создана multi-сессия: handle = 0x{_multiHandle.ToInt64():X}");
        }

        /// <summary>
        /// Выполняет easy-хэндл в multi-цикле до завершения или отмены.
        /// </summary>
        /// <param name="easyHandle">Настроенный easy-хэндл (из <see cref="CurlEasySession.Handle"/>).</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Финальный <see cref="CURLcode"/> завершённой передачи.</returns>
        /// <exception cref="ObjectDisposedException">Сессия освобождена.</exception>
        /// <exception cref="InvalidOperationException">
        /// Не удалось добавить easy-хэндл или получить результат.
        /// </exception>
        /// <exception cref="OperationCanceledException">Запрос отменён.</exception>
        public Task<CURLcode> PerformAsync(IntPtr easyHandle, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (easyHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Easy-хэндл не может быть нулевым.", nameof(easyHandle));
            }

            // Сам цикл синхронный (curl_multi_* — блокирующие вызовы с таймаутом).
            // Возвращаем Task, чтобы вызывающий мог единообразно работать с async-цепочкой.
            return Task.FromResult(Perform(easyHandle, cancellationToken));
        }

        private CURLcode Perform(IntPtr easyHandle, CancellationToken cancellationToken)
        {
            var addResult = NativeMethods.Curl_multi_add_handle(_multiHandle, easyHandle);
            if (addResult != CURLMcode.CURLM_OK)
            {
                throw new InvalidOperationException(
                    $"curl_multi_add_handle failed: {addResult} ({(int)addResult}).");
            }

            try
            {
                Logger.Trace(Category, "Передача добавлена в multi-хэндл.");

                var runningHandles = 0;

                // Первый perform — запускает передачу.
                var performResult = NativeMethods.Curl_multi_perform(_multiHandle, out runningHandles);
                if (performResult != CURLMcode.CURLM_OK)
                {
                    throw new InvalidOperationException(
                        $"curl_multi_perform failed: {performResult} ({(int)performResult}).");
                }

                while (runningHandles > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Ждём активности (или таймаута), чтобы не крутить цикл вхолостую.
                    var pollResult = NativeMethods.Curl_multi_poll(
                        _multiHandle,
                        IntPtr.Zero,
                        0,
                        PollTimeoutMs,
                        out _);

                    if (pollResult != CURLMcode.CURLM_OK)
                    {
                        throw new InvalidOperationException(
                            $"curl_multi_poll failed: {pollResult} ({(int)pollResult}).");
                    }

                    performResult = NativeMethods.Curl_multi_perform(_multiHandle, out runningHandles);
                    if (performResult != CURLMcode.CURLM_OK)
                    {
                        throw new InvalidOperationException(
                            $"curl_multi_perform failed: {performResult} ({(int)performResult}).");
                    }
                }

                // Передача завершена — читаем финальный CURLcode из очереди сообщений.
                var finalCode = ReadFinalResult(easyHandle);
                Logger.Trace(Category, $"Передача завершена: {finalCode} ({(int)finalCode}).");
                return finalCode;
            }
            finally
            {
                // Всегда снимаем easy-хэндл, даже при исключении.
                var removeResult = NativeMethods.Curl_multi_remove_handle(_multiHandle, easyHandle);
                if (removeResult != CURLMcode.CURLM_OK)
                {
                    // Не бросаем из finally — только логируем.
                    Logger.Warning(Category,
                        $"curl_multi_remove_handle failed: {removeResult} ({(int)removeResult}).");
                }
            }
        }

        /// <summary>
        /// Читает сообщения <c>curl_multi_info_read</c> и возвращает <c>CURLcode</c>
        /// для указанного easy-хэндла. Если сообщение не найдено — <c>CURLE_OK</c>
        /// (передача завершилась без явной ошибки).
        /// </summary>
        private CURLcode ReadFinalResult(IntPtr easyHandle)
        {
            int msgsInQueue;
            IntPtr msgPtr;

            while ((msgPtr = NativeMethods.Curl_multi_info_read(_multiHandle, out msgsInQueue)) != IntPtr.Zero)
            {
                var msg = Marshal.PtrToStructure<CURLMsg>(msgPtr);

                if (msg.Msg != CURLMSG_DONE)
                {
                    continue;
                }

                if (msg.EasyHandle != easyHandle)
                {
                    continue;
                }

                return (CURLcode)msg.Result;
            }

            // Сообщение не найдено — считаем передачу успешной (не должно происходить).
            return CURLcode.CURLE_OK;
        }

        // CURLMSG_DONE = 1 (CURLMSG_NONE = 0, CURLMSG_DONE = 1).
        private const int CURLMSG_DONE = 1;

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CurlMultiSession));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_multiHandle != IntPtr.Zero)
            {
                var result = NativeMethods.Curl_multi_cleanup(_multiHandle);
                if (result != CURLMcode.CURLM_OK)
                {
                    Logger.Warning(Category,
                        $"curl_multi_cleanup failed: {result} ({(int)result}).");
                }

                Logger.Trace(Category, $"Multi-сессия освобождена: handle = 0x{_multiHandle.ToInt64():X}");
                _multiHandle = IntPtr.Zero;
            }
        }
    }
}