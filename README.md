# System.Curl.Http

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-8.0-purple)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Win%207%20SP1%20%7C%208.1%20%7C%2010%20%7C%2011-lightgrey)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

🚀 **Drop-in HTTP-клиент для .NET Framework 4.8**, работающий через `libcurl` и `OpenSSL`. Предоставляет API, полностью совместимый с `System.Net.Http.HttpClient`, но использует собственный современный TLS-стек вместо системного Schannel.

---

## 📖 Зачем это нужно?

> **Проблема:** Стандартный `System.Net.Http.HttpClient` в .NET Framework 4.8 использует **Schannel** — системный TLS-стек Windows. 
> - В **Windows 7** Schannel не поддерживает TLS 1.3.
> - В **Windows 8.1** поддержка ограничена.
> - Современные API (DeepSeek, OpenAI, Anthropic, Discord и др.) требуют TLS 1.2+ со строгим набором шифров, которые Schannel на старых системах часто не поддерживает, что приводит к ошибкам рукопожатия (handshake failures).

✅ **Решение:** `System.Curl.Http` несет с собой `libcurl` с `OpenSSL`. Это гарантирует работу **TLS 1.3, современных шифров и HTTP/2** на любой поддерживаемой версии Windows (начиная с Windows 7 SP1), полностью независимо от ограничений системного Schannel.

---

## 💻 Совместимость

| Платформа | Статус | Примечание |
| :--- | :---: | :--- |
| **Windows 7 SP1** | ✅ Полная | Основная целевая платформа |
| **Windows 8.1** | ✅ Полная | |
| **Windows 10 / 11** | ✅ Полная | |
| **.NET Framework** | ✅ 4.8 | |
| **C#** | ✅ 8.0 | |
| **Архитектура** | x86, x64 | ARM64 — в планах |

---

## 📦 Установка

1. Добавьте ссылку на `System.Curl.Http.dll` или установите NuGet-пакет.
2. Убедитесь, что в проекте установлены следующие зависимости (если не используются встроенные пакеты):
   - `System.Memory`
   - `System.Buffers`
   - `System.Numerics.Vectors`
   - `System.Runtime.CompilerServices.Unsafe`
   - `System.Threading.Tasks.Extensions`

> 💡 **Удобство:** Нативные библиотеки (`libcurl-x64.dll`, `libcurl-x86.dll`) и CA-бандл (`curl-ca-bundle.crt`) встроены в сборку как `EmbeddedResource`. Они автоматически распаковываются во временную директорию при первом обращении, ничего дополнительно скачивать не нужно.

---

## ⚡ Использование

Замена максимально тривиальна. Просто измените пространство имен в `using`. Весь остальной код остается без изменений.

```csharp
// Было: using System.Net.Http;
// Стало:
using System.Curl.Http;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        
        // Современный API, который может не работать на Win7 через Schannel
        var response = await client.GetAsync("https://api.deepseek.com/v1/models");
        response.EnsureSuccessStatusCode();
        
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
    }
}

> ℹ️ **Совет:** Если в одном файле нужен доступ к стандартному BCL-овскому `HttpClient` параллельно с новым, используйте полное имя: `System.Net.Http.HttpClient`.

---

## 🔄 Что подменяется, а что нет

Библиотека предоставляет собственные реализации ключевых типов в пространстве имен `System.Curl.Http`, которые полностью заменяют BCL-овские при соответствующем `using`.

### ✅ Подменяемые типы (`System.Curl.Http`)
* `HttpClient`, `HttpMessageHandler`, `HttpClientHandler`
* `SocketsHttpHandler` *(заглушка для совместимости кода)*
* `HttpRequestMessage`, `HttpResponseMessage`
* `HttpContent` и наследники (`StringContent`, `ByteArrayContent`, `StreamContent`, `FormUrlEncodedContent`, `MultipartFormDataContent`)
* `HttpRequestHeaders`, `HttpResponseHeaders`, `HttpContentHeaders`
* `HttpRequestOptions`, `HttpRequestOptionsKey<T>`
* `HttpMethod`, `HttpStatusCode`, `HttpCompletionOption`, `HttpVersionPolicy`
* `HttpRequestException`, `SslClientAuthenticationOptions`

### ℹ️ НЕ подменяются (используются стандартные BCL)
* `System.Uri` *(парсер URI, не привязан к HTTP-транспорту)*
* `System.Net.Http.Headers.*` *(парсеры заголовков по RFC, не привязаны к TLS/сокетам)*
* `System.Net.Security.SslPolicyErrors`, `EncryptionPolicy`, `SslProtocols`
* `System.Security.Cryptography.X509Certificates.*`

---

## 🏗 Архитектура

```text
+-----------------------------------+
| Ваш код (System.Curl.Http)        |
+-----------------------------------+
                  │
                  ▼
+-----------------------------------+
| CurlHttpMessageHandler            |
+-----------------------------------+
                  │
                  ▼
+-----------------------------------+      +------------------------+
| HttpRequestMapper                 | ---> | CurlEasySession        |
+-----------------------------------+      +------------------------+
                                                  │ (P/Invoke)
                                                  ▼
+-----------------------------------+      +------------------------+
| LibCurlLoader                     | ---> | libcurl + OpenSSL      |  ---> Интернет / API
| (Распаковка из EmbeddedResource)  |      | (x64 / x86 .dll)       |
+-----------------------------------+      +------------------------+
                                                  │ (Callbacks)
                                                  ▼
+-----------------------------------+      +------------------------+
| HttpResponseParser                | <--- | ParsedHttpResponse     |
+-----------------------------------+      +------------------------+
                  │
                  ▼
+-----------------------------------+
| Возврат ответа в ваш код          |
+-----------------------------------+
```

---

## 📝 Логирование

Встроенный логгер по умолчанию пишет только ошибки (`Error`). Доступные уровни: `None`, `Error`, `Warning`, `Info`, `Trace`.

Для включения детального логирования:

```csharp
using System.Curl.Http;

// Включение максимального уровня детализации
Logger.CurrentLevel = LogLevel.Trace;

// Опционально: запись лога в файл (создается рядом с System.Curl.Http.dll)
Logger.EnableFileLogging();
```

---

## ⚠️ Ограничения текущей версии

| Функция | Статус | Комментарий |
| :--- | :---: | :--- |
| **HTTP/2** | ✅ | Поддерживается через `libcurl` (`CURL_HTTP_VERSION_2TLS`). |
| **HTTP/3 (QUIC)** | ❌ | Нет стабильной сборки `libcurl` с QUIC под Windows 7. |
| **Прокси** | ✅ | HTTP, HTTPS, SOCKS4, SOCKS4a, SOCKS5, SOCKS5h. Pre-proxy (цепочка SOCKS → HTTP), аутентификация (`Basic`, `NTLM`, `Negotiate`, `Bearer`, `AWS SigV4`), обход по списку `NoProxy`, отдельные TLS-настройки для HTTPS-прокси, клиентские сертификаты для прокси. |
| **Стриминг ответа** | ⚠️ | `HttpCompletionOption.ResponseHeadersRead` пока читает тело целиком в память. Настоящий async-стриминг через `curl_multi_*` в планах. |
| **CancelPendingRequests** | ⚠️ | `no-op`. Отмена конкретного запроса работает корректно только через `CancellationToken`. |
| **ServerCertificateCustomValidationCallback** | ⚠️ | Сохраняется для совместимости API, но пока не подключён к валидации `libcurl`. |
| **Credentials (сервер)** | ❌ | `HttpClientHandler.Credentials` и `UseDefaultCredentials` сохраняются, но не проброшены в `libcurl`. |
| **Client Certificates (mTLS к серверу)** | ❌ | `SslOptions.ClientCertificates` не реализовано. Для прокси — реализовано. |

---

## ⚙️ Требования к нативным библиотекам

Если вы хотите самостоятельно пересобрать нативные компоненты, `libcurl-x64.dll` / `libcurl-x86.dll` должны быть скомпилированы со следующими флагами:

- **Crypto Backend:** OpenSSL 3.x (или совместимым).
- **TLS:** Обязательная поддержка TLS 1.2 и TLS 1.3.
- **HTTP:** Поддержка HTTP/2 через `nghttp2`.
- **Target OS:** Windows 7 SP1 или выше (toolset должен быть настроен на `_WIN32_WINNT = 0x0601` или совместимый).

> 📌 *Сборки, встроенные в данный репозиторий, предварительно проверены и гарантированно работают на Windows 7 SP1 (x86/x64) и Windows 10/11.*

---

## 🤝 Лицензия

Этот проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE) для подробностей.
