namespace UniProject.API.Modules.Identity
{
    /// <summary>
    /// Băm mật khẩu bằng BCrypt (work factor 12). Salt được sinh ngẫu nhiên và nằm sẵn trong chuỗi hash
    /// dạng "$2a$12$...", nên DB chỉ cần lưu một chuỗi duy nhất.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        /// <summary>
        /// Chuỗi hash sai định dạng (ví dụ hash PBKDF2 cũ) được coi là mật khẩu sai thay vì ném lỗi 500.
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Hash có work factor thấp hơn mức hiện tại (vd tài khoản seed dùng work factor 4) cần băm lại khi đăng nhập.
        /// </summary>
        public bool NeedsRehash(string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, WorkFactor);
            }
            catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
            {
                return true;
            }
        }
    }
}
