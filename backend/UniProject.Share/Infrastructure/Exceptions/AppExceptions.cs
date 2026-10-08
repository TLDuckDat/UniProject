namespace UniProject.Share.Infrastructure.Exceptions
{
    public class AppException : Exception
    {
        public AppException(string message) : base(message) { }
        public AppException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ValidationException : AppException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Dữ liệu không hợp lệ.")
        {
            Errors = errors;
        }

        public ValidationException(string propertyName, string errorMessage)
            : base("Dữ liệu không hợp lệ.")
        {
            Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
        }
    }
    /// <summary>
    /// Raised when a requested resource does not exist.
    /// Typically mapped to HTTP 404.
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key)
            : base($"Không tìm thấy {entityName} với mã '{key}'.") { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn.")
            : base(message) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
            : base(message) { }
    }

    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}
