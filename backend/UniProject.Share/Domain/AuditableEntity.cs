namespace UniProject.Share.Domain
{
    /// <summary>
    /// Thực thể cơ sở chứa siêu dữ liệu về kiểm tra, vòng đời và kiểm soát đồng thời.
    /// </summary>
    public abstract class AuditableEntity<TId> : Entity<TId>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public byte[]? RowVersion { get; set; }
    }
}
