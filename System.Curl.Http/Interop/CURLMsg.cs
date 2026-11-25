#nullable enable

using System;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Interop
{
    /// <summary>
    /// Структура <c>CURLMsg</c> из <c>multi.h</c> — сообщение очереди
    /// <c>curl_multi_info_read</c>. Полное соответствие <c>multi.h</c> версии 8.18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// C-объявление:
    /// <code>
    /// typedef struct CURLMsg {
    ///   CURLMSG msg;       /* CURLMSG_NONE, CURLMSG_DONE */
    ///   CURL *easy_handle;
    ///   union {
    ///     void *whatever;
    ///     CURLcode result;
    ///   } data;
    /// } CURLMsg;
    /// </code>
    /// </para>
    /// <para>
    /// <c>CURLMSG</c> — это <c>int</c> (<c>CURLMSG_NONE = 0</c>, <c>CURLMSG_DONE = 1</c>).
    /// <c>data</c> — union, но для <c>CURLMSG_DONE</c> используется <c>data.result</c> (<c>CURLcode</c>, <c>int</c>).
    /// В C# union моделируется как <see cref="IntPtr"/> (размер union = размер указателя),
    /// а <see cref="Result"/> читается как младшие 4 байта union.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CURLMsg
    {
        /// <summary><c>CURLMSG msg</c> — тип сообщения (<c>CURLMSG_DONE = 1</c>).</summary>
        public int Msg;

        /// <summary><c>CURL *easy_handle</c> — easy-хэндл, к которому относится сообщение.</summary>
        public IntPtr EasyHandle;

        /// <summary>
        /// <c>union { void *whatever; CURLcode result; } data</c>.
        /// Для <c>CURLMSG_DONE</c> младшие 4 байта содержат <c>CURLcode</c>.
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// <c>data.result</c> как <c>CURLcode</c> (младшие 4 байта union).
        /// </summary>
        public int Result
        {
            get
            {
                // union занимает IntPtr.Size байт; result — int (4 байта) в начале union.
                // На x86/x64 младшие 4 байта IntPtr — это и есть int (little-endian).
                return unchecked((int)(long)Data);
            }
        }
    }
}