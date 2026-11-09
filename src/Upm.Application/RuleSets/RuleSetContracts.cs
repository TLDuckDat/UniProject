using Upm.Application.Common;

namespace Upm.Application.RuleSets;

public sealed record RuleSetItem(long Id, string MaRuleSet, string TenRuleSet, string? CanCuPhapLy,
    int MaxTCChuaTichLuy, decimal GpaBaoVeMin, decimal GpaTotNghiepMin, decimal DiemDatDATN,
    decimal TrongSoQT, decimal HeSoThoiGianToiDa, int MinTVHoiDong, int MaxTVHoiDong,
    decimal NguongHaHangHocLaiPct, bool HaHangKhiKyLuat, DateOnly HieuLucTu, DateOnly? HieuLucDen, bool DaSuDung);
public sealed record RuleSetRequest(string MaRuleSet, string TenRuleSet, string? CanCuPhapLy,
    int MaxTCChuaTichLuy, decimal GpaBaoVeMin, decimal GpaTotNghiepMin, decimal DiemDatDATN,
    decimal TrongSoQT, decimal HeSoThoiGianToiDa, int MinTVHoiDong, int MaxTVHoiDong,
    decimal NguongHaHangHocLaiPct, bool HaHangKhiKyLuat, DateOnly HieuLucTu, DateOnly? HieuLucDen);
public sealed record RuleSetKhoaHocRequest(long RuleSetId, int KhoaHocTu, int? KhoaHocDen, bool CanXacNhan = false);
public sealed record ThamSoBoMonRequest(long BoMonId, long RuleSetId, decimal TrongSoQT, decimal TrongSoHD,
    bool TachDiemKhac, byte SoNgaySuaGapToiDa, bool DungHanKhiNghiTamThoi);
public sealed record ThamSoDaoVanRequest(long KhoaId, long? RuleSetId, string ChuongTrinh, bool Bat, decimal NguongPhanTram);
public sealed record ThamSoBoMonItem(long Id, long BoMonId, long RuleSetId, decimal TrongSoQT, decimal TrongSoHD,
    bool TachDiemKhac, byte SoNgaySuaGapToiDa, bool DungHanKhiNghiTamThoi);
public sealed record ThamSoDaoVanItem(long Id, long KhoaId, long? RuleSetId, string ChuongTrinh, bool Bat, decimal NguongPhanTram);

public interface IRuleSetService
{
    Task<PagedResult<RuleSetItem>> DanhSachAsync(string? q, PageRequest page, CancellationToken ct = default);
    Task<RuleSetItem> LayAsync(long id, CancellationToken ct = default);
    Task<RuleSetItem> TaoAsync(RuleSetRequest request, CancellationToken ct = default);
    Task<RuleSetItem> SuaAsync(long id, RuleSetRequest request, CancellationToken ct = default);
    Task<RuleSetItem> NhanBanAsync(long id, string maMoi, string tenMoi, CancellationToken ct = default);
    Task<IReadOnlyList<RuleSetKhoaHocRequest>> LayKhoaHocAsync(long id, CancellationToken ct = default);
    Task GanKhoaHocAsync(long id, IReadOnlyList<RuleSetKhoaHocRequest> items, CancellationToken ct = default);
    Task<IReadOnlyList<ThamSoBoMonItem>> LayThamSoBoMonAsync(long ruleSetId, CancellationToken ct = default);
    Task<ThamSoBoMonItem> LuuThamSoBoMonAsync(ThamSoBoMonRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ThamSoDaoVanItem>> LayThamSoDaoVanAsync(long khoaId, CancellationToken ct = default);
    Task<ThamSoDaoVanItem> LuuThamSoDaoVanAsync(ThamSoDaoVanRequest request, CancellationToken ct = default);
}
