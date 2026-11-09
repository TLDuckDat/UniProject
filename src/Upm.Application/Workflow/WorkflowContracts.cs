using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;

namespace Upm.Application.Workflow;

public sealed record WorkflowTransitionRequest(
    string RowVersion,
    string? MaChuyen = null,
    TrangThaiSVTrongDot? TrangThaiDich = null,
    string? LyDo = null,
    long? QuyetDinhId = null);

public sealed record WorkflowTransitionView(string MaChuyen, TrangThaiSVTrongDot Tu, TrangThaiSVTrongDot Den, string MoTaDieuKien);

public sealed record WorkflowHistoryItem(
    long Id,
    TrangThaiSVTrongDot? TuTrangThai,
    TrangThaiSVTrongDot DenTrangThai,
    string? LyDo,
    long? NguoiThucHienId,
    long? QuyetDinhId,
    DateTime ThoiDiem);

public sealed record WorkflowRecordView(long Id, TrangThaiSVTrongDot TrangThai, string RowVersion);

public sealed record WorkflowAvailableTransitions(WorkflowRecordView HoSo, IReadOnlyList<WorkflowTransitionView> ChuyenHopLe);

public sealed record WorkflowEvidence(IReadOnlySet<WorkflowFact> Facts, IReadOnlyList<IKetQuaLuat> KetQuaLuat);

public interface IWorkflowService
{
    IReadOnlyList<WorkflowTransitionDefinition> LayDinhNghia();
    Task<IReadOnlyList<WorkflowHistoryItem>> LayLichSuAsync(long svTrongDotId, CancellationToken ct = default);
    Task<WorkflowAvailableTransitions> LayChuyenHopLeAsync(long svTrongDotId, CancellationToken ct = default);
    Task<WorkflowRecordView> TransitionAsync(long svTrongDotId, WorkflowTransitionRequest request, CancellationToken ct = default);
    Task<WorkflowRecordView> TransitionHeThongAsync(long svTrongDotId, WorkflowTransitionRequest request, CancellationToken ct = default);
    Task<WorkflowRecordView> TaoHoSoLamLaiAsync(long hoSoTruocId, long dotMoiId, CancellationToken ct = default);
}

/// <summary>Persistence port; implementation giữ transaction EF và lấy guard từ dữ liệu đã lưu.</summary>
public interface IWorkflowRepository
{
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
    Task<SVTrongDot?> LayHoSoAsync(long id, bool heThong, CancellationToken ct);
    Task<WorkflowEvidence> LayBangChungAsync(SVTrongDot hoSo, CancellationToken ct);
    Task<IReadOnlyList<LichSuTrangThai>> LayLichSuAsync(long id, CancellationToken ct);
    Task LuuChuyenTrangThaiAsync(SVTrongDot hoSo, LichSuTrangThai lichSu, byte[] rowVersionDuKien, CancellationToken ct);
    Task<DotDATN?> LayDotAsync(long id, CancellationToken ct);
    Task<string?> LaySoQuyetDinhAsync(long id, CancellationToken ct);
    Task<bool> DaCoHoSoAsync(long dotId, long sinhVienId, long ctdtId, CancellationToken ct);
    Task LuuHoSoMoiAsync(SVTrongDot hoSo, LichSuTrangThai lichSu, CancellationToken ct);
}
