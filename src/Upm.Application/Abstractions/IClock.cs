namespace Upm.Application.Abstractions;

/// <summary>
/// Nguồn thời gian duy nhất của hệ thống. Không gọi <c>DateTime.Now</c>/<c>DateTimeOffset.Now</c>
/// trực tiếp trong nghiệp vụ – luôn đi qua <see cref="IClock"/> để test được.
/// </summary>
public interface IClock
{
    /// <summary>Thời điểm hiện tại theo múi giờ nghiệp vụ (mặc định Asia/Ho_Chi_Minh).</summary>
    DateTimeOffset Now { get; }

    /// <summary>Ngày hiện tại theo múi giờ nghiệp vụ.</summary>
    DateOnly Today { get; }
}
