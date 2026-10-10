#nullable enable

using System;

namespace System.Curl.Http
{
    /// <summary>
    /// HTTP-метод. Модель полностью повторяет <c>System.Net.Http.HttpMethod</c>:
    /// статические предопределённые экземпляры, конструктор из строки,
    /// сравнение по строковому значению.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Сравнение регистрозависимо (как в BCL): <c>"GET"</c> и <c>"get"</c> — разные методы.
    /// Это соответствует RFC 9110: имена методов регистрозависимы.
    /// </para>
    /// <para>
    /// Класс неизменяем. Экземпляры можно безопасно шарить между потоками.
    /// </para>
    /// </remarks>
    public class HttpMethod : IEquatable<HttpMethod>
    {
        /// <summary>
        /// Создаёт HTTP-метод из строки.
        /// </summary>
        /// <param name="method">Имя метода. Не может быть <see langword="null"/> или пустым.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="method"/> пуст или состоит только из пробелов.
        /// </exception>
        public HttpMethod(string method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("Имя HTTP-метода не может быть пустым.", nameof(method));
            }

            Method = method;
        }

        /// <summary>
        /// Строковое значение метода.
        /// </summary>
        public string Method { get; }

        // ================================================================
        //  Предопределённые методы
        // ================================================================

        /// <summary>GET.</summary>
        public static HttpMethod Get { get; } = new HttpMethod("GET");

        /// <summary>POST.</summary>
        public static HttpMethod Post { get; } = new HttpMethod("POST");

        /// <summary>PUT.</summary>
        public static HttpMethod Put { get; } = new HttpMethod("PUT");

        /// <summary>DELETE.</summary>
        public static HttpMethod Delete { get; } = new HttpMethod("DELETE");

        /// <summary>PATCH.</summary>
        public static HttpMethod Patch { get; } = new HttpMethod("PATCH");

        /// <summary>HEAD.</summary>
        public static HttpMethod Head { get; } = new HttpMethod("HEAD");

        /// <summary>OPTIONS.</summary>
        public static HttpMethod Options { get; } = new HttpMethod("OPTIONS");

        /// <summary>TRACE.</summary>
        public static HttpMethod Trace { get; } = new HttpMethod("TRACE");

        /// <summary>CONNECT.</summary>
        public static HttpMethod Connect { get; } = new HttpMethod("CONNECT");

        // ================================================================
        //  Равенство
        // ================================================================

        /// <inheritdoc />
        public bool Equals(HttpMethod? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Method, other.Method, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is HttpMethod other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Method);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Method;
        }

        /// <summary>
        /// Сравнивает два HTTP-метода по строковому значению (регистрозависимо).
        /// </summary>
        public static bool operator ==(HttpMethod? left, HttpMethod? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Проверяет неравенство двух HTTP-методов.
        /// </summary>
        public static bool operator !=(HttpMethod? left, HttpMethod? right)
        {
            return !(left == right);
        }
    }
}