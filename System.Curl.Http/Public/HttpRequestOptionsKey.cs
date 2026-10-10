#nullable enable

using System;

namespace System.Curl.Http
{
    /// <summary>
    /// Типизированный ключ для доступа к значениям в <see cref="HttpRequestOptions"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения, связанного с ключом.</typeparam>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpRequestOptionsKey&lt;T&gt;</c>.
    /// </para>
    /// <para>
    /// Ключ неизменяем и сравнивается по строковому значению <see cref="Key"/>.
    /// </para>
    /// </remarks>
    public readonly struct HttpRequestOptionsKey<T> : IEquatable<HttpRequestOptionsKey<T>>
    {
        /// <summary>
        /// Строковое имя ключа.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Создаёт типизированный ключ с указанным именем.
        /// </summary>
        /// <param name="key">Имя ключа. Не может быть <see langword="null"/>.</param>
        public HttpRequestOptionsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        /// <inheritdoc />
        public bool Equals(HttpRequestOptionsKey<T> other)
        {
            return string.Equals(Key, other.Key, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is HttpRequestOptionsKey<T> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Key == null ? 0 : StringComparer.Ordinal.GetHashCode(Key);
        }

        /// <summary>
        /// Сравнивает два ключа по строковому имени.
        /// </summary>
        public static bool operator ==(HttpRequestOptionsKey<T> left, HttpRequestOptionsKey<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Проверяет неравенство двух ключей.
        /// </summary>
        public static bool operator !=(HttpRequestOptionsKey<T> left, HttpRequestOptionsKey<T> right)
        {
            return !left.Equals(right);
        }
    }
}