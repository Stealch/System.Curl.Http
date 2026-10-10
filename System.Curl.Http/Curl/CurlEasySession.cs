#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Curl.Http.Interop;
using System.Curl.Http.Internal;
using System.Curl.Http.Native;

namespace System.Curl.Http.Curl
{
    /// <summary>
    /// Обёртка над easy-хендлом libcurl. Инкапсулирует создание, настройку,
    /// выполнение запроса и чтение метаданных ответа.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Не потокобезопасен.</b> Один экземпляр — один запрос за раз.
    /// Для параллельных запросов создавайте отдельные экземпляры.
    /// </para>
    /// <para>
    /// <b>Время жизни.</b> Экземпляр владеет easy-хендлом, списком заголовков
    /// и pinned-буфером POST-данных. Всё освобождается в <see cref="Dispose"/>.
    /// </para>
    /// </remarks>
    internal sealed class CurlEasySession : IDisposable
    {
        private const string Category = "CurlEasySession";
        private const int ErrorBufferSize = 256;

        private IntPtr _handle;
        private IntPtr _headersList;
        private GCHandleScope _pinnedPostData;
        private GCHandleScope _pinnedPostDataHandle;
        private bool _disposed;

        /// <summary>
        /// Easy-хендл libcurl. Для передачи в <c>curl_multi_*</c> и в callback'и.
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                ThrowIfDisposed();
                return _handle;
            }
        }

        public CurlEasySession()
        {
            _handle = NativeMethods.Curl_easy_init();
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("curl_easy_init вернул нулевой handle.");
            }

            Logger.Trace(Category, $"Создана сессия: handle = 0x{_handle.ToInt64():X}");
        }

        // ================================================================
        //  Настройка запроса
        // ================================================================

        /// <summary>
        /// Устанавливает URL запроса.
        /// </summary>
        public void SetUrl(string url)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_URL, url),
                "SetUrl");
        }

        /// <summary>
        /// Устанавливает HTTP-метод GET. Сбрасывает POST/UPLOAD-режимы.
        /// </summary>
        public void SetHttpGet()
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTPGET, 1L),
                "SetHttpGet");
        }

        /// <summary>
        /// Устанавливает HTTP-метод POST.
        /// </summary>
        public void SetPost()
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_POST, 1L),
                "SetPost");
        }

        /// <summary>
        /// Устанавливает HTTP-метод HEAD (без тела ответа).
        /// </summary>
        public void SetNoBody()
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_NOBODY, 1L),
                "SetNoBody");
        }

        /// <summary>
        /// Устанавливает произвольный HTTP-метод (PUT, DELETE, PATCH и т.д.).
        /// </summary>
        public void SetCustomRequest(string method)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_CUSTOMREQUEST, method),
                "SetCustomRequest");
        }

        /// <summary>
        /// Устанавливает тело POST-запроса из массива байт. Массив фиксируется
        /// в памяти до <see cref="Dispose"/>.
        /// </summary>
        public void SetPostData(byte[] data)
        {
            ThrowIfDisposed();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Освобождаем предыдущий pinned-буфер, если был.
            _pinnedPostData.Dispose();

            _pinnedPostData = GCHandleScope.Pin(data);

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_POSTFIELDS, _pinnedPostData.Handle),
                "SetPostDataPointer");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_POSTFIELDSIZE, (long)data.Length),
                "SetPostDataSize");
        }

        /// <summary>
        /// Добавляет заголовок запроса. Формат: <c>"Name: value"</c>.
        /// </summary>
        public void AddHeader(string header)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentException("Заголовок не может быть пустым.", nameof(header));
            }

            var newList = NativeMethods.Curl_slist_append(_headersList, header);
            if (newList == IntPtr.Zero)
            {
                throw new InvalidOperationException($"curl_slist_append вернул нулевой указатель для заголовка '{header}'.");
            }

            _headersList = newList;
        }

        /// <summary>
        /// Устанавливает User-Agent.
        /// </summary>
        public void SetUserAgent(string userAgent)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_USERAGENT, userAgent),
                "SetUserAgent");
        }

        /// <summary>
        /// Устанавливает путь к CA bundle (PEM-файл с корневыми сертификатами).
        /// </summary>
        public void SetCaBundle(string caBundlePath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(caBundlePath))
            {
                throw new ArgumentException("Путь к CA bundle не может быть пустым.", nameof(caBundlePath));
            }

            if (!File.Exists(caBundlePath))
            {
                throw new FileNotFoundException($"CA bundle не найден: {caBundlePath}", caBundlePath);
            }

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_CAINFO, caBundlePath),
                "SetCaBundle");
        }

        /// <summary>
        /// Устанавливает минимальную версию TLS.
        /// </summary>
        public void SetSslVersion(long sslVersion)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_SSLVERSION, sslVersion),
                "SetSslVersion");
        }

        /// <summary>
        /// Устанавливает версию HTTP (1.0, 1.1, 2, 2TLS, 3).
        /// </summary>
        public void SetHttpVersion(long httpVersion)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTP_VERSION, httpVersion),
                "SetHttpVersion");
        }

        /// <summary>
        /// Управляет автоматическим следованием редиректам.
        /// </summary>
        public void SetFollowLocation(bool follow)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_FOLLOWLOCATION, follow ? 1L : 0L),
                "SetFollowLocation");
        }

        /// <summary>
        /// Максимальное число автоматических редиректов.
        /// </summary>
        public void SetMaxRedirs(long maxRedirs)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_MAXREDIRS, maxRedirs),
                "SetMaxRedirs");
        }

        /// <summary>
        /// Устанавливает Accept-Encoding (например, <c>"gzip, deflate"</c>).
        /// </summary>
        public void SetAcceptEncoding(string encoding)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_ACCEPT_ENCODING, encoding),
                "SetAcceptEncoding");
        }

        /// <summary>
        /// Устанавливает таймауты подключения и передачи (в секундах).
        /// </summary>
        public void SetTimeouts(int connectTimeoutSeconds, int timeoutSeconds)
        {
            ThrowIfDisposed();

            if (connectTimeoutSeconds > 0)
            {
                CheckResult(
                    NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_CONNECTTIMEOUT, (long)connectTimeoutSeconds),
                    "SetConnectTimeout");
            }

            if (timeoutSeconds > 0)
            {
                CheckResult(
                    NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_TIMEOUT, (long)timeoutSeconds),
                    "SetTimeout");
            }
        }

        /// <summary>
        /// Отключает обработку сигналов libcurl. Обязательно для многопоточных приложений.
        /// </summary>
        public void SetNoSignal(bool noSignal)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_NOSIGNAL, noSignal ? 1L : 0L),
                "SetNoSignal");
        }

        /// <summary>
        /// Управляет проверкой сертификата сервера.
        /// </summary>
        public void SetSslVerifyPeer(bool verify)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_SSL_VERIFYPEER, verify ? 1L : 0L),
                "SetSslVerifyPeer");
        }

        /// <summary>
        /// Управляет проверкой имени хоста в сертификате.
        /// </summary>
        public void SetSslVerifyHost(bool verify)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_SSL_VERIFYHOST, verify ? 2L : 0L),
                "SetSslVerifyHost");
        }

        /// <summary>
        /// Отключает проверку содержимого тела ответа (HTTP_CONTENT_DECODING = 0).
        /// Используется, если декодирование gzip/deflate делаем сами.
        /// </summary>
        public void DisableContentDecoding()
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTP_CONTENT_DECODING, 0L),
                "DisableContentDecoding");
        }

        /// <summary>
        /// Отключает декодирование transfer-encoding (chunked).
        /// </summary>
        public void DisableTransferDecoding()
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTP_TRANSFER_DECODING, 0L),
                "DisableTransferDecoding");
        }

        // ================================================================
        //  Callback'и
        // ================================================================

        /// <summary>
        /// Устанавливает callback для тела ответа и пользовательский указатель.
        /// </summary>
        public void SetWriteCallback(Delegates.curl_write_callback callback, IntPtr userData)
        {
            ThrowIfDisposed();

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_WRITEFUNCTION, callback),
                "SetWriteCallback");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_WRITEDATA, userData),
                "SetWriteData");
        }

        /// <summary>
        /// Устанавливает callback для заголовков ответа и пользовательский указатель.
        /// </summary>
        public void SetHeaderCallback(Delegates.curl_header_callback callback, IntPtr userData)
        {
            ThrowIfDisposed();

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HEADERFUNCTION, callback),
                "SetHeaderCallback");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HEADERDATA, userData),
                "SetHeaderData");
        }

        /// <summary>
        /// Устанавливает callback для чтения тела запроса и пользовательский указатель.
        /// </summary>
        public void SetReadCallback(Delegates.curl_read_callback callback, IntPtr userData)
        {
            ThrowIfDisposed();

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_READFUNCTION, callback),
                "SetReadCallback");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_READDATA, userData),
                "SetReadData");
        }

        /// <summary>
        /// Устанавливает callback прогресса/отмены и пользовательский указатель.
        /// </summary>
        public void SetXferInfoCallback(Delegates.curl_xferinfo_callback callback, IntPtr userData)
        {
            ThrowIfDisposed();

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_XFERINFOFUNCTION, callback),
                "SetXferInfoCallback");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_XFERINFODATA, userData),
                "SetXferInfoData");

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_NOPROGRESS, 0L),
                "SetNoProgress");
        }

        // ================================================================
        //  Выполнение и чтение результата
        // ================================================================

        /// <summary>
        /// Применяет список заголовков и выполняет запрос.
        /// </summary>
        public void Perform()
        {
            ThrowIfDisposed();

            if (_headersList != IntPtr.Zero)
            {
                CheckResult(
                    NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTPHEADER, _headersList),
                    "SetHttpHeader");
            }

            var result = NativeMethods.Curl_easy_perform(_handle);
            if (result != CURLcode.CURLE_OK)
            {
                throw BuildException("Perform", result);
            }
        }

        /// <summary>
        /// Возвращает HTTP status code последнего ответа (0, если ответа не было).
        /// </summary>
        public long GetResponseCode()
        {
            ThrowIfDisposed();

            var result = NativeMethods.Curl_easy_getinfo(
                _handle, CURLINFO.CURLINFO_RESPONSE_CODE, out long statusCode);

            CheckResult(result, "GetResponseCode");
            return statusCode;
        }

        /// <summary>
        /// Возвращает URL с учётом редиректов.
        /// </summary>
        public string? GetEffectiveUrl()
        {
            ThrowIfDisposed();

            var result = NativeMethods.Curl_easy_getinfo(
                _handle, CURLINFO.CURLINFO_EFFECTIVE_URL, out IntPtr urlPtr);

            CheckResult(result, "GetEffectiveUrl");
            return NativeMethods.PtrToStringUtf8(urlPtr);
        }

        /// <summary>
        /// Возвращает Content-Type ответа.
        /// </summary>
        public string? GetContentType()
        {
            ThrowIfDisposed();

            var result = NativeMethods.Curl_easy_getinfo(
                _handle, CURLINFO.CURLINFO_CONTENT_TYPE, out IntPtr contentTypePtr);

            CheckResult(result, "GetContentType");
            return NativeMethods.PtrToStringUtf8(contentTypePtr);
        }

        /// <summary>
        /// Возвращает версию HTTP, использованную в соединении.
        /// </summary>
        public long GetHttpVersion()
        {
            ThrowIfDisposed();

            var result = NativeMethods.Curl_easy_getinfo(
                _handle, CURLINFO.CURLINFO_HTTP_VERSION, out long httpVersion);

            CheckResult(result, "GetHttpVersion");
            return httpVersion;
        }

        // ================================================================
        //  Служебное
        // ================================================================

        private void CheckResult(CURLcode result, string operation)
        {
            if (result != CURLcode.CURLE_OK)
            {
                throw BuildException(operation, result);
            }
        }

        private CurlException BuildException(string operation, CURLcode result)
        {
            var errorPtr = NativeMethods.Curl_easy_strerror(result);
            var errorText = NativeMethods.PtrToStringUtf8(errorPtr) ?? $"CURLcode {(int)result}";

            return new CurlException(operation, result, errorText);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CurlEasySession));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _pinnedPostData.Dispose();
            _pinnedPostDataHandle.Dispose();

            if (_headersList != IntPtr.Zero)
            {
                NativeMethods.Curl_slist_free_all(_headersList);
                _headersList = IntPtr.Zero;
            }

            if (_handle != IntPtr.Zero)
            {
                NativeMethods.Curl_easy_cleanup(_handle);
                Logger.Trace(Category, $"Сессия освобождена: handle = 0x{_handle.ToInt64():X}");
                _handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        ~CurlEasySession()
        {
            Dispose();
        }
    }
}