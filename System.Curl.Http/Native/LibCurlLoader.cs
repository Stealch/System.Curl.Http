#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Curl.Http.Internal;

namespace System.Curl.Http.Native
{
    /// <summary>
    /// Загрузчик нативных библиотек libcurl и CA bundle из встроенных ресурсов сборки.
    /// Распаковывает их в уникальную сессионную папку в <c>%TEMP%</c> и загружает
    /// <c>libcurl-x64.dll</c> или <c>libcurl-x86.dll</c> через <c>LoadLibrary</c>
    /// в соответствии с разрядностью процесса.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Разрядность процесса, не системы.</b> 32-битный процесс на 64-битной Windows
    /// должен загружать <c>libcurl-x86.dll</c>, иначе <c>LoadLibrary</c> вернёт ошибку
    /// или загрузит несовместимый модуль.
    /// </para>
    /// <para>
    /// <b>Сессионная папка.</b> Имя папки формируется как <c>{AssemblyGuid}_{timestamp}</c>,
    /// где <c>AssemblyGuid</c> — значение <see cref="GuidAttribute"/> текущей сборки
    /// (без дефисов, в верхнем регистре). Это гарантирует уникальность и отсутствие
    /// конфликтов с другими библиотеками.
    /// </para>
    /// <para>
    /// <b>Без очистки предыдущих сессий.</b> Опыт DeepSeekApiLib показал, что
    /// <c>%TEMP%</c> чистится системой самостоятельно, а попытки удалить старые
    /// папки приводят к ошибкам доступа к загруженным DLL.
    /// </para>
    /// <para>
    /// <b>Идемпотентность.</b> <see cref="EnsureLoaded"/> можно вызывать многократно
    /// из любого потока. Повторные вызовы после первой успешной загрузки — no-op.
    /// </para>
    /// </remarks>
    internal static class LibCurlLoader
    {
        private const string Category = "LibCurlLoader";

        private static readonly object _lock = new object();
        private static bool _isLoaded;
        private static IntPtr _handle;
        private static string? _tempDllPath;
        private static string? _tempCaBundlePath;
        private static string? _tempFolderPath;
        private static string? _sessionId;

        private static string? _assemblyGuid;
        private static string AssemblyGuid
        {
            get
            {
                if (_assemblyGuid == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var guidAttr = assembly.GetCustomAttribute<GuidAttribute>();
                    _assemblyGuid = guidAttr?.Value?.Replace("-", "").ToUpperInvariant()
                                    ?? "SYSTEMCURLHTTP";
                }

                return _assemblyGuid;
            }
        }

        /// <summary>
        /// Гарантирует, что libcurl загружен. Идемпотентно и потокобезопасно.
        /// </summary>
        /// <exception cref="DllNotFoundException">
        /// Если нативная библиотека не найдена в ресурсах или не загрузилась.
        /// </exception>
        public static void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            lock (_lock)
            {
                if (_isLoaded)
                {
                    return;
                }

                try
                {
                    Logger.Info(Category, "Начало загрузки libcurl.");

                    CreateSessionFolder();
                    Logger.Info(Category, $"Сессионная папка: {_tempFolderPath}");

                    ExtractEmbeddedResources();
                    Logger.Info(Category, $"DLL распакована: {_tempDllPath}");
                    Logger.Info(Category, $"CA bundle распакован: {_tempCaBundlePath}");

                    LoadNativeLibrary();
                    Logger.Info(Category, $"libcurl загружен: handle = 0x{_handle.ToInt64():X}");

                    _isLoaded = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(Category, "Не удалось загрузить libcurl.", ex);
                    throw new DllNotFoundException(
                        $"Не удалось загрузить libcurl: {ex.Message}" +
                        (_tempDllPath != null ? $"\nПуть: {_tempDllPath}" : string.Empty),
                        ex);
                }
            }
        }

        /// <summary>
        /// Возвращает handle загруженной библиотеки. Требует предварительного вызова
        /// <see cref="EnsureLoaded"/>.
        /// </summary>
        public static IntPtr GetHandle()
        {
            if (!_isLoaded || _handle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "libcurl не загружен. Вызовите LibCurlLoader.EnsureLoaded() перед использованием.");
            }

            return _handle;
        }

        /// <summary>
        /// Возвращает путь к распакованному <c>curl-ca-bundle.crt</c>.
        /// Может быть <see langword="null"/>, если файл отсутствовал в ресурсах.
        /// </summary>
        public static string? GetCaBundlePath()
        {
            return _tempCaBundlePath;
        }

        /// <summary>
        /// Возвращает путь к сессионной папке. Требует предварительного вызова
        /// <see cref="EnsureLoaded"/>.
        /// </summary>
        public static string GetTempFolderPath()
        {
            if (string.IsNullOrEmpty(_tempFolderPath))
            {
                throw new InvalidOperationException(
                    "Сессионная папка не создана. Вызовите LibCurlLoader.EnsureLoaded().");
            }

            return _tempFolderPath;
        }

        /// <summary>
        /// Возвращает отладочную информацию о состоянии загрузчика.
        /// </summary>
        public static string GetDebugInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            return $"Process: {(IntPtr.Size == 8 ? "x64" : "x86")} (IntPtr.Size={IntPtr.Size}), " +
                   $"OS: {Environment.Is64BitOperatingSystem}, " +
                   $"Loaded: {_isLoaded}, " +
                   $"Handle: 0x{_handle.ToInt64():X}, " +
                   $"Session: {_sessionId ?? "(none)"}, " +
                   $"Resources: {string.Join(", ", resources)}";
        }

        // ================================================================
        //  Внутренняя реализация
        // ================================================================

        private static void CreateSessionFolder()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            _sessionId = $"{AssemblyGuid}_{timestamp}";
            _tempFolderPath = Path.Combine(Path.GetTempPath(), _sessionId);
            Directory.CreateDirectory(_tempFolderPath);
        }

        private static void ExtractEmbeddedResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allResources = assembly.GetManifestResourceNames();

            Logger.Trace(Category, $"Всего ресурсов: {allResources.Length}");

            var is64BitProcess = IntPtr.Size == 8;
            var targetDllName = is64BitProcess ? "libcurl-x64.dll" : "libcurl-x86.dll";

            Logger.Info(Category, $"Целевая DLL: {targetDllName} (процесс {(is64BitProcess ? "x64" : "x86")})");

            var dllResourceName = FindResourceBySuffix(allResources, targetDllName);
            if (dllResourceName == null)
            {
                throw new DllNotFoundException(
                    $"{targetDllName} не найден во встроенных ресурсах. " +
                    $"Доступные ресурсы: {string.Join(", ", allResources)}");
            }

            _tempDllPath = Path.Combine(_tempFolderPath!, targetDllName);
            ExtractResourceToFile(assembly, dllResourceName, _tempDllPath);

            const string caBundleResourceName = "curl-ca-bundle.crt";
            var caResourceName = FindResourceBySuffix(allResources, caBundleResourceName);
            if (caResourceName != null)
            {
                _tempCaBundlePath = Path.Combine(_tempFolderPath!, caBundleResourceName);
                ExtractResourceToFile(assembly, caResourceName, _tempCaBundlePath);
            }
            else
            {
                Logger.Warning(Category, "curl-ca-bundle.crt не найден во встроенных ресурсах.");
            }
        }

        private static string? FindResourceBySuffix(string[] resources, string suffix)
        {
            var dotSuffix = "." + suffix;

            foreach (var resource in resources)
            {
                if (resource.EndsWith(dotSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return resource;
                }
            }

            return null;
        }

        private static void ExtractResourceToFile(Assembly assembly, string resourceName, string targetPath)
        {
            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new FileNotFoundException(
                    $"Не удалось открыть поток ресурса '{resourceName}'.");
            }

            using var fileStream = File.Create(targetPath);
            resourceStream.CopyTo(fileStream);

            Logger.Trace(Category, $"Ресурс '{resourceName}' → '{targetPath}' ({new FileInfo(targetPath).Length} байт).");
        }

        private static void LoadNativeLibrary()
        {
            if (string.IsNullOrEmpty(_tempDllPath) || !File.Exists(_tempDllPath))
            {
                throw new FileNotFoundException($"DLL не найдена: {_tempDllPath}");
            }

            var handle = LoadLibrary(_tempDllPath);
            if (handle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new DllNotFoundException(
                    $"LoadLibrary не смогла загрузить '{_tempDllPath}' (Win32 error {error}).");
            }

            _handle = handle;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);
    }
}