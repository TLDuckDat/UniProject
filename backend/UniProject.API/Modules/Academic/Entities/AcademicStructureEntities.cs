using UniProject.Share.Domain;

namespace UniProject.API.Modules.Academic
{
    public class Khoa : AuditableEntity<int>
    {
        public string MaKhoaCode { get; private set; } = string.Empty;
        public string TenKhoa { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? SoDienThoai { get; private set; }

        public virtual ICollection<BoMon> BoMons { get; private set; } = new List<BoMon>();

        private Khoa() { }

        public Khoa(string maKhoaCode, string tenKhoa, string? email = null, string? soDienThoai = null)
        {
            MaKhoaCode = maKhoaCode.Trim().ToUpperInvariant();
            TenKhoa = tenKhoa.Trim();
            Email = email?.Trim();
            SoDienThoai = soDienThoai?.Trim();
        }

        public void Update(string tenKhoa, string? email, string? soDienThoai)
        {
            TenKhoa = tenKhoa.Trim();
            Email = email?.Trim();
            SoDienThoai = soDienThoai?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class BoMon : AuditableEntity<int>
    {
        public string MaBoMonCode { get; private set; } = string.Empty;
        public string TenBoMon { get; private set; } = string.Empty;
        public int MaKhoa { get; private set; }

        public virtual Khoa Khoa { get; private set; } = null!;
        public virtual ICollection<ChuyenNganh> ChuyenNganhs { get; private set; } = new List<ChuyenNganh>();
        public virtual ICollection<GiangVien> GiangViens { get; private set; } = new List<GiangVien>();

        private BoMon() { }

        public BoMon(string maBoMonCode, string tenBoMon, int maKhoa)
        {
            MaBoMonCode = maBoMonCode.Trim().ToUpperInvariant();
            TenBoMon = tenBoMon.Trim();
            MaKhoa = maKhoa;
        }

        public void Update(string tenBoMon, int maKhoa)
        {
            TenBoMon = tenBoMon.Trim();
            MaKhoa = maKhoa;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class ChuyenNganh : AuditableEntity<int>
    {
        public string MaChuyenNganhCode { get; private set; } = string.Empty;
        public string TenChuyenNganh { get; private set; } = string.Empty;
        public int MaBoMon { get; private set; }

        public virtual BoMon BoMon { get; private set; } = null!;
        public virtual ICollection<Lop> Lops { get; private set; } = new List<Lop>();

        private ChuyenNganh() { }

        public ChuyenNganh(string maChuyenNganhCode, string tenChuyenNganh, int maBoMon)
        {
            MaChuyenNganhCode = maChuyenNganhCode.Trim().ToUpperInvariant();
            TenChuyenNganh = tenChuyenNganh.Trim();
            MaBoMon = maBoMon;
        }

        public void Update(string tenChuyenNganh, int maBoMon)
        {
            TenChuyenNganh = tenChuyenNganh.Trim();
            MaBoMon = maBoMon;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class Lop : AuditableEntity<int>
    {
        public string MaLopCode { get; private set; } = string.Empty;
        public string TenLop { get; private set; } = string.Empty;
        public int NamNhapHoc { get; private set; }
        public int MaChuyenNganh { get; private set; }

        public virtual ChuyenNganh ChuyenNganh { get; private set; } = null!;
        public virtual ICollection<SinhVien> SinhViens { get; private set; } = new List<SinhVien>();

        private Lop() { }

        public Lop(string maLopCode, string tenLop, int namNhapHoc, int maChuyenNganh)
        {
            MaLopCode = maLopCode.Trim().ToUpperInvariant();
            TenLop = tenLop.Trim();
            NamNhapHoc = namNhapHoc;
            MaChuyenNganh = maChuyenNganh;
        }

        public void Update(string tenLop, int namNhapHoc, int maChuyenNganh)
        {
            TenLop = tenLop.Trim();
            NamNhapHoc = namNhapHoc;
            MaChuyenNganh = maChuyenNganh;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
