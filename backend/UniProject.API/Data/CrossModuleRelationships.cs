using Microsoft.EntityFrameworkCore;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Identity;
using UniProject.API.Modules.Notification;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;

namespace UniProject.API.Data
{
    /// <summary>
    /// Khóa ngoại giữa các module. Entity của mỗi module chỉ giữ mã (int) của module khác, không có navigation,
    /// để các module không phụ thuộc chéo về code; nhưng ở tầng CSDL vẫn có FK thật để đảm bảo toàn vẹn dữ liệu.
    /// Tất cả đều Restrict: không cho xóa bản ghi đang được module khác tham chiếu.
    /// </summary>
    public static class CrossModuleRelationships
    {
        public static void Apply(ModelBuilder b)
        {
            // Academic → Identity
            b.Entity<GiangVien>().HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
            b.Entity<SinhVien>().HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);

            // ProjectPeriod → Academic
            b.Entity<DotDoAn>().HasOne<HocKy>().WithMany().HasForeignKey(x => x.MaHocKy).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DotDoAnLop>().HasOne<Lop>().WithMany().HasForeignKey(x => x.MaLop).OnDelete(DeleteBehavior.Restrict);

            // Topic → Academic, ProjectPeriod
            b.Entity<DeTai>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGVDeXuat).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DeTai>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGVDuyet).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DeTai>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSVDeXuat).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DETAI_SINHVIEN_MaSVDeXuat");
            b.Entity<DeTai>().HasOne<ChuyenNganh>().WithMany().HasForeignKey(x => x.MaChuyenNganh).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DeTaiDotDoAn>().HasOne<DotDoAn>().WithMany().HasForeignKey(x => x.MaDotDoAn).OnDelete(DeleteBehavior.Restrict);

            // Registration → ProjectPeriod, Academic, Topic
            b.Entity<DangKyDeTai>().HasOne<DotDoAn>().WithMany().HasForeignKey(x => x.MaDotDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ThanhVienDangKy>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ChiTietDangKy>().HasOne<DeTaiDotDoAn>().WithMany().HasForeignKey(x => x.MaDeTaiDotDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ChiTietDangKy>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGVDuyet).OnDelete(DeleteBehavior.Restrict);
            b.Entity<YeuCauThayDoi>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSVYeuCau).OnDelete(DeleteBehavior.Restrict);
            b.Entity<YeuCauThayDoi>().HasOne<DeTaiDotDoAn>().WithMany().HasForeignKey(x => x.MaDeTaiDotDoAnMoi).OnDelete(DeleteBehavior.Restrict);
            b.Entity<YeuCauThayDoi>().HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.MaTaiKhoanXuLy).OnDelete(DeleteBehavior.Restrict);

            // Project → ProjectPeriod, Topic, Registration, Academic
            b.Entity<DoAn>().HasOne<DotDoAn>().WithMany().HasForeignKey(x => x.MaDotDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DoAn>().HasOne<DeTai>().WithMany().HasForeignKey(x => x.MaDeTai).OnDelete(DeleteBehavior.Restrict);
            b.Entity<DoAn>().HasOne<DangKyDeTai>().WithMany().HasForeignKey(x => x.MaDangKy).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ThanhVienDoAn>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<PhanCongHuongDan>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<BaoCaoTienDo>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSVNop).OnDelete(DeleteBehavior.Restrict);
            b.Entity<NhanXetTienDo>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<YeuCauChinhSua>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGVYeuCau).OnDelete(DeleteBehavior.Restrict);
            b.Entity<TaiLieuDoAn>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSVNop).OnDelete(DeleteBehavior.Restrict);

            // Defense → Project, ProjectPeriod, Academic
            b.Entity<PhanBienDoAn>().HasOne<DoAn>().WithMany().HasForeignKey(x => x.MaDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<PhanBienDoAn>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<HoiDong>().HasOne<DotDoAn>().WithMany().HasForeignKey(x => x.MaDotDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ThanhVienHoiDong>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<LichBaoVe>().HasOne<DoAn>().WithMany().HasForeignKey(x => x.MaDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<LichBaoVe>().HasOne<DotDoAn>().WithMany().HasForeignKey(x => x.MaDotDoAn).OnDelete(DeleteBehavior.Restrict);

            // Grading → Project, Academic, Defense
            b.Entity<PhieuCham>().HasOne<DoAn>().WithMany().HasForeignKey(x => x.MaDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<PhieuCham>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<PhieuCham>().HasOne<GiangVien>().WithMany().HasForeignKey(x => x.MaGV).OnDelete(DeleteBehavior.Restrict);
            b.Entity<PhieuCham>().HasOne<HoiDong>().WithMany().HasForeignKey(x => x.MaHoiDong).OnDelete(DeleteBehavior.Restrict);
            b.Entity<KetQuaDoAn>().HasOne<DoAn>().WithMany().HasForeignKey(x => x.MaDoAn).OnDelete(DeleteBehavior.Restrict);
            b.Entity<KetQuaDoAn>().HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSV).OnDelete(DeleteBehavior.Restrict);

            // Notification → Identity
            b.Entity<ThongBao>().HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.NguoiTaoId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<NguoiNhanThongBao>().HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
