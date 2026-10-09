using System;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Делегаты для функций libcurl, загружаемых через <c>GetProcAddress</c>.
    /// Используются в <see cref="NativeMethods"/> как типы для
    /// <see cref="Marshal.GetDelegateForFunctionPointer{TDelegate}(IntPtr)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Для variadic-функций (<c>curl_easy_setopt</c>, <c>curl_easy_getinfo</c>,
    /// <c>curl_multi_setopt</c>) объявлено несколько делегатов с разными типами
    /// последнего аргумента. ABI выбирает правильный вариант по сигнатуре.
    /// </para>
    /// <para>
    /// <c>size_t</c> → <see cref="UIntPtr"/>, <c>curl_off_t</c> → <see cref="long"/>,
    /// <c>curl_socket_t</c> → <see cref="IntPtr"/>.
    /// </para>
    /// </remarks>
    internal static class NativeFunctions
    {
        // ================================================================
        //  curl_global_*
        // ================================================================

        /// <summary><c>CURLcode curl_global_init(long flags)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlGlobalInitDelegate(long flags);

        /// <summary><c>void curl_global_cleanup(void)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CurlGlobalCleanupDelegate();

        // ================================================================
        //  curl_easy_*
        // ================================================================

        /// <summary><c>CURL* curl_easy_init(void)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlEasyInitDelegate();

        /// <summary><c>void curl_easy_cleanup(CURL* handle)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CurlEasyCleanupDelegate(IntPtr handle);

        /// <summary><c>CURLcode curl_easy_perform(CURL* handle)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasyPerformDelegate(IntPtr handle);

        /// <summary><c>const char* curl_easy_strerror(CURLcode)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlEasyStrerrorDelegate(CURLcode code);

        /// <summary><c>void curl_easy_reset(CURL* handle)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CurlEasyResetDelegate(IntPtr handle);

        /// <summary><c>CURL* curl_easy_duphandle(CURL* handle)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlEasyDuplicateDelegate(IntPtr handle);

        // ================================================================
        //  curl_easy_setopt — 10 перегрузок
        // ================================================================

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLoption, const char*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal delegate CURLcode CurlEasySetoptStringDelegate(IntPtr handle, CURLoption option, string value);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLoption, long)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptLongDelegate(IntPtr handle, CURLoption option, long value);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLoption, curl_off_t)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptOffTDelegate(IntPtr handle, CURLoption option, long value);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLoption, void*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptPtrDelegate(IntPtr handle, CURLoption option, IntPtr value);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_WRITEFUNCTION, curl_write_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptWriteCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_write_callback callback);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_HEADERFUNCTION, curl_write_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptHeaderCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_header_callback callback);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_READFUNCTION, curl_read_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptReadCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_read_callback callback);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_XFERINFOFUNCTION, curl_xferinfo_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptXferInfoCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_xferinfo_callback callback);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_DEBUGFUNCTION, curl_debug_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptDebugCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_debug_callback callback);

        /// <summary><c>CURLcode curl_easy_setopt(CURL*, CURLOPT_SSL_CTX_FUNCTION, curl_ssl_ctx_callback)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasySetoptSslCtxCallbackDelegate(IntPtr handle, CURLoption option, Delegates.curl_ssl_ctx_callback callback);

        // ================================================================
        //  curl_easy_getinfo — 4 перегрузки
        // ================================================================

        /// <summary><c>CURLcode curl_easy_getinfo(CURL*, CURLINFO, long*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasyGetinfoLongDelegate(IntPtr handle, CURLINFO info, out long value);

        /// <summary><c>CURLcode curl_easy_getinfo(CURL*, CURLINFO, curl_off_t*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasyGetinfoOffTDelegate(IntPtr handle, CURLINFO info, out long value);

        /// <summary><c>CURLcode curl_easy_getinfo(CURL*, CURLINFO, double*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasyGetinfoDoubleDelegate(IntPtr handle, CURLINFO info, out double value);

        /// <summary><c>CURLcode curl_easy_getinfo(CURL*, CURLINFO, char** / void**)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLcode CurlEasyGetinfoPtrDelegate(IntPtr handle, CURLINFO info, out IntPtr value);

        // ================================================================
        //  curl_slist_*
        // ================================================================

        /// <summary><c>struct curl_slist* curl_slist_append(struct curl_slist*, const char*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal delegate IntPtr CurlSlistAppendDelegate(IntPtr list, string data);

        /// <summary><c>void curl_slist_free_all(struct curl_slist*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CurlSlistFreeAllDelegate(IntPtr list);

        // ================================================================
        //  curl_multi_*
        // ================================================================

        /// <summary><c>CURLM* curl_multi_init(void)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlMultiInitDelegate();

        /// <summary><c>CURLMcode curl_multi_cleanup(CURLM*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiCleanupDelegate(IntPtr multi);

        /// <summary><c>CURLMcode curl_multi_add_handle(CURLM*, CURL*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiAddHandleDelegate(IntPtr multi, IntPtr easy);

        /// <summary><c>CURLMcode curl_multi_remove_handle(CURLM*, CURL*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiRemoveHandleDelegate(IntPtr multi, IntPtr easy);

        /// <summary><c>CURLMcode curl_multi_perform(CURLM*, int* running_handles)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiPerformDelegate(IntPtr multi, out int runningHandles);

        /// <summary><c>CURLMcode curl_multi_poll(CURLM*, struct curl_waitfd*, unsigned int, int, int*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiPollDelegate(IntPtr multi, IntPtr extraFds, uint extraNfds, int timeoutMs, out int numFds);

        /// <summary><c>CURLMcode curl_multi_wait(CURLM*, struct curl_waitfd*, unsigned int, int, int*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiWaitDelegate(IntPtr multi, IntPtr extraFds, uint extraNfds, int timeoutMs, out int numFds);

        /// <summary><c>CURLMsg* curl_multi_info_read(CURLM*, int* msgs_in_queue)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlMultiInfoReadDelegate(IntPtr multi, out int msgsInQueue);

        /// <summary><c>const char* curl_multi_strerror(CURLMcode)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlMultiStrerrorDelegate(CURLMcode code);

        /// <summary><c>CURLMcode curl_multi_wakeup(CURLM*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiWakeupDelegate(IntPtr multi);

        /// <summary><c>CURLMcode curl_multi_setopt(CURLM*, CURLMoption, long)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiSetoptLongDelegate(IntPtr multi, int option, long value);

        /// <summary><c>CURLMcode curl_multi_setopt(CURLM*, CURLMoption, void*)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CURLMcode CurlMultiSetoptPtrDelegate(IntPtr multi, int option, IntPtr value);

        // ================================================================
        //  curl_version / curl_free
        // ================================================================

        /// <summary><c>char* curl_version(void)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlVersionDelegate();

        /// <summary><c>curl_version_info_data* curl_version_info(CURLversion)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CurlVersionInfoDelegate(int age);

        /// <summary><c>void curl_free(void* p)</c>.</summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CurlFreeDelegate(IntPtr ptr);
    }
}