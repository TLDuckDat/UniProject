namespace Upm.Application.SystemInfo;

/// <summary>Kết quả kiểm tra kết nối API.</summary>
/// <param name="Message">Luôn là <c>pong</c>.</param>
/// <param name="Version">Phiên bản ứng dụng.</param>
/// <param name="ServerTime">Thời gian máy chủ (lấy qua <c>IClock</c>).</param>
public sealed record PingResponse(string Message, string Version, DateTimeOffset ServerTime);
