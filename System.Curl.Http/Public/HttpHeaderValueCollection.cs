#nullable enable

using System;
using System.Collections;
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
    /// Модель повторяет <c>System.Net.Http.Headers.HttpHeaderValueCollection&lt;T&gt;</c>,
    /// но привязана к <see cref="HttpHeaders"/>, а не к BCL-овскому <c>HttpHeaders</c>.
    /// </para>
    /// <para>
    /// Экземпляр не хранит значения сам — он читает и пишет в родительский
    /// <see cref="HttpHeaders"/> по имени заголовка. Это позволяет коллекции
    /// и заголовкам быть согласованными.
    /// </para>
    /// </remarks>
    public class HttpHeaderValueCollection<T> : ICollection<T>
        where T : class
    {
        private readonly HttpHeaders _headers;
        private readonly string _name;
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
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _validator = validator;
        }

        /// <summary>
        /// Имя заголовка, к которому привязана коллекция.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Количество значений заголовка.
        /// </summary>
        public int Count => _headers.GetValues(_name).Count();

        /// <summary>
        /// Коллекция только для чтения? Всегда <see langword="false"/>.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Добавляет значение в заголовок.
        /// </summary>
        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _validator?.Invoke(_headers, _name, item);
            _headers.Add(_name, item.ToString()!);
        }

        /// <summary>
        /// Удаляет все значения заголовка.
        /// </summary>
        public void Clear()
        {
            _headers.Remove(_name);
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
            foreach (var value in _headers.GetValues(_name))
            {
                if (string.Equals(value, target, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Копирует строковые значения заголовка в массив.
        /// </summary>
        /// <remarks>
        /// Из-за того, что в хранилище лежат строки, а не экземпляры <typeparamref name="T"/>,
        /// полноценная типизированная копия невозможна. Метод копирует
        /// строковые представления, приведённые к <typeparamref name="T"/> через
        /// <see cref="Convert"/> — если тип совместим. В большинстве случаев это
        /// не сработает, и метод бросает <see cref="NotSupportedException"/>.
        /// </remarks>
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException(
                "CopyTo не поддерживается: строковое представление заголовков нельзя " +
                "преобразовать в экземпляры типа T без парсера.");
        }

        /// <summary>
        /// Удаляет первое вхождение значения (по строковому представлению).
        /// </summary>
        public bool Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var target = item.ToString();
            var values = new List<string>(_headers.GetValues(_name));
            var index = values.FindIndex(v => string.Equals(v, target, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            values.RemoveAt(index);
            if (values.Count == 0)
            {
                _headers.Remove(_name);
            }
            else
            {
                _headers.Remove(_name);
                foreach (var value in values)
                {
                    _headers.Add(_name, value);
                }
            }

            return true;
        }

        /// <summary>
        /// Перечисляет строковые значения заголовка.
        /// </summary>
        /// <remarks>
        /// Возвращает <see cref="string"/> как <typeparamref name="T"/>, что
        /// небезопасно. Используйте <see cref="ToString"/> или прямые вызовы
        /// <c>HttpHeaders.GetValues</c> для получения строк.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var value in _headers.GetValues(_name))
            {
                yield return (T)(object)value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Возвращает объединённое строковое представление всех значений,
        /// разделённых запятыми.
        /// </summary>
        public override string ToString()
        {
            return string.Join(", ", _headers.GetValues(_name));
        }
    }
}