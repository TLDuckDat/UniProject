using UniProject.Share.Domain;

namespace UniProject.API.Modules.Identity
{
    public class VaiTro : Entity<int>
    {
        public string TenVaiTro { get; private set; } = string.Empty;
        public string? MoTa { get; private set; }

        public virtual ICollection<TaiKhoan> TaiKhoans { get; private set; } = new List<TaiKhoan>();

        private VaiTro() { }

        public VaiTro(int id, string tenVaiTro, string? moTa = null)
        {
            Id = id;
            TenVaiTro = tenVaiTro;
            MoTa = moTa;
        }
    }
}
