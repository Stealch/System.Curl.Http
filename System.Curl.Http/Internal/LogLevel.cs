namespace System.Curl.Http.Internal
{
    /// <summary>
    /// Уровень детализации логирования.
    /// </summary>
    internal enum LogLevel
    {
        /// <summary>
        /// Логирование отключено полностью.
        /// </summary>
        None = 0,

        /// <summary>
        /// Только ошибки. Режим по умолчанию.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Ошибки и предупреждения.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Ошибки, предупреждения и информационные сообщения.
        /// </summary>
        Info = 3,

        /// <summary>
        /// Максимальная детализация: ошибки, предупреждения, info, debug, trace.
        /// </summary>
        Trace = 4,
    }
}