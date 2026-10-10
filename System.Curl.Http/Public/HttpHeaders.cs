#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Curl.Http
{
    /// <summary>
    /// Базовый класс для коллекций HTTP-заголовков.
    /// Модель повторяет <c>System.Net.Http.Headers.HttpHeaders</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Имена заголовков регистронезависимы, значения — регистрозависимы.
    /// Один заголовок может иметь несколько значений.
    /// </para>
    /// <para>
    /// Не потокобезопасен.
    /// </para>
    /// </remarks>
    public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        private readonly Dictionary<string, List<string>> _store;

        /// <summary>
        /// Создаёт экземпляр с указанным компаратором для имён заголовков.
        /// </summary>
        /// <param name="comparer">
        /// Компаратор для имён заголовков. По умолчанию <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// </param>
        protected HttpHeaders(IEqualityComparer<string>? comparer = null)
        {
            _store = new Dictionary<string, List<string>>(comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Добавляет заголовок с одним значением.
        /// </summary>
        public void Add(string name, string value)
        {
            Add(name, new[] { value });
        }

        /// <summary>
        /// Добавляет заголовок с несколькими значениями.
        /// </summary>
        public void Add(string name, IEnumerable<string> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя заголовка не может быть пустым.", nameof(name));
            }

            if (!_store.TryGetValue(name, out var list))
            {
                list = new List<string>();
                _store[name] = list;
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Значение заголовка не может быть null.", nameof(values));
                }

                list.Add(value);
            }
        }

        /// <summary>
        /// Удаляет все значения указанного заголовка.
        /// </summary>
        /// <returns><see langword="true"/>, если заголовок был удалён.</returns>
        public bool Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _store.Remove(name);
        }

        /// <summary>
        /// Удаляет все заголовки.
        /// </summary>
        public void Clear()
        {
            _store.Clear();
        }

        /// <summary>
        /// Проверяет наличие заголовка с указанным именем.
        /// </summary>
        public bool Contains(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _store.ContainsKey(name);
        }

        /// <summary>
        /// Пытается получить значения указанного заголовка.
        /// </summary>
        public bool TryGetValues(string name, out IEnumerable<string>? values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_store.TryGetValue(name, out var list))
            {
                values = list;
                return true;
            }

            values = null;
            return false;
        }

        /// <summary>
        /// Возвращает значения указанного заголовка. Пустая коллекция, если заголовка нет.
        /// </summary>
        public IEnumerable<string> GetValues(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _store.TryGetValue(name, out var list)
                ? (IEnumerable<string>)list
                : Array.Empty<string>();
        }

        /// <summary>
        /// Возвращает первую строку заголовка в формате <c>"Name: value1, value2"</c>.
        /// </summary>
        public override string ToString()
        {
            if (_store.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var pair in _store)
            {
                sb.Append(pair.Key);
                sb.Append(": ");
                sb.Append(string.Join(", ", pair.Value));
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Возвращает перечислитель по всем парам «имя — значения».
        /// </summary>
        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            foreach (var pair in _store)
            {
                yield return new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // ================================================================
        //  Внутренний API для наследников
        // ================================================================

        /// <summary>
        /// Возвращает единственное значение заголовка или <see langword="null"/>.
        /// Бросает <see cref="InvalidOperationException"/>, если значений больше одного.
        /// </summary>
        protected string? GetSingleValue(string name)
        {
            if (!_store.TryGetValue(name, out var list) || list.Count == 0)
            {
                return null;
            }

            if (list.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Заголовок '{name}' содержит {list.Count} значений; ожидалось одно.");
            }

            return list[0];
        }

        /// <summary>
        /// Возвращает все значения заголовка или пустую коллекцию.
        /// </summary>
        protected IReadOnlyList<string> GetValueList(string name)
        {
            return _store.TryGetValue(name, out var list)
                ? list
                : (IReadOnlyList<string>)Array.Empty<string>();
        }

        /// <summary>
        /// Устанавливает заголовок с единственным значением, заменяя существующие.
        /// </summary>
        protected void SetSingleValue(string name, string? value)
        {
            if (value == null)
            {
                _store.Remove(name);
                return;
            }

            _store[name] = new List<string> { value };
        }

        /// <summary>
        /// Устанавливает заголовок с несколькими значениями, заменяя существующие.
        /// </summary>
        protected void SetValueList(string name, IEnumerable<string>? values)
        {
            if (values == null)
            {
                _store.Remove(name);
                return;
            }

            var list = values.ToList();
            if (list.Count == 0)
            {
                _store.Remove(name);
                return;
            }

            _store[name] = list;
        }
    }
}