using Microsoft.EntityFrameworkCore;
using UniProject.API.Data;
using UniProject.Share.Application.Interfaces;
using UniProject.Share.Infrastructure.Constants;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Common
{
    /// <summary>
    /// Xác định "ai đang thao tác" từ JWT: tài khoản, vai trò và hồ sơ giảng viên / sinh viên tương ứng.
    /// Mọi kiểm tra quyền trong service đều dựa vào lớp này, KHÔNG dựa vào lecturerId / studentId do client gửi lên.
    /// </summary>
    public class CurrentActor
    {
        private readonly AppDbContext _dbContext;
        private readonly ICurrentUser _currentUser;
        private int? _lecturerId;
        private int? _studentId;

        public CurrentActor(AppDbContext dbContext, ICurrentUser currentUser)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
        }

        public int AccountId => _currentUser.UserID ?? throw new UnauthorizedException();
        public bool IsAdmin => _currentUser.IsInRole(AppRoles.Admin);
        public bool IsLecturer => _currentUser.IsInRole(AppRoles.GiangVien);
        public bool IsStudent => _currentUser.IsInRole(AppRoles.SinhVien);

        /// <summary>
        /// Mã giảng viên của tài khoản hiện tại; lỗi 403 nếu tài khoản chưa liên kết hồ sơ giảng viên.
        /// </summary>
        public async Task<int> GetLecturerIdAsync(CancellationToken cancellationToken = default)
        {
            _lecturerId ??= await _dbContext.GiangViens
                .Where(x => x.MaTaiKhoan == AccountId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ForbiddenException("Tài khoản chưa được liên kết với hồ sơ giảng viên.");
            return _lecturerId.Value;
        }

        /// <summary>
        /// Mã sinh viên của tài khoản hiện tại; lỗi 403 nếu tài khoản chưa liên kết hồ sơ sinh viên.
        /// </summary>
        public async Task<int> GetStudentIdAsync(CancellationToken cancellationToken = default)
        {
            _studentId ??= await _dbContext.SinhViens
                .Where(x => x.MaTaiKhoan == AccountId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ForbiddenException("Tài khoản chưa được liên kết với hồ sơ sinh viên.");
            return _studentId.Value;
        }
    }
}
