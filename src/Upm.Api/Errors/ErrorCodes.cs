namespace Upm.Api.Errors;

/// <summary>Mã lỗi kỹ thuật chung (mã nghiệp vụ A01…G06 nằm ở tầng Domain/Application).</summary>
public static class ErrorCodes
{
    public const string BadRequest = "UPM_BAD_REQUEST";
    public const string Validation = "UPM_VALIDATION";
    public const string Unauthorized = "UPM_UNAUTHORIZED";
    public const string Forbidden = "UPM_FORBIDDEN";
    public const string NotFound = "UPM_NOT_FOUND";
    public const string Conflict = "UPM_CONFLICT";
    public const string TooManyRequests = "UPM_TOO_MANY_REQUESTS";
    public const string Internal = "UPM_INTERNAL_ERROR";
    public const string Unknown = "UPM_ERROR";
}
