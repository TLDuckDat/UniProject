using Upm.Application.Abstractions;
using Upm.Application.PhanQuyen;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Workflow;

namespace Upm.Application.Workflow;

/// <summary>Use case duy nhất để đọc và thay đổi trạng thái SVTrongDot.</summary>
public sealed class WorkflowService(
    IWorkflowRepository repository,
    ICurrentUser currentUser,
    IQuyenHienTai quyenHienTai,
    IQuyenTrenSV quyenTrenSV,
    IAuditContext auditContext,
    IClock clock) : IWorkflowService
{
    public IReadOnlyList<WorkflowTransitionDefinition> LayDinhNghia() => DATNWorkflow.DinhNghia;

    public async Task<IReadOnlyList<WorkflowHistoryItem>> LayLichSuAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var rows = await repository.LayLichSuAsync(svTrongDotId, ct);
        return rows.Select(x => new WorkflowHistoryItem(
            x.Id, x.TuTrangThai, x.DenTrangThai, x.LyDo, x.NguoiThucHienId, x.QuyetDinhId, x.ThoiDiem)).ToArray();
    }

    public async Task<WorkflowAvailableTransitions> LayChuyenHopLeAsync(long svTrongDotId, CancellationToken ct = default)
    {
        var hoSo = await LayHoSoBatBuocAsync(svTrongDotId, ct);
        var evidence = await repository.LayBangChungAsync(hoSo, ct);
        var context = await TaoNguCanhNguoiDungAsync(hoSo, evidence, ct);
        var transitions = DATNWorkflow.ChuyenHopLe(hoSo, context)
            .Select(x => new WorkflowTransitionView(x.MaChuyen, x.Tu, x.Den, x.MoTaDieuKien))
            .ToArray();
        return new WorkflowAvailableTransitions(ToView(hoSo), transitions);
    }

    public Task<WorkflowRecordView> TransitionAsync(long svTrongDotId, WorkflowTransitionRequest request, CancellationToken ct = default) =>
        TransitionCoreAsync(svTrongDotId, request, heThong: false, ct);

    /// <summary>Chỉ gọi từ job/service nội bộ; không được ánh xạ ra HTTP endpoint.</summary>
    public Task<WorkflowRecordView> TransitionHeThongAsync(long svTrongDotId, WorkflowTransitionRequest request, CancellationToken ct = default) =>
        TransitionCoreAsync(svTrongDotId, request, heThong: true, ct);

    public Task<WorkflowRecordView> TaoHoSoLamLaiAsync(long hoSoTruocId, long dotMoiId, CancellationToken ct = default) =>
        repository.InTransactionAsync(async token =>
        {
            var roles = await quyenHienTai.LayAsync(token);
            if (!roles.CoVaiTro(VaiTroMa.PhongDaoTao, VaiTroMa.QuanTri))
            {
                throw new DomainException("UPM_WORKFLOW_PERMISSION_DENIED", "Chỉ Phòng Đào tạo được tạo hồ sơ làm lại.");
            }

            var previous = await LayHoSoBatBuocAsync(hoSoTruocId, token);
            var dot = await repository.LayDotAsync(dotMoiId, token)
                ?? throw new DomainException("UPM_WORKFLOW_DOT_MOI_NOT_FOUND", "Không tìm thấy đợt mới.");
            var dotTruoc = await repository.LayDotAsync(previous.DotId, token)
                ?? throw new DomainException("UPM_WORKFLOW_DOT_TRUOC_NOT_FOUND", "Không tìm thấy đợt của hồ sơ trước.");
            if (dot.NgayBatDau <= dotTruoc.NgayBatDau)
            {
                throw new DomainException("UPM_WORKFLOW_DOT_MOI_KHONG_PHAI_DOT_SAU", "Hồ sơ làm lại phải thuộc đợt bắt đầu sau đợt trước.");
            }
            if (dot.CoSoId != previous.CoSoId)
            {
                throw new DomainException("UPM_WORKFLOW_CO_SO_KHONG_KHOP", "Đợt mới phải cùng cơ sở với hồ sơ trước.");
            }
            if (!roles.CoSoDuocPhep.Contains(dot.CoSoId))
            {
                throw new DomainException("UPM_WORKFLOW_PERMISSION_DENIED", "Bạn không có quyền tạo hồ sơ tại cơ sở của đợt mới.");
            }
            if (await repository.DaCoHoSoAsync(dotMoiId, previous.SinhVienId, previous.CTDTId, token))
            {
                throw new DomainException("UPM_WORKFLOW_HO_SO_DA_TON_TAI", "Sinh viên đã có hồ sơ trong đợt mới.");
            }

            var next = DATNWorkflow.TaoHoSoLamLai(previous, dotMoiId);
            var now = clock.Now.UtcDateTime;
            var history = new LichSuTrangThai
            {
                SVTrongDot = next,
                TuTrangThai = null,
                DenTrangThai = TrangThaiSVTrongDot.CHO_XET,
                LyDo = $"Tạo hồ sơ làm lại từ hồ sơ {previous.Id}.",
                NguoiThucHienId = currentUser.TaiKhoanId,
                ThoiDiem = now,
            };
            await repository.LuuHoSoMoiAsync(next, history, token);
            return ToView(next);
        }, ct);

    private Task<WorkflowRecordView> TransitionCoreAsync(long id, WorkflowTransitionRequest request, bool heThong, CancellationToken ct) =>
        repository.InTransactionAsync(async token =>
        {
            ArgumentNullException.ThrowIfNull(request);
            KiemTraRequest(request);
            var expected = ParseRowVersion(request.RowVersion);
            var hoSo = await LayHoSoBatBuocAsync(id, token, heThong);
            if (!hoSo.RowVersion.AsSpan().SequenceEqual(expected))
            {
                throw LoiXungDot();
            }

            var evidence = await repository.LayBangChungAsync(hoSo, token);
            var context = heThong
                ? new WorkflowContext(WorkflowActorType.HeThong, null, Set(), request.LyDo, request.QuyetDinhId,
                    clock.Now.UtcDateTime, evidence.Facts, evidence.KetQuaLuat)
                : await TaoNguCanhNguoiDungAsync(hoSo, evidence, token, request);

            var result = request.MaChuyen is not null
                ? DATNWorkflow.Transition(hoSo, request.MaChuyen, context)
                : DATNWorkflow.Transition(hoSo, request.TrangThaiDich!.Value, context);
            var oldDecision = auditContext.SoQDBatBuoc;
            try
            {
                if (request.QuyetDinhId is { } decisionId)
                {
                    auditContext.SoQDBatBuoc = await repository.LaySoQuyetDinhAsync(decisionId, token)
                        ?? throw new DomainException("UPM_WORKFLOW_QUYET_DINH_NOT_FOUND", "Không tìm thấy quyết định được gắn với chuyển trạng thái.");
                }
                await repository.LuuChuyenTrangThaiAsync(hoSo, result.LichSu, expected, token);
            }
            finally
            {
                auditContext.SoQDBatBuoc = oldDecision;
            }
            return ToView(hoSo);
        }, ct);

    private async Task<WorkflowContext> TaoNguCanhNguoiDungAsync(
        SVTrongDot hoSo,
        WorkflowEvidence evidence,
        CancellationToken ct,
        WorkflowTransitionRequest? request = null)
    {
        if (!currentUser.DaDangNhap || currentUser.TaiKhoanId is not { } accountId)
        {
            return new WorkflowContext(WorkflowActorType.NguoiDung, null, Set(), request?.LyDo, request?.QuyetDinhId,
                clock.Now.UtcDateTime, evidence.Facts, evidence.KetQuaLuat);
        }

        var permissions = await quyenHienTai.LayAsync(ct);
        var roles = permissions.VaiTro.Select(x => x.MaVaiTro)
            .Where(x => x is not VaiTroMa.SinhVien and not VaiTroMa.GiangVien)
            .ToHashSet(StringComparer.Ordinal);

        if (await quyenTrenSV.LaChinhSVAsync(hoSo.Id, ct) && currentUser.SinhVienId == hoSo.SinhVienId)
        {
            roles.Add(VaiTroMa.SinhVien);
        }
        if (await quyenTrenSV.LaGVHDAsync(hoSo.Id, ct))
        {
            roles.Add(VaiTroMa.GVHD);
        }
        if (await quyenTrenSV.LaGVDDAsync(hoSo.Id, ct))
        {
            roles.Add(VaiTroMa.GVDD);
        }
        if (await quyenTrenSV.LaThanhVienHoiDongAsync(hoSo.Id, ct))
        {
            roles.Add(VaiTroMa.ThanhVienHoiDong);
        }

        return new WorkflowContext(WorkflowActorType.NguoiDung, accountId, roles, request?.LyDo,
            request?.QuyetDinhId, clock.Now.UtcDateTime, evidence.Facts, evidence.KetQuaLuat);
    }

    private async Task<SVTrongDot> LayHoSoBatBuocAsync(long id, CancellationToken ct, bool heThong = false) =>
        await repository.LayHoSoAsync(id, heThong, ct)
        ?? throw new DomainException("UPM_WORKFLOW_HO_SO_NOT_FOUND", "Không tìm thấy hồ sơ hoặc bạn không có quyền truy cập.");

    private static void KiemTraRequest(WorkflowTransitionRequest request)
    {
        if ((request.MaChuyen is null) == (request.TrangThaiDich is null))
        {
            throw new DomainException("UPM_WORKFLOW_CHUYEN_KHONG_HOP_LE", "Cần cung cấp đúng một trong maChuyen hoặc trangThaiDich.");
        }
        if (request.QuyetDinhId is <= 0)
        {
            throw new DomainException("UPM_WORKFLOW_QUYET_DINH_KHONG_HOP_LE", "Số định danh quyết định không hợp lệ.");
        }
    }

    private static byte[] ParseRowVersion(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 8)
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
            // Ném lỗi nghiệp vụ có mã thống nhất bên dưới.
        }

        throw new DomainException("UPM_WORKFLOW_ROWVERSION_INVALID", "RowVersion không hợp lệ.");
    }

    private static DomainException LoiXungDot() =>
        new("UPM_WORKFLOW_CONCURRENCY_CONFLICT", "Hồ sơ đã được cập nhật bởi thao tác khác. Hãy tải lại trạng thái mới nhất.");

    private static WorkflowRecordView ToView(SVTrongDot hoSo) =>
        new(hoSo.Id, hoSo.TrangThai, Convert.ToBase64String(hoSo.RowVersion));

    private static HashSet<string> Set() => new(StringComparer.Ordinal);
}
