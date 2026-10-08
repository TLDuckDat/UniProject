using UniProject.Share.Domain;

namespace UniProject.API.Modules.ProjectPeriod
{
    public enum PeriodStatus
    {
        CHUA_MO,
        DANG_MO_DANG_KY,
        DANG_THUC_HIEN,
        DANG_BAO_VE,
        DA_KET_THUC,
        DA_HUY
    }

    public class DotDoAn : AuditableEntity<int>
    {
        public string MaDotDoAnCode { get; private set; } = string.Empty;
        public string TenDotDoAn { get; private set; } = string.Empty;
        public ProjectKind LoaiDoAn { get; private set; }
        public int MaHocKy { get; private set; }
        public DateTime NgayBatDau { get; private set; }
        public DateTime NgayKetThuc { get; private set; }
        public DateTime HanDangKyDeTai { get; private set; }
        public DateTime HanNopDeCuong { get; private set; }
        public DateTime HanNopBaoCao { get; private set; }
        public DateTime NgayBatDauBaoVe { get; private set; }
        public DateTime NgayKetThucBaoVe { get; private set; }
        public PeriodStatus TrangThai { get; private set; } = PeriodStatus.CHUA_MO;

        public virtual ICollection<DotDoAnLop> DotDoAnLops { get; private set; } = new List<DotDoAnLop>();

        private DotDoAn() { }

        public DotDoAn(
            string maDotDoAnCode,
            string tenDotDoAn,
            ProjectKind loaiDoAn,
            int maHocKy,
            DateTime ngayBatDau,
            DateTime ngayKetThuc,
            DateTime hanDangKyDeTai,
            DateTime hanNopDeCuong,
            DateTime hanNopBaoCao,
            DateTime ngayBatDauBaoVe,
            DateTime ngayKetThucBaoVe)
        {
            MaDotDoAnCode = maDotDoAnCode.Trim().ToUpperInvariant();
            TenDotDoAn = tenDotDoAn.Trim();
            LoaiDoAn = loaiDoAn;
            MaHocKy = maHocKy;
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
            HanDangKyDeTai = hanDangKyDeTai;
            HanNopDeCuong = hanNopDeCuong;
            HanNopBaoCao = hanNopBaoCao;
            NgayBatDauBaoVe = ngayBatDauBaoVe;
            NgayKetThucBaoVe = ngayKetThucBaoVe;
            TrangThai = PeriodStatus.CHUA_MO;
        }

        public void Update(
            string tenDotDoAn,
            int maHocKy,
            DateTime ngayBatDau,
            DateTime ngayKetThuc,
            DateTime hanDangKyDeTai,
            DateTime hanNopDeCuong,
            DateTime hanNopBaoCao,
            DateTime ngayBatDauBaoVe,
            DateTime ngayKetThucBaoVe)
        {
            TenDotDoAn = tenDotDoAn.Trim();
            MaHocKy = maHocKy;
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
            HanDangKyDeTai = hanDangKyDeTai;
            HanNopDeCuong = hanNopDeCuong;
            HanNopBaoCao = hanNopBaoCao;
            NgayBatDauBaoVe = ngayBatDauBaoVe;
            NgayKetThucBaoVe = ngayKetThucBaoVe;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(PeriodStatus newStatus)
        {
            TrangThai = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class DotDoAnLop : Entity<int>
    {
        public int MaDotDoAn { get; private set; }
        public int MaLop { get; private set; }

        public virtual DotDoAn DotDoAn { get; private set; } = null!;

        private DotDoAnLop() { }

        public DotDoAnLop(int maDotDoAn, int maLop)
        {
            MaDotDoAn = maDotDoAn;
            MaLop = maLop;
        }
    }
}
