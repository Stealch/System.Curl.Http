namespace System.Curl.Http
{
    /// <summary>
    /// HTTP-коды состояния. Точная копия <c>System.Net.HttpStatusCode</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Числовые значения соответствуют RFC 9110 и реестру IANA.
    /// </para>
    /// <para>
    /// Если сервер вернёт код, отсутствующий в enum, его можно представить
    /// приведением: <c>(HttpStatusCode)code</c>.
    /// </para>
    /// </remarks>
    public enum HttpStatusCode
    {
        // ===== 1xx Informational =====

        /// <summary>100 Continue.</summary>
        Continue = 100,

        /// <summary>101 Switching Protocols.</summary>
        SwitchingProtocols = 101,

        /// <summary>102 Processing (WebDAV).</summary>
        Processing = 102,

        /// <summary>103 Early Hints.</summary>
        EarlyHints = 103,

        // ===== 2xx Success =====

        /// <summary>200 OK.</summary>
        OK = 200,

        /// <summary>201 Created.</summary>
        Created = 201,

        /// <summary>202 Accepted.</summary>
        Accepted = 202,

        /// <summary>203 Non-Authoritative Information.</summary>
        NonAuthoritativeInformation = 203,

        /// <summary>204 No Content.</summary>
        NoContent = 204,

        /// <summary>205 Reset Content.</summary>
        ResetContent = 205,

        /// <summary>206 Partial Content.</summary>
        PartialContent = 206,

        /// <summary>207 Multi-Status (WebDAV).</summary>
        MultiStatus = 207,

        /// <summary>208 Already Reported (WebDAV).</summary>
        AlreadyReported = 208,

        /// <summary>226 IM Used (RFC 3229).</summary>
        IMUsed = 226,

        // ===== 3xx Redirection =====

        /// <summary>300 Multiple Choices.</summary>
        MultipleChoices = 300,

        /// <summary>301 Moved Permanently.</summary>
        MovedPermanently = 301,

        /// <summary>302 Found.</summary>
        Found = 302,

        /// <summary>303 See Other.</summary>
        SeeOther = 303,

        /// <summary>304 Not Modified.</summary>
        NotModified = 304,

        /// <summary>305 Use Proxy.</summary>
        UseProxy = 305,

        /// <summary>307 Temporary Redirect.</summary>
        TemporaryRedirect = 307,

        /// <summary>308 Permanent Redirect.</summary>
        PermanentRedirect = 308,

        // ===== 4xx Client Error =====

        /// <summary>400 Bad Request.</summary>
        BadRequest = 400,

        /// <summary>401 Unauthorized.</summary>
        Unauthorized = 401,

        /// <summary>402 Payment Required.</summary>
        PaymentRequired = 402,

        /// <summary>403 Forbidden.</summary>
        Forbidden = 403,

        /// <summary>404 Not Found.</summary>
        NotFound = 404,

        /// <summary>405 Method Not Allowed.</summary>
        MethodNotAllowed = 405,

        /// <summary>406 Not Acceptable.</summary>
        NotAcceptable = 406,

        /// <summary>407 Proxy Authentication Required.</summary>
        ProxyAuthenticationRequired = 407,

        /// <summary>408 Request Timeout.</summary>
        RequestTimeout = 408,

        /// <summary>409 Conflict.</summary>
        Conflict = 409,

        /// <summary>410 Gone.</summary>
        Gone = 410,

        /// <summary>411 Length Required.</summary>
        LengthRequired = 411,

        /// <summary>412 Precondition Failed.</summary>
        PreconditionFailed = 412,

        /// <summary>413 Payload Too Large.</summary>
        RequestEntityTooLarge = 413,

        /// <summary>414 URI Too Long.</summary>
        RequestUriTooLong = 414,

        /// <summary>415 Unsupported Media Type.</summary>
        UnsupportedMediaType = 415,

        /// <summary>416 Range Not Satisfiable.</summary>
        RequestedRangeNotSatisfiable = 416,

        /// <summary>417 Expectation Failed.</summary>
        ExpectationFailed = 417,

        /// <summary>418 I'm a teapot (RFC 2324).</summary>
        ImATeapot = 418,

        /// <summary>421 Misdirected Request (RFC 7540).</summary>
        MisdirectedRequest = 421,

        /// <summary>422 Unprocessable Entity (WebDAV).</summary>
        UnprocessableEntity = 422,

        /// <summary>423 Locked (WebDAV).</summary>
        Locked = 423,

        /// <summary>424 Failed Dependency (WebDAV).</summary>
        FailedDependency = 424,

        /// <summary>426 Upgrade Required.</summary>
        UpgradeRequired = 426,

        /// <summary>428 Precondition Required (RFC 6585).</summary>
        PreconditionRequired = 428,

        /// <summary>429 Too Many Requests (RFC 6585).</summary>
        TooManyRequests = 429,

        /// <summary>431 Request Header Fields Too Large (RFC 6585).</summary>
        RequestHeaderFieldsTooLarge = 431,

        /// <summary>451 Unavailable For Legal Reasons (RFC 7725).</summary>
        UnavailableForLegalReasons = 451,

        // ===== 5xx Server Error =====

        /// <summary>500 Internal Server Error.</summary>
        InternalServerError = 500,

        /// <summary>501 Not Implemented.</summary>
        NotImplemented = 501,

        /// <summary>502 Bad Gateway.</summary>
        BadGateway = 502,

        /// <summary>503 Service Unavailable.</summary>
        ServiceUnavailable = 503,

        /// <summary>504 Gateway Timeout.</summary>
        GatewayTimeout = 504,

        /// <summary>505 HTTP Version Not Supported.</summary>
        HttpVersionNotSupported = 505,

        /// <summary>506 Variant Also Negotiates (RFC 2295).</summary>
        VariantAlsoNegotiates = 506,

        /// <summary>507 Insufficient Storage (WebDAV).</summary>
        InsufficientStorage = 507,

        /// <summary>508 Loop Detected (WebDAV).</summary>
        LoopDetected = 508,

        /// <summary>510 Not Extended (RFC 2774).</summary>
        NotExtended = 510,

        /// <summary>511 Network Authentication Required (RFC 6585).</summary>
        NetworkAuthenticationRequired = 511,
    }
}