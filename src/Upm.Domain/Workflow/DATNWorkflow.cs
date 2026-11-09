using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;

namespace Upm.Domain.Workflow;

public enum WorkflowActorType
{
    NguoiDung,
    HeThong,
}

/// <summary>Các dữ kiện được xác minh từ snapshot luật hoặc dữ liệu nghiệp vụ đã lưu.</summary>
public enum WorkflowFact
{
    GiaoKhongDuDieuKien,
    GiaoChuyenHocPhanThayThe,
    GiaoDuDieuKien,
    CoDeTai,
    CoQuyetDinhGiao,
    DenNgayBatDauDot,
    SinhVienNghiTamThoi,
    DonXinDungDaDuyet,
    DonGiaHanDaDuyet,
    CoBanQuyenNop,
    CoBanQuyenNopDungHan,
    CoBanQuyenSuaGap,
    BanSuaGapMoiNhat,
    ConVongSuaGap,
    KetQuaDocDuyetSuaGap,
    GVHDKhongDongY,
    GVDDKhongDongY,
    HetHanHoanBaoVe,
    VangBaoVeCoLyDo,
    BaoVeHoanTatDuPhieu,
    DiemDATNDat,
    DiemDATNKhongDat,
    CoBanCuoiDaNhan,
    GVHDXacNhanBanCuoi,
    ThuVienNhanBanCuoi,
}

public enum WorkflowConditionType
{
    Fact,
    RuleDat,
    RuleKhongDat,
    CoItNhatMotRuleKhongDat,
}

public sealed record WorkflowCondition(
    WorkflowConditionType Type,
    string Ma = "",
    IReadOnlyList<string>? CacMa = null)
{
    public static WorkflowCondition Fact(WorkflowFact fact) => new(WorkflowConditionType.Fact, fact.ToString());
    public static WorkflowCondition RuleDat(string maLuat) => new(WorkflowConditionType.RuleDat, maLuat);
    public static WorkflowCondition RuleKhongDat(string maLuat) => new(WorkflowConditionType.RuleKhongDat, maLuat);
    public static WorkflowCondition CoRuleKhongDat(params string[] maLuat) =>
        new(WorkflowConditionType.CoItNhatMotRuleKhongDat, CacMa: maLuat);
}

/// <summary>Một nhóm guard phải thỏa toàn bộ điều kiện; definition có nhiều nhóm thì chỉ cần một nhóm đạt.</summary>
public sealed record WorkflowGuardGroup(IReadOnlyList<WorkflowCondition> Conditions);

public sealed record WorkflowTransitionDefinition(
    string MaChuyen,
    TrangThaiSVTrongDot Tu,
    TrangThaiSVTrongDot Den,
    IReadOnlySet<string> VaiTroDuocPhep,
    bool HeThongDuocPhep,
    IReadOnlyList<WorkflowGuardGroup> GuardGroups,
    string MoTaDieuKien);

public sealed record WorkflowContext(
    WorkflowActorType ActorType,
    long? NguoiThucHienId,
    IReadOnlySet<string> VaiTro,
    string? LyDo,
    long? QuyetDinhId,
    DateTime ThoiDiem,
    IReadOnlySet<WorkflowFact> Facts,
    IReadOnlyList<IKetQuaLuat> KetQuaLuat);

public sealed record WorkflowTransitionResult(
    WorkflowTransitionDefinition Chuyen,
    LichSuTrangThai LichSu);

/// <summary>
/// State machine SVTrongDot (docs/thiet-ke.md mục 2, T01–T22; docs/dac-ta-nghiep-vu.html mục 9).
/// Mọi thay đổi TrangThai phải đi qua Transition.
/// </summary>
public static class DATNWorkflow
{
    private static readonly WorkflowCondition GiaoKhongDu = WorkflowCondition.Fact(WorkflowFact.GiaoKhongDuDieuKien);
    private static readonly WorkflowCondition GiaoThayThe = WorkflowCondition.Fact(WorkflowFact.GiaoChuyenHocPhanThayThe);
    private static readonly WorkflowCondition GiaoDu = WorkflowCondition.Fact(WorkflowFact.GiaoDuDieuKien);
    private static WorkflowCondition RuleDat(string ma) => WorkflowCondition.RuleDat(ma);
    private static WorkflowCondition RuleKhongDat(string ma) => WorkflowCondition.RuleKhongDat(ma);
    private static WorkflowCondition Fact(WorkflowFact fact) => WorkflowCondition.Fact(fact);
    private static WorkflowGuardGroup Group(params WorkflowCondition[] conditions) => new(conditions);
    private static readonly IReadOnlySet<string> PhongDaoTao = Set(VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri);
    private static readonly IReadOnlySet<string> SinhVien = Set(VaiTroMa.SinhVien);

    private static readonly IReadOnlyList<WorkflowTransitionDefinition> Definitions =
    [
        D("T01", TrangThaiSVTrongDot.CHO_XET, TrangThaiSVTrongDot.KHONG_DU_DK, PhongDaoTao, true,
            [Group(GiaoKhongDu)], "Kết quả xét giao không đủ điều kiện (G1–G3)."),
        D("T02", TrangThaiSVTrongDot.CHO_XET, TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE, PhongDaoTao, true,
            [Group(GiaoThayThe)], "Kết quả xét giao yêu cầu đăng ký học phần thay thế ĐATN."),
        D("T03", TrangThaiSVTrongDot.CHO_XET, TrangThaiSVTrongDot.DA_GIAO, PhongDaoTao, true,
            [Group(GiaoDu, Fact(WorkflowFact.CoDeTai), Fact(WorkflowFact.CoQuyetDinhGiao))], "Đủ điều kiện, có đề tài và quyết định giao."),
        // A19: xét lại (điểm thay đổi / khiếu nại được chấp nhận) TRƯỚC khi có QĐ giao cho kết quả đủ điều kiện →
        // hồ sơ quay về CHO_XET để được gắn đề tài + QĐ (T03). Bảng T01–T22 trong thiết kế không có chiều này (xem PROGRESS – Câu hỏi cho BA).
        D("T01R", TrangThaiSVTrongDot.KHONG_DU_DK, TrangThaiSVTrongDot.CHO_XET, PhongDaoTao, true,
            [Group(GiaoDu)], "A19: xét lại trước khi có quyết định giao cho kết quả đủ điều kiện."),
        D("T02R", TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE, TrangThaiSVTrongDot.CHO_XET, PhongDaoTao, true,
            [Group(GiaoDu)], "A19: xét lại trước khi có quyết định giao cho kết quả đủ điều kiện."),
        D("T04", TrangThaiSVTrongDot.DA_GIAO, TrangThaiSVTrongDot.DANG_THUC_HIEN, PhongDaoTao, true,
            [Group(Fact(WorkflowFact.DenNgayBatDauDot))], "Đến ngày bắt đầu đợt (hệ thống tự chuyển; đặc tả T04)."),
        D("T05", TrangThaiSVTrongDot.DANG_THUC_HIEN, TrangThaiSVTrongDot.TAM_DUNG, PhongDaoTao, false,
            [Group(Fact(WorkflowFact.SinhVienNghiTamThoi))], "Sinh viên được xác nhận nghỉ học tạm thời."),
        D("T06", TrangThaiSVTrongDot.DANG_THUC_HIEN, TrangThaiSVTrongDot.RUT,
            Set(VaiTroMa.SinhVien, VaiTroMa.GVHD, VaiTroMa.TruongBoMon, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.DonXinDungDaDuyet))], "Đơn xin dừng hoặc đề nghị dừng đã được duyệt; bắt buộc ghi lý do."),
        D("T07", TrangThaiSVTrongDot.DANG_THUC_HIEN, TrangThaiSVTrongDot.DA_NOP_QUYEN, SinhVien, false,
            [Group(Fact(WorkflowFact.CoBanQuyenNopDungHan)), Group(Fact(WorkflowFact.CoBanQuyenNop), Fact(WorkflowFact.DonGiaHanDaDuyet))], "Đã nộp quyển trong hạn hoặc có gia hạn được duyệt."),
        D("T08", TrangThaiSVTrongDot.DA_NOP_QUYEN, TrangThaiSVTrongDot.YEU_CAU_CHINH_SUA, Set(VaiTroMa.GVDD, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.KetQuaDocDuyetSuaGap), Fact(WorkflowFact.ConVongSuaGap))], "GVĐD yêu cầu sửa gấp, còn vòng sửa."),
        D("T09", TrangThaiSVTrongDot.YEU_CAU_CHINH_SUA, TrangThaiSVTrongDot.DA_NOP_QUYEN, SinhVien, false,
            [Group(Fact(WorkflowFact.BanSuaGapMoiNhat))], "Sinh viên đã nộp bản sửa gấp để GVĐD đọc lại."),
        D("T10", TrangThaiSVTrongDot.DA_NOP_QUYEN, TrangThaiSVTrongDot.DUOC_BAO_VE, PhongDaoTao, true,
            [Group(RuleDat(MaLuat.K1), RuleDat(MaLuat.K2), RuleDat(MaLuat.K3), RuleDat(MaLuat.K4), RuleDat(MaLuat.K5), RuleDat(MaLuat.K6))], "K1–K6 đều đạt theo snapshot xét bảo vệ."),
        D("T11", TrangThaiSVTrongDot.DA_NOP_QUYEN, TrangThaiSVTrongDot.HOAN_BAO_VE, PhongDaoTao, true,
            [Group(RuleDat(MaLuat.K1), RuleDat(MaLuat.K2), RuleDat(MaLuat.K3), WorkflowCondition.CoRuleKhongDat(MaLuat.K4, MaLuat.K5, MaLuat.K6))], "K1–K3 đạt và thiếu ít nhất một trong K4–K6."),
        D("T12", TrangThaiSVTrongDot.DA_NOP_QUYEN, TrangThaiSVTrongDot.PHAI_LAM_LAI,
            Set(VaiTroMa.GVHD, VaiTroMa.GVDD, VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri), true,
            [Group(RuleKhongDat(MaLuat.K1)), Group(Fact(WorkflowFact.GVHDKhongDongY)), Group(Fact(WorkflowFact.GVDDKhongDongY))], "K1 không đạt, GVHD không đồng ý hoặc GVĐD không đồng ý; cần lý do."),
        D("T13", TrangThaiSVTrongDot.HOAN_BAO_VE, TrangThaiSVTrongDot.DUOC_BAO_VE, PhongDaoTao, true,
            [Group(RuleDat(MaLuat.K4), RuleDat(MaLuat.K5), RuleDat(MaLuat.K6))], "K4–K6 đạt trong học kỳ liền kề."),
        D("T14", TrangThaiSVTrongDot.HOAN_BAO_VE, TrangThaiSVTrongDot.PHAI_LAM_LAI, PhongDaoTao, true,
            [Group(Fact(WorkflowFact.HetHanHoanBaoVe))], "Đã hết học kỳ liền kề."),
        D("T15", TrangThaiSVTrongDot.DUOC_BAO_VE, TrangThaiSVTrongDot.PHAI_LAM_LAI, PhongDaoTao, true,
            [Group(RuleKhongDat(MaLuat.K1))], "K1 không đạt tại ngày bảo vệ."),
        D("T16", TrangThaiSVTrongDot.DUOC_BAO_VE, TrangThaiSVTrongDot.HOAN_BAO_VE, PhongDaoTao, false,
            [Group(Fact(WorkflowFact.VangBaoVeCoLyDo))], "Vắng bảo vệ có lý do chính đáng."),
        D("T17", TrangThaiSVTrongDot.DUOC_BAO_VE, TrangThaiSVTrongDot.DA_BAO_VE,
            Set(VaiTroMa.ThanhVienHoiDong, VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.BaoVeHoanTatDuPhieu))], "Bảo vệ hoàn tất và đủ phiếu điểm của thành viên có mặt."),
        D("T18", TrangThaiSVTrongDot.DA_BAO_VE, TrangThaiSVTrongDot.DAT,
            Set(VaiTroMa.ThanhVienHoiDong, VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.DiemDATNDat))], "Điểm ĐATN đã chốt và đạt ngưỡng."),
        D("T19", TrangThaiSVTrongDot.DA_BAO_VE, TrangThaiSVTrongDot.KHONG_DAT,
            Set(VaiTroMa.ThanhVienHoiDong, VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.DiemDATNKhongDat))], "Điểm ĐATN đã chốt và không đạt."),
        D("T20", TrangThaiSVTrongDot.KHONG_DAT, TrangThaiSVTrongDot.PHAI_LAM_LAI, Set(), true, [], "Tự động chuyển sau khi chốt không đạt."),
        D("T21", TrangThaiSVTrongDot.DAT, TrangThaiSVTrongDot.CHO_BAN_CUOI, SinhVien, false, [], "Chờ sinh viên nộp bản cuối."),
        D("T22", TrangThaiSVTrongDot.CHO_BAN_CUOI, TrangThaiSVTrongDot.HOAN_TAT,
            Set(VaiTroMa.GVHD, VaiTroMa.ThuVien, VaiTroMa.QuanTri), false,
            [Group(Fact(WorkflowFact.CoBanCuoiDaNhan), Fact(WorkflowFact.GVHDXacNhanBanCuoi), Fact(WorkflowFact.ThuVienNhanBanCuoi))], "GVHD xác nhận và Thư viện nhận bản cuối."),
    ];

    public static IReadOnlyList<WorkflowTransitionDefinition> DinhNghia => Definitions;

    public static WorkflowTransitionResult Transition(SVTrongDot svTrongDot, string maChuyen, WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(svTrongDot);
        ArgumentException.ThrowIfNullOrWhiteSpace(maChuyen);
        ArgumentNullException.ThrowIfNull(context);

        var definition = Definitions.SingleOrDefault(x => string.Equals(x.MaChuyen, maChuyen, StringComparison.OrdinalIgnoreCase));
        return Transition(svTrongDot, definition, context);
    }

    public static WorkflowTransitionResult Transition(SVTrongDot svTrongDot, TrangThaiSVTrongDot trangThaiDich, WorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(svTrongDot);
        ArgumentNullException.ThrowIfNull(context);

        var matches = Definitions.Where(x => x.Tu == svTrongDot.TrangThai && x.Den == trangThaiDich).ToArray();
        if (matches.Length != 1)
        {
            throw new DomainException("UPM_WORKFLOW_TRANSITION_NOT_FOUND", "Không có chuyển trạng thái phù hợp trong bảng T01–T22.");
        }

        return Transition(svTrongDot, matches[0], context);
    }

    public static IReadOnlyList<WorkflowTransitionDefinition> ChuyenHopLe(SVTrongDot svTrongDot, WorkflowContext context) =>
        Definitions.Where(x => x.Tu == svTrongDot.TrangThai && CoQuyen(x, context) && GuardDat(x, context)).ToArray();

    public static SVTrongDot TaoHoSoLamLai(SVTrongDot hoSoTruoc, long dotMoiId)
    {
        ArgumentNullException.ThrowIfNull(hoSoTruoc);
        if (hoSoTruoc.TrangThai is not (TrangThaiSVTrongDot.PHAI_LAM_LAI or TrangThaiSVTrongDot.KHONG_DU_DK))
        {
            throw new DomainException("UPM_WORKFLOW_KHONG_PHAI_HO_SO_LAM_LAI", "Chỉ hồ sơ PHAI_LAM_LAI hoặc KHONG_DU_DK được tạo hồ sơ làm lại.");
        }

        if (dotMoiId <= 0)
        {
            throw new DomainException("UPM_WORKFLOW_DOT_MOI_KHONG_HOP_LE", "Đợt mới không hợp lệ.");
        }

        return new SVTrongDot
        {
            DotId = dotMoiId,
            SinhVienId = hoSoTruoc.SinhVienId,
            CTDTId = hoSoTruoc.CTDTId,
            CoSoId = hoSoTruoc.CoSoId,
            TrangThai = TrangThaiSVTrongDot.CHO_XET,
            LanLamLai = checked(hoSoTruoc.LanLamLai + 1),
            HoSoTruocId = hoSoTruoc.Id,
        };
    }

    private static WorkflowTransitionResult Transition(SVTrongDot svTrongDot, WorkflowTransitionDefinition? definition, WorkflowContext context)
    {
        if (definition is null || definition.Tu != svTrongDot.TrangThai)
        {
            throw new DomainException("UPM_WORKFLOW_TRANSITION_NOT_FOUND", "Không có chuyển trạng thái từ trạng thái hiện tại trong bảng T01–T22.");
        }

        if (!CoQuyen(definition, context))
        {
            throw new DomainException("UPM_WORKFLOW_PERMISSION_DENIED", $"Người thực hiện không có quyền chạy {definition.MaChuyen}.");
        }

        if (!GuardDat(definition, context))
        {
            throw new DomainException("UPM_WORKFLOW_GUARD_FAILED", $"Hồ sơ chưa thỏa điều kiện của {definition.MaChuyen}: {definition.MoTaDieuKien}");
        }

        if (definition.Den is (TrangThaiSVTrongDot.RUT or TrangThaiSVTrongDot.PHAI_LAM_LAI) && string.IsNullOrWhiteSpace(context.LyDo))
        {
            throw new DomainException("UPM_WORKFLOW_REASON_REQUIRED", "Chuyển sang RUT hoặc PHAI_LAM_LAI phải ghi lý do.");
        }
        if (context.LyDo?.Length > 500)
        {
            throw new DomainException("UPM_WORKFLOW_REASON_TOO_LONG", "Lý do chuyển trạng thái không được vượt quá 500 ký tự.");
        }

        var tu = svTrongDot.TrangThai;
        svTrongDot.TrangThai = definition.Den;
        var history = new LichSuTrangThai
        {
            SVTrongDot = svTrongDot,
            SVTrongDotId = svTrongDot.Id,
            TuTrangThai = tu,
            DenTrangThai = definition.Den,
            LyDo = context.LyDo,
            NguoiThucHienId = context.NguoiThucHienId,
            QuyetDinhId = context.QuyetDinhId,
            ThoiDiem = context.ThoiDiem,
        };
        return new WorkflowTransitionResult(definition, history);
    }

    private static bool CoQuyen(WorkflowTransitionDefinition definition, WorkflowContext context) =>
        context.ActorType == WorkflowActorType.HeThong
            ? definition.HeThongDuocPhep
            : context.VaiTro.Overlaps(definition.VaiTroDuocPhep);

    private static bool GuardDat(WorkflowTransitionDefinition definition, WorkflowContext context) =>
        definition.GuardGroups.Count == 0 || definition.GuardGroups.Any(group => group.Conditions.All(condition => ConditionDat(condition, context)));

    private static bool ConditionDat(WorkflowCondition condition, WorkflowContext context) => condition.Type switch
    {
        WorkflowConditionType.Fact => Enum.TryParse<WorkflowFact>(condition.Ma, out var fact) && context.Facts.Contains(fact),
        WorkflowConditionType.RuleDat => context.KetQuaLuat.Any(rule => rule.MaLuat == condition.Ma && rule.Dat),
        WorkflowConditionType.RuleKhongDat => context.KetQuaLuat.Any(rule => rule.MaLuat == condition.Ma && !rule.Dat),
        WorkflowConditionType.CoItNhatMotRuleKhongDat => condition.CacMa is not null
            && condition.CacMa.Any(code => context.KetQuaLuat.Any(rule => rule.MaLuat == code && !rule.Dat)),
        _ => false,
    };

    private static WorkflowTransitionDefinition D(
        string code,
        TrangThaiSVTrongDot from,
        TrangThaiSVTrongDot to,
        IReadOnlySet<string> roles,
        bool system,
        IReadOnlyList<WorkflowGuardGroup> guards,
        string description) => new(code, from, to, roles, system, guards, description);

    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);
}
