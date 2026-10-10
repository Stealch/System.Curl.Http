#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Curl.Http
{
    /// <summary>
    /// Коллекция параметров HTTP-запроса. Хранит произвольные значения,
    /// ключом которых служит строка или <see cref="HttpRequestOptionsKey{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модель повторяет <c>System.Net.Http.HttpRequestOptions</c>.
    /// </para>
    /// <para>
    /// Не потокобезопасна. Предназначена для использования в рамках одного запроса.
    /// </para>
    /// </remarks>
    public class HttpRequestOptions : IDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _store;

        /// <summary>
        /// Создаёт пустую коллекцию параметров.
        /// </summary>
        public HttpRequestOptions()
        {
            _store = new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        // ================================================================
        //  Типизированный доступ
        // ================================================================

        /// <summary>
        /// Устанавливает значение по типизированному ключу.
        /// </summary>
        /// <typeparam name="T">Тип значения.</typeparam>
        /// <param name="key">Ключ.</param>
        /// <param name="value">Значение. Может быть <see langword="null"/> для ссылочных типов.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> не задан (имеет <see langword="null"/> в <see cref="HttpRequestOptionsKey{T}.Key"/>).
        /// </exception>
        public void Set<T>(HttpRequestOptionsKey<T> key, T value)
        {
            if (key.Key == null)
            {
                throw new ArgumentNullException(nameof(key), "Ключ не инициализирован.");
            }

            _store[key.Key] = value;
        }

        /// <summary>
        /// Пытается получить значение по типизированному ключу.
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип значения.</typeparam>
        /// <param name="key">Ключ.</param>
        /// <param name="value">Найденное значение или <see langword="default"/>.</param>
        /// <returns><see langword="true"/>, если значение найдено и имеет совместимый тип.</returns>
        public bool TryGetValue<T>(HttpRequestOptionsKey<T> key, out T value)
        {
            if (key.Key != null && _store.TryGetValue(key.Key, out var stored) && stored is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        // ================================================================
        //  IDictionary<string, object?>
        // ================================================================

        /// <inheritdoc />
        public object? this[string key]
        {
            get => _store[key];
            set => _store[key] = value;
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _store.Keys;

        /// <inheritdoc />
        public ICollection<object?> Values => _store.Values;

        /// <inheritdoc />
        public int Count => _store.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(string key, object? value)
        {
            _store.Add(key, value);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, object?> item)
        {
            ((IDictionary<string, object?>)_store).Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _store.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object?> item)
        {
            return ((IDictionary<string, object?>)_store).Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return _store.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ((IDictionary<string, object?>)_store).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            return _store.Remove(key);
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object?> item)
        {
            return ((IDictionary<string, object?>)_store).Remove(item);
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object? value)
        {
            return _store.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}