using Microsoft.EntityFrameworkCore;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.Share.Infrastructure.Exceptions;

namespace UniProject.API.Modules.Project
{
    /// <summary>
    /// Các kiểm tra quyền trên một đồ án, dùng chung cho module Project và Grading:
    /// - Xem: Admin, Giảng viên, hoặc sinh viên là thành viên đồ án.
    /// - Thành viên: sinh viên đang đăng nhập thuộc đồ án.
    /// - Hướng dẫn: giảng viên đang đăng nhập được phân công hướng dẫn (còn hiệu lực).
    /// </summary>
    public class ProjectAccess
    {
        private readonly AppDbContext _dbContext;
        private readonly CurrentActor _actor;

        public ProjectAccess(AppDbContext dbContext, CurrentActor actor)
        {
            _dbContext = dbContext;
            _actor = actor;
        }

        public async Task EnsureCanViewAsync(int projectId, CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(projectId, cancellationToken);
            if (_actor.IsAdmin || _actor.IsLecturer) return;
            await EnsureMemberAsync(projectId, cancellationToken);
        }

        /// <summary>Trả về mã sinh viên đang đăng nhập nếu là thành viên đồ án, ngược lại 403.</summary>
        public async Task<int> EnsureMemberAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var studentId = await _actor.GetStudentIdAsync(cancellationToken);
            if (!await _dbContext.ThanhVienDoAns.AnyAsync(x => x.MaDoAn == projectId && x.MaSV == studentId, cancellationToken))
                throw new ForbiddenException("Bạn không phải thành viên của đồ án này.");
            return studentId;
        }

        /// <summary>Trả về mã giảng viên đang đăng nhập nếu đang hướng dẫn đồ án, ngược lại 403.</summary>
        public async Task<int> EnsureActiveAdvisorAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var lecturerId = await _actor.GetLecturerIdAsync(cancellationToken);
            if (!await IsActiveAdvisorAsync(projectId, lecturerId, cancellationToken))
                throw new ForbiddenException("Chỉ giảng viên hướng dẫn của đồ án mới được thực hiện thao tác này.");
            return lecturerId;
        }

        public Task<bool> IsActiveAdvisorAsync(int projectId, int lecturerId, CancellationToken cancellationToken = default) =>
            _dbContext.PhanCongHuongDans.AnyAsync(x => x.MaDoAn == projectId && x.MaGV == lecturerId && x.TrangThai, cancellationToken);

        public async Task EnsureExistsAsync(int projectId, CancellationToken cancellationToken = default)
        {
            if (!await _dbContext.DoAns.AnyAsync(x => x.Id == projectId, cancellationToken))
                throw new NotFoundException("Đồ án", projectId);
        }
    }
}
