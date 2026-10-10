#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Curl.Http
{
    /// <summary>
    /// Коллекция значений одного HTTP-заголовка. Позволяет читать, добавлять
    /// и удалять значения, а также сериализовать их в строку через <see cref="ToString"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения заголовка (например, <c>MediaTypeWithQualityHeaderValue</c>).</typeparam>
    /// <remarks>
    /// <para>
    /// Модель адаптирована из <c>System.Net.Http.Headers.HttpHeaderValueCollection&lt;T&gt;</c>,
    /// но привязана к <see cref="HttpHeaders"/>, а не к BCL-овскому <c>HttpHeaders</c>.
    /// </para>
    /// <para>
    /// <b>Отличие от BCL:</b> коллекция <b>не реализует</b> <see cref="IEnumerable{T}"/>.
    /// Это осознанное решение: значения заголовков хранятся как строки, а их
    /// типизированное представление (<typeparamref name="T"/>) требует парсинга,
    /// специфичного для каждого типа. BCL использует для этого внутренний
    /// <c>HttpHeaderParser</c>, который не экспортируется.
    /// </para>
    /// <para>
    /// <b>Как перечислить значения:</b> используйте <see cref="HttpHeaders.GetValues"/>,
    /// который возвращает <see cref="IEnumerable{String}"/>:
    /// <code>
    /// foreach (var value in request.Headers.GetValues("Accept"))
    /// {
    ///     // value — string
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// Экземпляр не хранит значения сам — он читает и пишет в родительский
    /// <see cref="HttpHeaders"/> по имени заголовка.
    /// </para>
    /// </remarks>
    public class HttpHeaderValueCollection<T>
        where T : class
    {
        private readonly HttpHeaders _headers;
        private readonly Action<HttpHeaders, string, T>? _validator;

        /// <summary>
        /// Создаёт коллекцию значений для заголовка с указанным именем.
        /// </summary>
        /// <param name="headers">Родительская коллекция заголовков.</param>
        /// <param name="name">Имя заголовка.</param>
        /// <param name="validator">
        /// Необязательный валидатор, вызываемый при добавлении значения.
        /// Может бросить исключение, если значение недопустимо.
        /// </param>
        internal HttpHeaderValueCollection(
            HttpHeaders headers,
            string name,
            Action<HttpHeaders, string, T>? validator = null)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _validator = validator;
        }

        /// <summary>
        /// Имя заголовка, к которому привязана коллекция.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Количество значений заголовка.
        /// </summary>
        public int Count => _headers.GetValues(Name).Count();

        /// <summary>
        /// Коллекция только для чтения? Всегда <see langword="false"/>.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Добавляет значение в заголовок.
        /// </summary>
        /// <param name="item">Значение заголовка.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="item"/> равен <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Валидатор отклонил значение.
        /// </exception>
        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _validator?.Invoke(_headers, Name, item);

            var serialized = item.ToString();
            if (string.IsNullOrEmpty(serialized))
            {
                throw new InvalidOperationException(
                    $"Значение заголовка '{Name}' сериализуется в пустую строку.");
            }

            _headers.Add(Name, serialized!);
        }

        /// <summary>
        /// Удаляет все значения заголовка.
        /// </summary>
        public void Clear()
        {
            _headers.Remove(Name);
        }

        /// <summary>
        /// Проверяет наличие значения в заголовке (по строковому представлению).
        /// </summary>
        public bool Contains(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var target = item.ToString();
            foreach (var value in _headers.GetValues(Name))
            {
                if (string.Equals(value, target, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Удаляет первое вхождение значения (по строковому представлению).
        /// </summary>
        /// <returns><see langword="true"/>, если значение было удалено.</returns>
        public bool Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var target = item.ToString();
            var values = new List<string>(_headers.GetValues(Name));
            var index = values.FindIndex(v => string.Equals(v, target, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            values.RemoveAt(index);

            _headers.Remove(Name);
            foreach (var value in values)
            {
                _headers.Add(Name, value);
            }

            return true;
        }

        /// <summary>
        /// Возвращает объединённое строковое представление всех значений,
        /// разделённых запятыми.
        /// </summary>
        public override string ToString()
        {
            return string.Join(", ", _headers.GetValues(Name));
        }
    }
}