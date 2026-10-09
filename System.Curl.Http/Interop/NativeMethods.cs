#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Curl.Http.Internal;
using System.Curl.Http.Native;

namespace System.Curl.Http.Interop
{
    /// <summary>
    /// P/Invoke-обёртки над libcurl. Функции загружаются динамически через
    /// <c>LoadLibrary</c> + <c>GetProcAddress</c>, потому что libcurl распаковывается
    /// из встроенных ресурсов во временную папку сессии (см. <see cref="LibCurlLoader"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Для variadic-функций (<c>curl_easy_setopt</c>, <c>curl_easy_getinfo</c>,
    /// <c>curl_share_setopt</c>, <c>curl_multi_setopt</c>) объявлено несколько делегатов
    /// с разными типами последнего аргумента. ABI выбирает правильный вариант
    /// по сигнатуре C#-делегата.
    /// </para>
    /// <para>
    /// Все делегаты кэшируются через <see cref="Lazy{T}"/>: <c>GetProcAddress</c>
    /// вызывается один раз и потокобезопасно.
    /// </para>
    /// <para>
    /// Функциональные делегаты лежат в <see cref="NativeFunctions"/>.
    /// Callback-делегаты (то, что libcurl вызывает у нас) — в <see cref="Delegates"/>.
    /// </para>
    /// </remarks>
    internal static class NativeMethods
    {
        private static readonly IntPtr _libHandle;

        static NativeMethods()
        {
            LibCurlLoader.EnsureLoaded();
            _libHandle = LibCurlLoader.GetHandle();

            if (_libHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("libcurl не загружен: handle = 0.");
            }
        }

        // ================================================================
        //  Загрузка функций
        // ================================================================

        private static T GetFunction<T>(string name) where T : Delegate
        {
            var ptr = GetProcAddress(_libHandle, name);
            if (ptr == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new EntryPointNotFoundException(
                    $"Функция '{name}' не найдена в libcurl (Win32 error {error}).");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        // ================================================================
        //  curl_global_*
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlGlobalInitDelegate> _curlGlobalInit =
            new Lazy<NativeFunctions.CurlGlobalInitDelegate>(() => GetFunction<NativeFunctions.CurlGlobalInitDelegate>("curl_global_init"));

        private static readonly Lazy<NativeFunctions.CurlGlobalCleanupDelegate> _curlGlobalCleanup =
            new Lazy<NativeFunctions.CurlGlobalCleanupDelegate>(() => GetFunction<NativeFunctions.CurlGlobalCleanupDelegate>("curl_global_cleanup"));

        public static CURLcode Curl_global_init(long flags) => _curlGlobalInit.Value(flags);
        public static void Curl_global_cleanup() => _curlGlobalCleanup.Value();

        // ================================================================
        //  curl_easy_*
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlEasyInitDelegate> _curlEasyInit =
            new Lazy<NativeFunctions.CurlEasyInitDelegate>(() => GetFunction<NativeFunctions.CurlEasyInitDelegate>("curl_easy_init"));

        private static readonly Lazy<NativeFunctions.CurlEasyCleanupDelegate> _curlEasyCleanup =
            new Lazy<NativeFunctions.CurlEasyCleanupDelegate>(() => GetFunction<NativeFunctions.CurlEasyCleanupDelegate>("curl_easy_cleanup"));

        private static readonly Lazy<NativeFunctions.CurlEasyPerformDelegate> _curlEasyPerform =
            new Lazy<NativeFunctions.CurlEasyPerformDelegate>(() => GetFunction<NativeFunctions.CurlEasyPerformDelegate>("curl_easy_perform"));

        private static readonly Lazy<NativeFunctions.CurlEasyStrerrorDelegate> _curlEasyStrerror =
            new Lazy<NativeFunctions.CurlEasyStrerrorDelegate>(() => GetFunction<NativeFunctions.CurlEasyStrerrorDelegate>("curl_easy_strerror"));

        private static readonly Lazy<NativeFunctions.CurlEasyResetDelegate> _curlEasyReset =
            new Lazy<NativeFunctions.CurlEasyResetDelegate>(() => GetFunction<NativeFunctions.CurlEasyResetDelegate>("curl_easy_reset"));

        private static readonly Lazy<NativeFunctions.CurlEasyDuplicateDelegate> _curlEasyDuplicate =
            new Lazy<NativeFunctions.CurlEasyDuplicateDelegate>(() => GetFunction<NativeFunctions.CurlEasyDuplicateDelegate>("curl_easy_duphandle"));

        public static IntPtr Curl_easy_init() => _curlEasyInit.Value();
        public static void Curl_easy_cleanup(IntPtr handle) => _curlEasyCleanup.Value(handle);
        public static CURLcode Curl_easy_perform(IntPtr handle) => _curlEasyPerform.Value(handle);
        public static IntPtr Curl_easy_strerror(CURLcode code) => _curlEasyStrerror.Value(code);
        public static void Curl_easy_reset(IntPtr handle) => _curlEasyReset.Value(handle);
        public static IntPtr Curl_easy_duphandle(IntPtr handle) => _curlEasyDuplicate.Value(handle);

        // ================================================================
        //  curl_easy_setopt — перегрузки по типу последнего аргумента
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlEasySetoptStringDelegate> _curlEasySetoptString =
            new Lazy<NativeFunctions.CurlEasySetoptStringDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptStringDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptLongDelegate> _curlEasySetoptLong =
            new Lazy<NativeFunctions.CurlEasySetoptLongDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptLongDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptOffTDelegate> _curlEasySetoptOffT =
            new Lazy<NativeFunctions.CurlEasySetoptOffTDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptOffTDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptPtrDelegate> _curlEasySetoptPtr =
            new Lazy<NativeFunctions.CurlEasySetoptPtrDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptPtrDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptWriteCallbackDelegate> _curlEasySetoptWriteCallback =
            new Lazy<NativeFunctions.CurlEasySetoptWriteCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptWriteCallbackDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptHeaderCallbackDelegate> _curlEasySetoptHeaderCallback =
            new Lazy<NativeFunctions.CurlEasySetoptHeaderCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptHeaderCallbackDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptReadCallbackDelegate> _curlEasySetoptReadCallback =
            new Lazy<NativeFunctions.CurlEasySetoptReadCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptReadCallbackDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptXferInfoCallbackDelegate> _curlEasySetoptXferInfoCallback =
            new Lazy<NativeFunctions.CurlEasySetoptXferInfoCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptXferInfoCallbackDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptDebugCallbackDelegate> _curlEasySetoptDebugCallback =
            new Lazy<NativeFunctions.CurlEasySetoptDebugCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptDebugCallbackDelegate>("curl_easy_setopt"));

        private static readonly Lazy<NativeFunctions.CurlEasySetoptSslCtxCallbackDelegate> _curlEasySetoptSslCtxCallback =
            new Lazy<NativeFunctions.CurlEasySetoptSslCtxCallbackDelegate>(() => GetFunction<NativeFunctions.CurlEasySetoptSslCtxCallbackDelegate>("curl_easy_setopt"));

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, string value)
            => _curlEasySetoptString.Value(handle, option, value);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, long value)
            => _curlEasySetoptLong.Value(handle, option, value);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, IntPtr value)
            => _curlEasySetoptPtr.Value(handle, option, value);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_write_callback callback)
            => _curlEasySetoptWriteCallback.Value(handle, option, callback);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_header_callback callback)
            => _curlEasySetoptHeaderCallback.Value(handle, option, callback);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_read_callback callback)
            => _curlEasySetoptReadCallback.Value(handle, option, callback);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_xferinfo_callback callback)
            => _curlEasySetoptXferInfoCallback.Value(handle, option, callback);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_debug_callback callback)
            => _curlEasySetoptDebugCallback.Value(handle, option, callback);

        public static CURLcode Curl_easy_setopt(IntPtr handle, CURLoption option, Delegates.curl_ssl_ctx_callback callback)
            => _curlEasySetoptSslCtxCallback.Value(handle, option, callback);

        // ================================================================
        //  curl_easy_getinfo — перегрузки по типу out-параметра
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlEasyGetinfoLongDelegate> _curlEasyGetinfoLong =
            new Lazy<NativeFunctions.CurlEasyGetinfoLongDelegate>(() => GetFunction<NativeFunctions.CurlEasyGetinfoLongDelegate>("curl_easy_getinfo"));

        private static readonly Lazy<NativeFunctions.CurlEasyGetinfoOffTDelegate> _curlEasyGetinfoOffT =
            new Lazy<NativeFunctions.CurlEasyGetinfoOffTDelegate>(() => GetFunction<NativeFunctions.CurlEasyGetinfoOffTDelegate>("curl_easy_getinfo"));

        private static readonly Lazy<NativeFunctions.CurlEasyGetinfoDoubleDelegate> _curlEasyGetinfoDouble =
            new Lazy<NativeFunctions.CurlEasyGetinfoDoubleDelegate>(() => GetFunction<NativeFunctions.CurlEasyGetinfoDoubleDelegate>("curl_easy_getinfo"));

        private static readonly Lazy<NativeFunctions.CurlEasyGetinfoPtrDelegate> _curlEasyGetinfoPtr =
            new Lazy<NativeFunctions.CurlEasyGetinfoPtrDelegate>(() => GetFunction<NativeFunctions.CurlEasyGetinfoPtrDelegate>("curl_easy_getinfo"));

        public static CURLcode Curl_easy_getinfo(IntPtr handle, CURLINFO info, out long value)
            => _curlEasyGetinfoLong.Value(handle, info, out value);

        public static CURLcode Curl_easy_getinfo(IntPtr handle, CURLINFO info, out double value)
            => _curlEasyGetinfoDouble.Value(handle, info, out value);

        public static CURLcode Curl_easy_getinfo(IntPtr handle, CURLINFO info, out IntPtr value)
            => _curlEasyGetinfoPtr.Value(handle, info, out value);

        // ================================================================
        //  curl_slist_*
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlSlistAppendDelegate> _curlSlistAppend =
            new Lazy<NativeFunctions.CurlSlistAppendDelegate>(() => GetFunction<NativeFunctions.CurlSlistAppendDelegate>("curl_slist_append"));

        private static readonly Lazy<NativeFunctions.CurlSlistFreeAllDelegate> _curlSlistFreeAll =
            new Lazy<NativeFunctions.CurlSlistFreeAllDelegate>(() => GetFunction<NativeFunctions.CurlSlistFreeAllDelegate>("curl_slist_free_all"));

        public static IntPtr Curl_slist_append(IntPtr list, string data)
            => _curlSlistAppend.Value(list, data);

        public static void Curl_slist_free_all(IntPtr list)
            => _curlSlistFreeAll.Value(list);

        // ================================================================
        //  curl_multi_*
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlMultiInitDelegate> _curlMultiInit =
            new Lazy<NativeFunctions.CurlMultiInitDelegate>(() => GetFunction<NativeFunctions.CurlMultiInitDelegate>("curl_multi_init"));

        private static readonly Lazy<NativeFunctions.CurlMultiCleanupDelegate> _curlMultiCleanup =
            new Lazy<NativeFunctions.CurlMultiCleanupDelegate>(() => GetFunction<NativeFunctions.CurlMultiCleanupDelegate>("curl_multi_cleanup"));

        private static readonly Lazy<NativeFunctions.CurlMultiAddHandleDelegate> _curlMultiAddHandle =
            new Lazy<NativeFunctions.CurlMultiAddHandleDelegate>(() => GetFunction<NativeFunctions.CurlMultiAddHandleDelegate>("curl_multi_add_handle"));

        private static readonly Lazy<NativeFunctions.CurlMultiRemoveHandleDelegate> _curlMultiRemoveHandle =
            new Lazy<NativeFunctions.CurlMultiRemoveHandleDelegate>(() => GetFunction<NativeFunctions.CurlMultiRemoveHandleDelegate>("curl_multi_remove_handle"));

        private static readonly Lazy<NativeFunctions.CurlMultiPerformDelegate> _curlMultiPerform =
            new Lazy<NativeFunctions.CurlMultiPerformDelegate>(() => GetFunction<NativeFunctions.CurlMultiPerformDelegate>("curl_multi_perform"));

        private static readonly Lazy<NativeFunctions.CurlMultiPollDelegate> _curlMultiPoll =
            new Lazy<NativeFunctions.CurlMultiPollDelegate>(() => GetFunction<NativeFunctions.CurlMultiPollDelegate>("curl_multi_poll"));

        private static readonly Lazy<NativeFunctions.CurlMultiWaitDelegate> _curlMultiWait =
            new Lazy<NativeFunctions.CurlMultiWaitDelegate>(() => GetFunction<NativeFunctions.CurlMultiWaitDelegate>("curl_multi_wait"));

        private static readonly Lazy<NativeFunctions.CurlMultiInfoReadDelegate> _curlMultiInfoRead =
            new Lazy<NativeFunctions.CurlMultiInfoReadDelegate>(() => GetFunction<NativeFunctions.CurlMultiInfoReadDelegate>("curl_multi_info_read"));

        private static readonly Lazy<NativeFunctions.CurlMultiStrerrorDelegate> _curlMultiStrerror =
            new Lazy<NativeFunctions.CurlMultiStrerrorDelegate>(() => GetFunction<NativeFunctions.CurlMultiStrerrorDelegate>("curl_multi_strerror"));

        private static readonly Lazy<NativeFunctions.CurlMultiWakeupDelegate> _curlMultiWakeup =
            new Lazy<NativeFunctions.CurlMultiWakeupDelegate>(() => GetFunction<NativeFunctions.CurlMultiWakeupDelegate>("curl_multi_wakeup"));

        private static readonly Lazy<NativeFunctions.CurlMultiSetoptLongDelegate> _curlMultiSetoptLong =
            new Lazy<NativeFunctions.CurlMultiSetoptLongDelegate>(() => GetFunction<NativeFunctions.CurlMultiSetoptLongDelegate>("curl_multi_setopt"));

        private static readonly Lazy<NativeFunctions.CurlMultiSetoptPtrDelegate> _curlMultiSetoptPtr =
            new Lazy<NativeFunctions.CurlMultiSetoptPtrDelegate>(() => GetFunction<NativeFunctions.CurlMultiSetoptPtrDelegate>("curl_multi_setopt"));

        public static IntPtr Curl_multi_init() => _curlMultiInit.Value();
        public static CURLMcode Curl_multi_cleanup(IntPtr multi) => _curlMultiCleanup.Value(multi);
        public static CURLMcode Curl_multi_add_handle(IntPtr multi, IntPtr easy) => _curlMultiAddHandle.Value(multi, easy);
        public static CURLMcode Curl_multi_remove_handle(IntPtr multi, IntPtr easy) => _curlMultiRemoveHandle.Value(multi, easy);
        public static CURLMcode Curl_multi_perform(IntPtr multi, out int runningHandles) => _curlMultiPerform.Value(multi, out runningHandles);
        public static CURLMcode Curl_multi_poll(IntPtr multi, IntPtr extraFds, uint extraNfds, int timeoutMs, out int numFds) => _curlMultiPoll.Value(multi, extraFds, extraNfds, timeoutMs, out numFds);
        public static CURLMcode Curl_multi_wait(IntPtr multi, IntPtr extraFds, uint extraNfds, int timeoutMs, out int numFds) => _curlMultiWait.Value(multi, extraFds, extraNfds, timeoutMs, out numFds);
        public static IntPtr Curl_multi_info_read(IntPtr multi, out int msgsInQueue) => _curlMultiInfoRead.Value(multi, out msgsInQueue);
        public static IntPtr Curl_multi_strerror(CURLMcode code) => _curlMultiStrerror.Value(code);
        public static CURLMcode Curl_multi_wakeup(IntPtr multi) => _curlMultiWakeup.Value(multi);
        public static CURLMcode Curl_multi_setopt(IntPtr multi, int option, long value) => _curlMultiSetoptLong.Value(multi, option, value);
        public static CURLMcode Curl_multi_setopt(IntPtr multi, int option, IntPtr value) => _curlMultiSetoptPtr.Value(multi, option, value);

        // ================================================================
        //  curl_version / curl_free
        // ================================================================

        private static readonly Lazy<NativeFunctions.CurlVersionDelegate> _curlVersion =
            new Lazy<NativeFunctions.CurlVersionDelegate>(() => GetFunction<NativeFunctions.CurlVersionDelegate>("curl_version"));

        private static readonly Lazy<NativeFunctions.CurlVersionInfoDelegate> _curlVersionInfo =
            new Lazy<NativeFunctions.CurlVersionInfoDelegate>(() => GetFunction<NativeFunctions.CurlVersionInfoDelegate>("curl_version_info"));

        private static readonly Lazy<NativeFunctions.CurlFreeDelegate> _curlFree =
            new Lazy<NativeFunctions.CurlFreeDelegate>(() => GetFunction<NativeFunctions.CurlFreeDelegate>("curl_free"));

        public static IntPtr Curl_version() => _curlVersion.Value();
        public static IntPtr Curl_version_info(int age) => _curlVersionInfo.Value(age);
        public static void Curl_free(IntPtr ptr) => _curlFree.Value(ptr);

        // ================================================================
        //  Утилиты
        // ================================================================

        /// <summary>
        /// Читает C-строку из <paramref name="ptr"/> как UTF-8.
        /// </summary>
        public static string? PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var length = 0;
            while (Marshal.ReadByte(ptr, length) != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            var bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}