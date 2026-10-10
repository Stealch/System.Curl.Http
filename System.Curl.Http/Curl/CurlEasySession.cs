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

        /// <summary>
        /// Устанавливает адрес прокси-сервера.
        /// </summary>
        /// <param name="proxyUrl">
        /// URL прокси в формате libcurl: <c>http://host:port</c>,
        /// <c>https://host:port</c>, <c>socks4://host:port</c>,
        /// <c>socks5://host:port</c>, <c>socks5h://host:port</c>.
        /// Может содержать учётные данные: <c>http://user:pass@host:port</c>.
        /// </param>
        public void SetProxy(string proxyUrl)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(proxyUrl))
            {
                throw new ArgumentException("URL прокси не может быть пустым.", nameof(proxyUrl));
            }

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY, proxyUrl),
                "SetProxy");
        }

        /// <summary>
        /// Включает или отключает туннелирование через HTTP-прокси
        /// (<c>CONNECT</c>). Обязательно для HTTPS-запросов через HTTP-прокси.
        /// </summary>
        public void SetHttpProxyTunnel(bool tunnel)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_HTTPPROXYTUNNEL, tunnel ? 1L : 0L),
                "SetHttpProxyTunnel");
        }

        /// <summary>
        /// Устанавливает тип прокси (HTTP, HTTPS, SOCKS4, SOCKS5, SOCKS5H).
        /// Обычно не требуется: libcurl определяет тип из схемы URL прокси.
        /// </summary>
        public void SetProxyType(long proxyType)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXYTYPE, proxyType),
                "SetProxyType");
        }

        /// <summary>
        /// Устанавливает учётные данные для аутентификации на прокси.
        /// </summary>
        /// <param name="credentials">Строка в формате <c>user:password</c>.</param>
        public void SetProxyCredentials(string credentials)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(credentials))
            {
                throw new ArgumentException("Учётные данные прокси не могут быть пустыми.", nameof(credentials));
            }

            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXYUSERPWD, credentials),
                "SetProxyCredentials");
        }

        // ================================================================
        //  Прокси: расширенные настройки
        // ================================================================

        /// <summary>
        /// Устанавливает метод аутентификации на прокси.
        /// </summary>
        /// <param name="authMask">Битовая маска из <see cref="CURLAUTH"/>.</param>
        public void SetProxyAuth(long authMask)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXYAUTH, authMask),
                "SetProxyAuth");
        }

        /// <summary>
        /// Устанавливает метод аутентификации для SOCKS5.
        /// </summary>
        public void SetSocks5Auth(long authMask)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_SOCKS5_AUTH, authMask),
                "SetSocks5Auth");
        }

        /// <summary>
        /// Устанавливает список хостов, для которых прокси обходится.
        /// </summary>
        /// <param name="noProxyList">
        /// Список через запятую. Поддерживает <c>*</c> (всё) и CIDR (с 7.86.0).
        /// </param>
        public void SetNoProxy(string noProxyList)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_NOPROXY, noProxyList),
                "SetNoProxy");
        }

        /// <summary>
        /// Устанавливает заголовки, отправляемые только прокси.
        /// </summary>
        /// <param name="headersList">Список <c>curl_slist</c>.</param>
        public void SetProxyHeaders(IntPtr headersList)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXYHEADER, headersList),
                "SetProxyHeaders");
        }

        /// <summary>
        /// Устанавливает порт прокси (устаревший способ, обычно порт в URL).
        /// </summary>
        public void SetProxyPort(long port)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXYPORT, port),
                "SetProxyPort");
        }

        /// <summary>
        /// Устанавливает pre-proxy (SOCKS-прокси перед HTTP-прокси).
        /// </summary>
        public void SetPreProxy(string preProxyUrl)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PRE_PROXY, preProxyUrl),
                "SetPreProxy");
        }

        // ================================================================
        //  Прокси: TLS
        // ================================================================

        /// <summary>
        /// Проверка сертификата HTTPS-прокси.
        /// </summary>
        public void SetProxySslVerifyPeer(bool verify)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSL_VERIFYPEER, verify ? 1L : 0L),
                "SetProxySslVerifyPeer");
        }

        /// <summary>
        /// Проверка имени хоста в сертификате HTTPS-прокси.
        /// </summary>
        public void SetProxySslVerifyHost(bool verify)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSL_VERIFYHOST, verify ? 2L : 0L),
                "SetProxySslVerifyHost");
        }

        /// <summary>
        /// Устанавливает CA bundle для проверки HTTPS-прокси.
        /// </summary>
        public void SetProxyCaBundle(string caBundlePath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_CAINFO, caBundlePath),
                "SetProxyCaBundle");
        }

        /// <summary>
        /// Устанавливает директорию с CA для проверки HTTPS-прокси.
        /// </summary>
        public void SetProxyCaPath(string caPath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_CAPATH, caPath),
                "SetProxyCaPath");
        }

        /// <summary>
        /// Устанавливает минимальную версию TLS для HTTPS-прокси.
        /// </summary>
        public void SetProxySslVersion(long sslVersion)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSLVERSION, sslVersion),
                "SetProxySslVersion");
        }

        /// <summary>
        /// Устанавливает список шифров TLS 1.2 и ниже для HTTPS-прокси.
        /// </summary>
        public void SetProxySslCipherList(string cipherList)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSL_CIPHER_LIST, cipherList),
                "SetProxySslCipherList");
        }

        /// <summary>
        /// Устанавливает список шифров TLS 1.3 для HTTPS-прокси.
        /// </summary>
        public void SetProxyTls13Ciphers(string ciphers)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_TLS13_CIPHERS, ciphers),
                "SetProxyTls13Ciphers");
        }

        /// <summary>
        /// Устанавливает флаги SSL для прокси (см. <see cref="CURLSSLOPT"/>).
        /// </summary>
        public void SetProxySslOptions(long sslOptions)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSL_OPTIONS, sslOptions),
                "SetProxySslOptions");
        }

        // ================================================================
        //  Прокси: клиентские сертификаты
        // ================================================================

        /// <summary>
        /// Устанавливает путь к клиентскому сертификату для mutual TLS на прокси.
        /// </summary>
        public void SetProxySslCert(string certPath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSLCERT, certPath),
                "SetProxySslCert");
        }

        /// <summary>
        /// Устанавливает тип клиентского сертификата прокси (PEM, DER, ENG).
        /// </summary>
        public void SetProxySslCertType(string certType)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSLCERTTYPE, certType),
                "SetProxySslCertType");
        }

        /// <summary>
        /// Устанавливает путь к приватному ключу клиентского сертификата прокси.
        /// </summary>
        public void SetProxySslKey(string keyPath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSLKEY, keyPath),
                "SetProxySslKey");
        }

        /// <summary>
        /// Устанавливает тип приватного ключа прокси (PEM, DER, ENG).
        /// </summary>
        public void SetProxySslKeyType(string keyType)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_SSLKEYTYPE, keyType),
                "SetProxySslKeyType");
        }

        /// <summary>
        /// Устанавливает пароль к приватному ключу прокси.
        /// </summary>
        public void SetProxyKeyPassword(string password)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_KEYPASSWD, password),
                "SetProxyKeyPassword");
        }

        /// <summary>
        /// Устанавливает issuer-сертификат для проверки прокси.
        /// </summary>
        public void SetProxyIssuerCert(string issuerCertPath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_ISSUERCERT, issuerCertPath),
                "SetProxyIssuerCert");
        }

        /// <summary>
        /// Устанавливает CRL-файл для проверки прокси.
        /// </summary>
        public void SetProxyCrlFile(string crlPath)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_CRLFILE, crlPath),
                "SetProxyCrlFile");
        }

        /// <summary>
        /// Устанавливает pinned public key для проверки прокси.
        /// </summary>
        public void SetProxyPinnedPublicKey(string publicKey)
        {
            ThrowIfDisposed();
            CheckResult(
                NativeMethods.Curl_easy_setopt(_handle, CURLoption.CURLOPT_PROXY_PINNEDPUBLICKEY, publicKey),
                "SetProxyPinnedPublicKey");
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