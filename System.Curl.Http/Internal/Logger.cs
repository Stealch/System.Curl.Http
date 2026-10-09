using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

#nullable enable

namespace System.Curl.Http.Internal
{
    /// <summary>
    /// Простой статический логгер с уровнями детализации.
    /// По умолчанию пишет в <see cref="Debug.WriteLine(string)"/>.
    /// Опционально — дублирует в файл рядом с библиотекой.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Настройка:
    /// <list type="bullet">
    ///   <item><see cref="CurrentLevel"/> — уровень детализации. По умолчанию <see cref="LogLevel.Error"/>.</item>
    ///   <item><see cref="LogFilePath"/> — если задан, лог дублируется в файл (append, AutoFlush).</item>
    /// </list>
    /// </para>
    /// <para>
    /// На релизной сборке вызовы <see cref="Debug.WriteLine(string)"/> вырезаются компилятором,
    /// поэтому файловый лог остаётся единственным источником диагностики.
    /// </para>
    /// </remarks>
    internal static class Logger
    {
        private static string? _logFilePath;
        private static readonly object _fileLock = new object();

        /// <summary>
        /// Текущий уровень детализации. По умолчанию <see cref="LogLevel.Error"/>.
        /// </summary>
        public static LogLevel CurrentLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Путь к файлу лога. Если <see langword="null"/> или пусто — файловый вывод отключён.
        /// По умолчанию указывает на <c>System.Curl.Http.log</c> рядом с библиотекой.
        /// </summary>
        public static string? LogFilePath
        {
            get => _logFilePath;
            set => _logFilePath = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Инициализирует <see cref="LogFilePath"/> значением по умолчанию —
        /// файл <c>System.Curl.Http.log</c> рядом с текущей сборкой.
        /// </summary>
        public static void EnableFileLogging()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                LogFilePath = Path.Combine(assemblyDir, "System.Curl.Http.log");
            }
        }

        /// <summary>
        /// Отключает файловый вывод.
        /// </summary>
        public static void DisableFileLogging()
        {
            LogFilePath = null;
        }

        public static void Error(string category, string message)
            => Write(LogLevel.Error, category, message, null);

        public static void Error(string category, string message, Exception exception)
            => Write(LogLevel.Error, category, message, exception);

        public static void Warning(string category, string message)
            => Write(LogLevel.Warning, category, message, null);

        public static void Warning(string category, string message, Exception exception)
            => Write(LogLevel.Warning, category, message, exception);

        public static void Info(string category, string message)
            => Write(LogLevel.Info, category, message, null);

        public static void Debug(string category, string message)
            => Write(LogLevel.Trace, category, message, null);

        public static void Trace(string category, string message)
            => Write(LogLevel.Trace, category, message, null);

        private static void Write(LogLevel level, string category, string message, Exception? exception)
        {
            if (level == LogLevel.None || level > CurrentLevel)
            {
                return;
            }

            var line = FormatLine(level, category, message);

            System.Diagnostics.Debug.WriteLine(line);

            var path = _logFilePath;
            if (path == null)
            {
                return;
            }

            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Файловый лог — вспомогательный. Если не удалось записать,
                // не ломаем основной сценарий.
            }

            if (exception != null)
            {
                var exLine = FormatException(exception);
                System.Diagnostics.Debug.WriteLine(exLine);

                if (path != null)
                {
                    try
                    {
                        lock (_fileLock)
                        {
                            File.AppendAllText(path, exLine + Environment.NewLine, Encoding.UTF8);
                        }
                    }
                    catch
                    {
                        // см. выше
                    }
                }
            }
        }

        private static string FormatLine(LogLevel level, string category, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var levelText = LevelToText(level);
            return $"[{timestamp}] [{levelText}] [{category}] {message}";
        }

        private static string FormatException(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append("    ");
            sb.Append(exception.GetType().FullName);
            sb.Append(": ");
            sb.Append(exception.Message);

            var inner = exception.InnerException;
            for (var depth = 1; inner != null && depth < 8; depth++)
            {
                sb.Append(Environment.NewLine);
                sb.Append("    ---> ");
                sb.Append(inner.GetType().FullName);
                sb.Append(": ");
                sb.Append(inner.Message);
                inner = inner.InnerException;
            }

            return sb.ToString();
        }

        private static string LevelToText(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => "ERROR",
                LogLevel.Warning => "WARN ",
                LogLevel.Info => "INFO ",
                LogLevel.Trace => "TRACE",
                _ => "?????",
            };
        }
    }
}