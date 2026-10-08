using UniProject.Share.Domain;

namespace UniProject.API.Modules.Grading
{
    public enum GradingType
    {
        HUONG_DAN,
        PHAN_BIEN,
        HOI_DONG
    }

    public enum GradeRank
    {
        XUAT_SAC,
        GIOI,
        KHA,
        TRUNG_BINH,
        YEU
    }

    public enum FinalResult
    {
        CHO_KET_QUA,
        DAT,
        KHONG_DAT
    }

    public class PhieuCham : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaSV { get; private set; }
        public int MaGV { get; private set; }
        public int? MaHoiDong { get; private set; }
        public GradingType LoaiCham { get; private set; }
        public decimal Diem { get; private set; }
        public string? NhanXet { get; private set; }
        public DateTime NgayCham { get; private set; } = DateTime.UtcNow;

        private PhieuCham() { }

        public PhieuCham(
            int maDoAn,
            int maSV,
            int maGV,
            int? maHoiDong,
            GradingType loaiCham,
            decimal diem,
            string? nhanXet = null)
        {
            if (diem < 0 || diem > 10)
                throw new DomainException("Điểm số phải từ 0 đến 10.");

            if (loaiCham == GradingType.HOI_DONG && !maHoiDong.HasValue)
                throw new DomainException("Chấm điểm hội đồng bắt buộc phải có mã hội đồng.");

            if (loaiCham != GradingType.HOI_DONG && maHoiDong.HasValue)
                throw new DomainException("Chấm điểm hướng dẫn hoặc phản biện không được gán mã hội đồng.");

            MaDoAn = maDoAn;
            MaSV = maSV;
            MaGV = maGV;
            MaHoiDong = maHoiDong;
            LoaiCham = loaiCham;
            Diem = Math.Round(diem, 2);
            NhanXet = nhanXet?.Trim();
            NgayCham = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateScore(decimal newScore, string? nhanXet)
        {
            if (newScore < 0 || newScore > 10)
                throw new DomainException("Điểm số phải từ 0 đến 10.");

            Diem = Math.Round(newScore, 2);
            NhanXet = nhanXet?.Trim();
            NgayCham = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class KetQuaDoAn : AuditableEntity<int>
    {
        public int MaDoAn { get; private set; }
        public int MaSV { get; private set; }
        public decimal? DiemHuongDan { get; private set; }
        public decimal? DiemPhanBien { get; private set; }
        public decimal? DiemHoiDong { get; private set; }
        public decimal? DiemTongKet { get; private set; }
        public GradeRank XepLoai { get; private set; } = GradeRank.TRUNG_BINH;
        public FinalResult KetQua { get; private set; } = FinalResult.CHO_KET_QUA;
        public DateTime? NgayCongBo { get; private set; }

        private KetQuaDoAn() { }

        public KetQuaDoAn(int maDoAn, int maSV)
        {
            MaDoAn = maDoAn;
            MaSV = maSV;
            KetQua = FinalResult.CHO_KET_QUA;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateGrades(
            decimal? diemHuongDan,
            decimal? diemPhanBien,
            decimal? diemHoiDong,
            decimal? diemTongKet,
            GradeRank xepLoai,
            FinalResult ketQua)
        {
            DiemHuongDan = diemHuongDan.HasValue ? Math.Round(diemHuongDan.Value, 2) : null;
            DiemPhanBien = diemPhanBien.HasValue ? Math.Round(diemPhanBien.Value, 2) : null;
            DiemHoiDong = diemHoiDong.HasValue ? Math.Round(diemHoiDong.Value, 2) : null;
            DiemTongKet = diemTongKet.HasValue ? Math.Round(diemTongKet.Value, 2) : null;
            XepLoai = xepLoai;
            KetQua = ketQua;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Chưa đủ 3 loại điểm: chỉ lưu điểm thành phần, chưa có tổng kết / kết quả.</summary>
        public void MarkPending(decimal? diemHuongDan, decimal? diemPhanBien, decimal? diemHoiDong)
        {
            DiemHuongDan = diemHuongDan.HasValue ? Math.Round(diemHuongDan.Value, 2) : null;
            DiemPhanBien = diemPhanBien.HasValue ? Math.Round(diemPhanBien.Value, 2) : null;
            DiemHoiDong = diemHoiDong.HasValue ? Math.Round(diemHoiDong.Value, 2) : null;
            DiemTongKet = null;
            KetQua = FinalResult.CHO_KET_QUA;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Publish()
        {
            if (KetQua == FinalResult.CHO_KET_QUA)
                throw new DomainException("Chưa đủ điểm để công bố kết quả.");
            if (NgayCongBo.HasValue)
                throw new DomainException("Kết quả đã được công bố.");
            NgayCongBo = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
