using UniProject.Share.Domain;

namespace UniProject.API.Modules.Notification
{
    public enum NotificationPriority
    {
        BINH_THUONG,
        QUAN_TRONG,
        KHAN_CAP
    }

    public class ThongBao : AuditableEntity<int>
    {
        public string TieuDe { get; private set; } = string.Empty;
        public string NoiDung { get; private set; } = string.Empty;
        public string? LoaiThongBao { get; private set; }
        public NotificationPriority MucDo { get; private set; } = NotificationPriority.BINH_THUONG;
        public DateTime NgayTao { get; private set; } = DateTime.UtcNow;
        public int? NguoiTaoId { get; private set; }

        public virtual ICollection<NguoiNhanThongBao> NguoiNhans { get; private set; } = new List<NguoiNhanThongBao>();

        private ThongBao() { }

        public ThongBao(string tieuDe, string noiDung, string? loaiThongBao, NotificationPriority mucDo, int? nguoiTaoId)
        {
            TieuDe = tieuDe.Trim();
            NoiDung = noiDung.Trim();
            LoaiThongBao = loaiThongBao?.Trim();
            MucDo = mucDo;
            NguoiTaoId = nguoiTaoId;
            NgayTao = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddRecipient(int accountId)
        {
            if (NguoiNhans.Any(x => x.MaTaiKhoan == accountId)) return;
            NguoiNhans.Add(new NguoiNhanThongBao(Id, accountId));
        }
    }

    public class NguoiNhanThongBao : Entity<int>
    {
        public int MaThongBao { get; private set; }
        public int MaTaiKhoan { get; private set; }
        public bool DaDoc { get; private set; } = false;
        public DateTime? NgayDoc { get; private set; }

        public virtual ThongBao ThongBao { get; private set; } = null!;

        private NguoiNhanThongBao() { }

        public NguoiNhanThongBao(int maThongBao, int maTaiKhoan)
        {
            MaThongBao = maThongBao;
            MaTaiKhoan = maTaiKhoan;
            DaDoc = false;
            NgayDoc = null;
        }

        public void MarkAsRead()
        {
            DaDoc = true;
            NgayDoc = DateTime.UtcNow;
        }
    }
}
