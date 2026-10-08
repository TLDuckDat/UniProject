using System.Text.Json.Serialization;

namespace UniProject.Share.Domain
{
    /// <summary>
    /// Đại diện cho kết quả của một chức năng không có giá trị trả về.
    /// </summary>
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public IDictionary<string, string[]>? Errors { get; set; }

        [JsonConstructor]
        public Result(bool success, string message, IDictionary<string, string[]>? errors = null)
        {
            Success = success;
            Message = message;
            Errors = errors;
        }

        public static Result Ok(string message = "Success") => new(true, message);
        public static Result Fail(string message, IDictionary<string, string[]>? errors = null) => new(false, message, errors);
    }

    public class Result<T> : Result
    {
        public T? Data { get; set; }

        [JsonConstructor]
        public Result(bool success, string message, T? data = default, IDictionary<string, string[]>? errors = null)
            : base(success, message, errors)
        {
            Data = data;
        }

        public static Result<T> Ok(T data, string message = "Success") => new(true, message, data);
        public new static Result<T> Fail(string message, IDictionary<string, string[]>? errors = null) => new(false, message, default, errors);
    }

}
