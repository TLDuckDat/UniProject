using UniProject.Share.Domain;

namespace UniProject.API.Modules.Identity
{
    public class TaiKhoan : AuditableEntity<int>
    {
        public string TenDangNhap { get; private set; } = string.Empty;
        public string MatKhauHash { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string HoTen { get; private set; } = string.Empty;
        public int MaVaiTro { get; private set; }
        public bool DaKhoa { get; private set; } = false;
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiryTime { get; private set; }
        public DateTime? LanDangNhapCuoi { get; private set; }

        public virtual VaiTro VaiTro { get; private set; } = null!;

        private TaiKhoan() { }

        public TaiKhoan(string tenDangNhap, string matKhauHash, string email, string hoTen, int maVaiTro)
        {
            TenDangNhap = tenDangNhap.Trim().ToLowerInvariant();
            MatKhauHash = matKhauHash;
            Email = email.Trim().ToLowerInvariant();
            HoTen = hoTen.Trim();
            MaVaiTro = maVaiTro;
            DaKhoa = false;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdatePassword(string newMatKhauHash)
        {
            MatKhauHash = newMatKhauHash;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Lock()
        {
            DaKhoa = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unlock()
        {
            DaKhoa = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignRole(int maVaiTro)
        {
            MaVaiTro = maVaiTro;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRefreshToken(string refreshToken, DateTime expiryTime)
        {
            RefreshToken = refreshToken;
            RefreshTokenExpiryTime = expiryTime;
            LanDangNhapCuoi = DateTime.UtcNow;
        }

        public void RevokeRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiryTime = null;
        }
    }
}
