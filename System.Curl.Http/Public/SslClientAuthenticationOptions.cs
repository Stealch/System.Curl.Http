#nullable enable

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace System.Curl.Http
{
    /// <summary>
    /// Опции TLS-соединения. Модель <c>System.Net.Security.SslClientAuthenticationOptions</c>
    /// из .NET Core 2.1+, адаптированная под .NET Framework 4.8 и libcurl.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В .NET Framework 4.8 типа <c>System.Net.Security.SslClientAuthenticationOptions</c>
    /// не существует — он появился только в .NET Core 2.1. Этот класс восполняет
    /// пробел и используется как тип для <c>SslOptions</c> в <see cref="HttpClientHandler"/>
    /// и <see cref="SocketsHttpHandler"/>.
    /// </para>
    /// <para>
    /// Свойства передаются в libcurl через опции <c>CURLOPT_SSL_*</c>
    /// и <c>CURLOPT_SSLCERT</c> / <c>CURLOPT_SSLKEY</c>.
    /// </para>
    /// <para>
    /// Свойства <see cref="ClientCertificates"/> и <see cref="ApplicationProtocols"/>
    /// пока не подключены к транспорту — сохранены для совместимости и будущей
    /// реализации.
    /// </para>
    /// </remarks>
    public class SslClientAuthenticationOptions
    {
        /// <summary>
        /// Целевое имя хоста для SNI (Server Name Indication).
        /// </summary>
        /// <remarks>
        /// Если задано — передаётся в libcurl через <c>CURLOPT_SSL_CTX_FUNCTION</c>
        /// или неявно через URL.
        /// </remarks>
        public string? TargetHost { get; set; }

        /// <summary>
        /// Клиентские сертификаты для mutual TLS.
        /// </summary>
        /// <remarks>
        /// Пока не подключено к транспорту. Для поддержки нужно
        /// экспортировать сертификат в PEM и передать в libcurl
        /// через <c>CURLOPT_SSLCERT</c> и <c>CURLOPT_SSLKEY</c>.
        /// </remarks>
        public X509CertificateCollection? ClientCertificates { get; set; }

        /// <summary>
        /// Разрешённые версии TLS. По умолчанию <see cref="SslProtocols.None"/> —
        /// libcurl использует значение по умолчанию (TLS 1.2+).
        /// </summary>
        /// <remarks>
        /// Маппится в <c>CURLOPT_SSLVERSION</c>:
        /// <list type="bullet">
        ///   <item><see cref="SslProtocols.Tls12"/> → <c>CURL_SSLVERSION_TLSv1_2</c>;</item>
        ///   <item><see cref="SslProtocols.Tls13"/> → <c>CURL_SSLVERSION_TLSv1_3</c>;</item>
        ///   <item><see cref="SslProtocols.None"/> → не устанавливать опцию.</item>
        /// </list>
        /// </remarks>
        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// Режим проверки отзыва сертификата. По умолчанию <see cref="X509RevocationMode.NoCheck"/>.
        /// </summary>
        /// <remarks>
        /// Маппится в <c>CURLOPT_SSL_OPTIONS</c> с флагом <c>CURLSSLOPT_NO_REVOKE</c>
        /// (если <see cref="X509RevocationMode.NoCheck"/>) или без него.
        /// </remarks>
        public X509RevocationMode CertificateRevocationCheckMode { get; set; } = X509RevocationMode.NoCheck;

        /// <summary>
        /// Политика шифрования. По умолчанию <see cref="EncryptionPolicy.RequireEncryption"/>.
        /// </summary>
        /// <remarks>
        /// Пока не подключено к транспорту. libcurl не имеет прямой опции
        /// для этого; проверка выполняется на уровне TLS-стека.
        /// </remarks>
        public EncryptionPolicy EncryptionPolicy { get; set; } = EncryptionPolicy.RequireEncryption;

        /// <summary>
        /// Разрешить переговоры (renegotiation). По умолчанию <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// Пока не подключено к транспорту.
        /// </remarks>
        public bool AllowRenegotiation { get; set; } = true;

        /// <summary>
        /// Список протоколов ALPN (например, <c>"h2"</c>, <c>"http/1.1"</c>).
        /// </summary>
        /// <remarks>
        /// Пока не подключено к транспорту. libcurl управляет ALPN автоматически
        /// через <c>CURLOPT_HTTP_VERSION</c> и <c>CURLOPT_SSL_ENABLE_ALPN</c>.
        /// </remarks>
        public List<string>? ApplicationProtocols { get; set; }
    }
}