using UniProject.Share.Domain;

namespace UniProject.API.Modules.Academic
{
    public class NamHoc : AuditableEntity<int>
    {
        public string TenNamHoc { get; private set; } = string.Empty; // e.g. "2025-2026"
        public DateTime NgayBatDau { get; private set; }
        public DateTime NgayKetThuc { get; private set; }

        public virtual ICollection<HocKy> HocKys { get; private set; } = new List<HocKy>();

        private NamHoc() { }

        public NamHoc(string tenNamHoc, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            TenNamHoc = tenNamHoc.Trim();
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
        }

        public void Update(string tenNamHoc, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            TenNamHoc = tenNamHoc.Trim();
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class HocKy : AuditableEntity<int>
    {
        public string TenHocKy { get; private set; } = string.Empty; // e.g. "Học kỳ 1"
        public int HocKySo { get; private set; } // 1, 2, 3
        public int MaNamHoc { get; private set; }
        public DateTime NgayBatDau { get; private set; }
        public DateTime NgayKetThuc { get; private set; }

        public virtual NamHoc NamHoc { get; private set; } = null!;

        private HocKy() { }

        public HocKy(string tenHocKy, int hocKySo, int maNamHoc, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            TenHocKy = tenHocKy.Trim();
            HocKySo = hocKySo;
            MaNamHoc = maNamHoc;
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
        }

        public void Update(string tenHocKy, int hocKySo, int maNamHoc, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            TenHocKy = tenHocKy.Trim();
            HocKySo = hocKySo;
            MaNamHoc = maNamHoc;
            NgayBatDau = ngayBatDau;
            NgayKetThuc = ngayKetThuc;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class HocVi : Entity<int>
    {
        public string TenHocVi { get; private set; } = string.Empty; // "Cử nhân", "Kỹ sư", "Thạc sĩ", "Tiến sĩ"

        public virtual ICollection<GiangVien> GiangViens { get; private set; } = new List<GiangVien>();

        private HocVi() { }

        public HocVi(int id, string tenHocVi)
        {
            Id = id;
            TenHocVi = tenHocVi.Trim();
        }
    }

    public class HocHam : Entity<int>
    {
        public string TenHocHam { get; private set; } = string.Empty; // "Phó Giáo sư", "Giáo sư"

        public virtual ICollection<GiangVien> GiangViens { get; private set; } = new List<GiangVien>();

        private HocHam() { }

        public HocHam(int id, string tenHocHam)
        {
            Id = id;
            TenHocHam = tenHocHam.Trim();
        }
    }

    public class GiangVien : AuditableEntity<int>
    {
        public string MaGVCode { get; private set; } = string.Empty;
        public string HoTen { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string? SoDienThoai { get; private set; }
        public int MaBoMon { get; private set; }
        public int MaHocVi { get; private set; }
        public int? MaHocHam { get; private set; }
        public int MaTaiKhoan { get; private set; } // External reference to Identity module

        public virtual BoMon BoMon { get; private set; } = null!;
        public virtual HocVi HocVi { get; private set; } = null!;
        public virtual HocHam? HocHam { get; private set; }

        private GiangVien() { }

        public GiangVien(
            string maGVCode,
            string hoTen,
            string email,
            string? soDienThoai,
            int maBoMon,
            int maHocVi,
            int? maHocHam,
            int maTaiKhoan)
        {
            MaGVCode = maGVCode.Trim().ToUpperInvariant();
            HoTen = hoTen.Trim();
            Email = email.Trim().ToLowerInvariant();
            SoDienThoai = soDienThoai?.Trim();
            MaBoMon = maBoMon;
            MaHocVi = maHocVi;
            MaHocHam = maHocHam;
            MaTaiKhoan = maTaiKhoan;
        }

        public void Update(string hoTen, string email, string? soDienThoai, int maBoMon, int maHocVi, int? maHocHam)
        {
            HoTen = hoTen.Trim();
            Email = email.Trim().ToLowerInvariant();
            SoDienThoai = soDienThoai?.Trim();
            MaBoMon = maBoMon;
            MaHocVi = maHocVi;
            MaHocHam = maHocHam;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class SinhVien : AuditableEntity<int>
    {
        public string MaSVCode { get; private set; } = string.Empty;
        public string HoTen { get; private set; } = string.Empty;
        public DateTime? NgaySinh { get; private set; }
        public string? GioiTinh { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string? SoDienThoai { get; private set; }
        public int MaLop { get; private set; }
        public int MaTaiKhoan { get; private set; } // External reference to Identity module

        public virtual Lop Lop { get; private set; } = null!;

        private SinhVien() { }

        public SinhVien(
            string maSVCode,
            string hoTen,
            DateTime? ngaySinh,
            string? gioiTinh,
            string email,
            string? soDienThoai,
            int maLop,
            int maTaiKhoan)
        {
            MaSVCode = maSVCode.Trim().ToUpperInvariant();
            HoTen = hoTen.Trim();
            NgaySinh = ngaySinh;
            GioiTinh = gioiTinh;
            Email = email.Trim().ToLowerInvariant();
            SoDienThoai = soDienThoai?.Trim();
            MaLop = maLop;
            MaTaiKhoan = maTaiKhoan;
        }

        public void Update(string hoTen, DateTime? ngaySinh, string? gioiTinh, string email, string? soDienThoai, int maLop)
        {
            HoTen = hoTen.Trim();
            NgaySinh = ngaySinh;
            GioiTinh = gioiTinh;
            Email = email.Trim().ToLowerInvariant();
            SoDienThoai = soDienThoai?.Trim();
            MaLop = maLop;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
