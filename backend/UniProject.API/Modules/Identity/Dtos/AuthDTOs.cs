namespace UniProject.API.Modules.Identity
{
    public record LoginRequest(string Username, string Password);

    public record AuthResponse(
        int UserId,
        string Username,
        string FullName,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpiryTime);

    public record RefreshTokenRequest(string AccessToken, string RefreshToken);

    public record ChangePasswordRequest(string OldPassword, string NewPassword);

    public record ResetPasswordRequest(string NewPassword);

    public record CreateAccountRequest(
        string Username,
        string Password,
        string Email,
        string FullName,
        string Role);

    public record AssignRoleRequest(string Role);

    public record UserDto(
        int Id,
        string Username,
        string FullName,
        string Email,
        string Role,
        bool IsLocked,
        DateTime? LastLogin,
        DateTime CreatedAt);
}
