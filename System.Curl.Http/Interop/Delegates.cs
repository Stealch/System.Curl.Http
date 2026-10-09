using System;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Callback-делегаты libcurl. Полное соответствие <c>curl.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Все делегаты помечены <see cref="UnmanagedFunctionPointerAttribute"/> с
    /// <see cref="CallingConvention.Cdecl"/>, потому что libcurl экспортирует функции
    /// с C-соглашением о вызовах.
    /// </para>
    /// <para>
    /// <c>size_t</c> в C-заголовке соответствует <see cref="UIntPtr"/> в C#.
    /// <c>curl_off_t</c> — <see cref="long"/>.
    /// <c>curl_socket_t</c> в Windows — <see cref="IntPtr"/>.
    /// </para>
    /// </remarks>
    internal static class Delegates
    {
        // ================================================================
        //  Write / Read / Header
        // ================================================================

        /// <summary>
        /// <c>curl_write_callback</c> — вызывается для каждого блока данных тела ответа.
        /// Возвращает количество обработанных байт; если меньше <c>size * nitems</c> —
        /// libcurl прерывает передачу с <c>CURLE_WRITE_ERROR</c>.
        /// </summary>
        /// <param name="buffer">Указатель на данные.</param>
        /// <param name="size">Размер одного элемента (обычно 1).</param>
        /// <param name="nitems">Количество элементов.</param>
        /// <param name="userdata">Пользовательский указатель, заданный через <c>CURLOPT_WRITEDATA</c>.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr curl_write_callback(
            IntPtr buffer,
            UIntPtr size,
            UIntPtr nitems,
            IntPtr userdata);

        /// <summary>
        /// <c>curl_read_callback</c> — вызывается для получения следующего блока тела запроса
        /// при <c>CURLOPT_UPLOAD</c> или <c>CURLOPT_READFUNCTION</c>.
        /// Возвращает количество прочитанных байт; 0 — конец данных.
        /// </summary>
        /// <param name="buffer">Буфер, в который нужно записать данные.</param>
        /// <param name="size">Размер одного элемента (обычно 1).</param>
        /// <param name="nitems">Количество элементов для чтения.</param>
        /// <param name="instream">Пользовательский указатель, заданный через <c>CURLOPT_READDATA</c>.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr curl_read_callback(
            IntPtr buffer,
            UIntPtr size,
            UIntPtr nitems,
            IntPtr instream);

        /// <summary>
        /// Callback заголовков ответа (сигнатура совпадает с <see cref="curl_write_callback"/>).
        /// Устанавливается через <c>CURLOPT_HEADERFUNCTION</c>.
        /// </summary>
        /// <remarks>
        /// В <c>curl.h</c> нет отдельного typedef — используется <c>curl_write_callback</c>.
        /// Оставляем как отдельное имя для ясности кода.
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr curl_header_callback(
            IntPtr buffer,
            UIntPtr size,
            UIntPtr nitems,
            IntPtr userdata);

        // ================================================================
        //  Progress / Transfer info
        // ================================================================

        /// <summary>
        /// <c>curl_progress_callback</c> — устаревший callback прогресса (до 7.32.0).
        /// Использует <see cref="double"/> для размеров.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_progress_callback(
            IntPtr clientp,
            double dltotal,
            double dlnow,
            double ultotal,
            double ulnow);

        /// <summary>
        /// <c>curl_xferinfo_callback</c> — современный callback прогресса (с 7.32.0).
        /// Возврат ненулевого значения прерывает передачу с <c>CURLE_ABORTED_BY_CALLBACK</c>.
        /// </summary>
        /// <param name="clientp">Пользовательский указатель (<c>CURLOPT_XFERINFODATA</c>).</param>
        /// <param name="dltotal">Ожидаемый объём загрузки в байтах (0, если неизвестен).</param>
        /// <param name="dlnow">Загружено байт на текущий момент.</param>
        /// <param name="ultotal">Ожидаемый объём отправки в байтах (0, если неизвестен).</param>
        /// <param name="ulnow">Отправлено байт на текущий момент.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_xferinfo_callback(
            IntPtr clientp,
            long dltotal,
            long dlnow,
            long ultotal,
            long ulnow);

        // ================================================================
        //  Debug / Trace
        // ================================================================

        /// <summary>
        /// <c>curl_debug_callback</c> — получает детальную информацию о ходе передачи
        /// (заголовки, TLS-данные, текстовые сообщения). Устанавливается через
        /// <c>CURLOPT_DEBUGFUNCTION</c>.
        /// </summary>
        /// <param name="handle">Easy-хендл.</param>
        /// <param name="type">Тип данных (<see cref="curl_infotype"/>).</param>
        /// <param name="data">Указатель на данные.</param>
        /// <param name="size">Размер данных в байтах.</param>
        /// <param name="userptr">Пользовательский указатель (<c>CURLOPT_DEBUGDATA</c>).</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_debug_callback(
            IntPtr handle,
            int type,
            IntPtr data,
            UIntPtr size,
            IntPtr userptr);

        /// <summary>
        /// <c>curl_trailer_callback</c> — вызывается перед завершением chunked-передачи
        /// для добавления трейлерных заголовков. Устанавливается через <c>CURLOPT_TRAILERFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_trailer_callback(
            IntPtr list,
            IntPtr userdata);

        // ================================================================
        //  Seek / IOCTL
        // ================================================================

        /// <summary>
        /// <c>curl_seek_callback</c> — вызывается при необходимости позиционирования
        /// во входном потоке (<c>CURLOPT_SEEKFUNCTION</c>).
        /// </summary>
        /// <param name="instream">Пользовательский указатель (<c>CURLOPT_SEEKDATA</c>).</param>
        /// <param name="offset">Смещение.</param>
        /// <param name="origin">Точка отсчёта (SEEK_SET / SEEK_CUR / SEEK_END).</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_seek_callback(
            IntPtr instream,
            long offset,
            int origin);

        /// <summary>
        /// <c>curl_ioctl_callback</c> — устаревший callback управления I/O (до 7.18.0).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_ioctl_callback(
            IntPtr handle,
            int cmd,
            IntPtr clientp);

        // ================================================================
        //  Socket
        // ================================================================

        /// <summary>
        /// <c>curl_sockopt_callback</c> — вызывается после создания сокета,
        /// до подключения. Устанавливается через <c>CURLOPT_SOCKOPTFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_sockopt_callback(
            IntPtr clientp,
            IntPtr curlfd,
            int purpose);

        /// <summary>
        /// <c>curl_opensocket_callback</c> — вызывается для создания сокета
        /// вместо стандартного <c>socket()</c>. Устанавливается через <c>CURLOPT_OPENSOCKETFUNCTION</c>.
        /// </summary>
        /// <returns>Созданный сокет или <c>CURL_SOCKET_BAD</c> при ошибке.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr curl_opensocket_callback(
            IntPtr clientp,
            int purpose,
            IntPtr address);

        /// <summary>
        /// <c>curl_closesocket_callback</c> — вызывается для закрытия сокета
        /// вместо стандартного <c>close()</c>. Устанавливается через <c>CURLOPT_CLOSESOCKETFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_closesocket_callback(
            IntPtr clientp,
            IntPtr item);

        // ================================================================
        //  SSL
        // ================================================================

        /// <summary>
        /// <c>curl_ssl_ctx_callback</c> — вызывается после создания SSL-контекста,
        /// позволяет его модифицировать. Устанавливается через <c>CURLOPT_SSL_CTX_FUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_ssl_ctx_callback(
            IntPtr curl,
            IntPtr sslCtx,
            IntPtr userptr);

        /// <summary>
        /// <c>curl_conv_callback</c> — устаревший callback конвертации кодировок.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_conv_callback(
            IntPtr buffer,
            UIntPtr length);

        // ================================================================
        //  SSH
        // ================================================================

        /// <summary>
        /// <c>curl_sshkeycallback</c> — вызывается для проверки SSH-ключа хоста.
        /// Устанавливается через <c>CURLOPT_SSH_KEYFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_sshkeycallback(
            IntPtr easy,
            IntPtr knownkey,
            IntPtr foundkey,
            int match,
            IntPtr clientp);

        /// <summary>
        /// <c>curl_sshhostkeycallback</c> — вызывается для проверки hostkey
        /// (альтернатива <c>curl_sshkeycallback</c>). Устанавливается через
        /// <c>CURLOPT_SSH_HOSTKEYFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_sshhostkeycallback(
            IntPtr clientp,
            int keytype,
            IntPtr key,
            UIntPtr keylen);

        // ================================================================
        //  FTP wildcard
        // ================================================================

        /// <summary>
        /// <c>curl_chunk_bgn_callback</c> — вызывается перед загрузкой очередного
        /// файла при wildcard-matching (<c>CURLOPT_CHUNK_BGN_FUNCTION</c>).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_chunk_bgn_callback(
            IntPtr transferInfo,
            IntPtr ptr,
            int remains);

        /// <summary>
        /// <c>curl_chunk_end_callback</c> — вызывается после загрузки очередного
        /// файла при wildcard-matching (<c>CURLOPT_CHUNK_END_FUNCTION</c>).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_chunk_end_callback(
            IntPtr ptr);

        /// <summary>
        /// <c>curl_fnmatch_callback</c> — callback сопоставления с шаблоном
        /// при wildcard-matching (<c>CURLOPT_FNMATCH_FUNCTION</c>).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_fnmatch_callback(
            IntPtr ptr,
            IntPtr pattern,
            IntPtr @string);

        // ================================================================
        //  Resolver
        // ================================================================

        /// <summary>
        /// <c>curl_resolver_start_callback</c> — вызывается перед началом разрешения DNS.
        /// Устанавливается через <c>CURLOPT_RESOLVER_START_FUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_resolver_start_callback(
            IntPtr resolverState,
            IntPtr reserved,
            IntPtr userdata);

        // ================================================================
        //  Pre-request
        // ================================================================

        /// <summary>
        /// <c>curl_prereq_callback</c> — вызывается непосредственно перед отправкой
        /// первого запроса на соединении (после TLS-хендшейка).
        /// Устанавливается через <c>CURLOPT_PREREQFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_prereq_callback(
            IntPtr clientp,
            IntPtr connPrimaryIp,
            IntPtr connLocalIp,
            int connPrimaryPort,
            int connLocalPort);

        // ================================================================
        //  HSTS
        // ================================================================

        /// <summary>
        /// <c>curl_hstsread_callback</c> — вызывается для чтения записей HSTS-кэша.
        /// Устанавливается через <c>CURLOPT_HSTSREADFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_hstsread_callback(
            IntPtr easy,
            IntPtr entry,
            IntPtr userp);

        /// <summary>
        /// <c>curl_hstswrite_callback</c> — вызывается для записи записей HSTS-кэша.
        /// Устанавливается через <c>CURLOPT_HSTSWRITEFUNCTION</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_hstswrite_callback(
            IntPtr easy,
            IntPtr entry,
            IntPtr index,
            IntPtr userp);

        // ================================================================
        //  Share (multi-handle sharing)
        // ================================================================

        /// <summary>
        /// <c>curl_lock_function</c> — callback блокировки при использовании <c>CURLSH</c>
        /// (<c>CURLSHOPT_LOCKFUNC</c>).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void curl_lock_function(
            IntPtr handle,
            int data,
            int locktype,
            IntPtr userptr);

        /// <summary>
        /// <c>curl_unlock_function</c> — callback разблокировки при использовании <c>CURLSH</c>
        /// (<c>CURLSHOPT_UNLOCKFUNC</c>).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void curl_unlock_function(
            IntPtr handle,
            int data,
            IntPtr userptr);

        // ================================================================
        //  Memory (для curl_global_init_mem)
        // ================================================================

        /// <summary>
        /// <c>curl_malloc_callback</c> — пользовательский аллокатор памяти.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr curl_malloc_callback(UIntPtr size);

        /// <summary>
        /// <c>curl_free_callback</c> — пользовательский освободитель памяти.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void curl_free_callback(IntPtr ptr);

        /// <summary>
        /// <c>curl_realloc_callback</c> — пользовательский реассайнер памяти.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr curl_realloc_callback(IntPtr ptr, UIntPtr size);

        /// <summary>
        /// <c>curl_strdup_callback</c> — пользовательский дубликатор строк.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr curl_strdup_callback(IntPtr str);

        /// <summary>
        /// <c>curl_calloc_callback</c> — пользовательский обнуляющий аллокатор.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr curl_calloc_callback(UIntPtr nmemb, UIntPtr size);

        // ================================================================
        //  Form (устаревшее, curl_formadd)
        // ================================================================

        /// <summary>
        /// <c>curl_formget_callback</c> — callback сериализации формы (устаревшее).
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr curl_formget_callback(
            IntPtr arg,
            IntPtr buf,
            UIntPtr len);

        // ================================================================
        //  SSL session export
        // ================================================================

        /// <summary>
        /// <c>curl_ssls_export_cb</c> — callback экспорта SSL-сессий.
        /// Устанавливается через <c>curl_easy_ssls_export</c>.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int curl_ssls_export_cb(
            IntPtr handle,
            IntPtr userptr,
            IntPtr sessionKey,
            IntPtr shmac,
            UIntPtr shmacLen,
            IntPtr sdata,
            UIntPtr sdataLen,
            long validUntil,
            int ietfTlsId,
            IntPtr alpn,
            UIntPtr earlydataMax);
    }
}