#nullable enable

using System;
using System.Runtime.InteropServices;

namespace System.Curl.Http.Internal
{
    /// <summary>
    /// RAII-обёртка над <see cref="GCHandle"/> для передачи управляемых объектов
    /// в неуправляемый код (callbacks libcurl) с гарантированным освобождением.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Два режима аллокации:
    /// <list type="bullet">
    ///   <item><see cref="Alloc(object)"/> — обычный <see cref="GCHandleType.Normal"/>.
    ///   Используется для делегатов и managed-объектов состояния (<see cref="System.IO.MemoryStream"/>,
    ///   <see cref="System.Text.StringBuilder"/>), которые не требуют фиксации в памяти.</item>
    ///   <item><see cref="Pin(object)"/> — <see cref="GCHandleType.Pinned"/>.
    ///   Используется для <see cref="byte"/>[]-буферов, чей адрес передаётся в native-код
    ///   и должен оставаться стабильным.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <see cref="GCHandleScope"/> — <see langword="struct"/>, не <see langword="readonly struct"/>,
    /// потому что <see cref="Dispose"/> обнуляет внутренний <see cref="GCHandle"/>.
    /// Это защищает от двойного освобождения и делает безопасным вызов <see cref="Dispose"/>
    /// на <see langword="default"/>(<see cref="GCHandleScope"/>) (когда scope не был создан через фабрику).
    /// </para>
    /// </remarks>
    internal struct GCHandleScope : IDisposable
    {
        private GCHandle _handle;

        private GCHandleScope(GCHandle handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// Аллоцирует обычный <see cref="GCHandleType.Normal"/> на указанный объект.
        /// </summary>
        /// <param name="target">Объект, который должен оставаться живым, пока scope активен.</param>
        public static GCHandleScope Alloc(object target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return new GCHandleScope(GCHandle.Alloc(target, GCHandleType.Normal));
        }

        /// <summary>
        /// Аллоцирует <see cref="GCHandleType.Pinned"/> на указанный объект.
        /// Используется для <see cref="byte"/>[]-буферов, передаваемых в native-код.
        /// </summary>
        /// <param name="target">Объект, который будет зафиксирован в памяти.</param>
        public static GCHandleScope Pin(object target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return new GCHandleScope(GCHandle.Alloc(target, GCHandleType.Pinned));
        }

        /// <summary>
        /// Указатель для передачи в неуправляемый код.
        /// Для <see cref="Alloc(object)"/> — токен, не адрес объекта.
        /// Для <see cref="Pin(object)"/> — адрес зафиксированного объекта.
        /// </summary>
        /// <exception cref="InvalidOperationException">Scope уже освобождён.</exception>
        public IntPtr Handle
        {
            get
            {
                if (!_handle.IsAllocated)
                {
                    throw new InvalidOperationException("GCHandleScope уже освобождён.");
                }

                return GCHandle.ToIntPtr(_handle);
            }
        }

        /// <summary>
        /// Признак того, что scope активен (handle аллоцирован).
        /// </summary>
        public bool IsAllocated => _handle.IsAllocated;

        /// <summary>
        /// Освобождает <see cref="GCHandle"/>. Идемпотентно: повторный вызов безопасен.
        /// </summary>
        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
                _handle = default;
            }
        }
    }
}