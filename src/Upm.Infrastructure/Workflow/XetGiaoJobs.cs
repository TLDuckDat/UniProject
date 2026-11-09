using System.Globalization;
using System.Text.Json;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Upm.Application.Workflow;
using Upm.Domain.Common;

namespace Upm.Infrastructure.Workflow;

/// <summary>
/// Job Hangfire "Xét điều kiện giao" của một đợt – chỉ chạy theo lệnh của Phòng ĐT (không định kỳ).
/// Không tự retry: lỗi dừng job thì Phòng ĐT bấm chạy lại (runner idempotent).
/// Tiến độ ghi vào job parameter để API đọc: <c>Progress</c> = "đã xử lý/tổng/số lỗi", <c>Errors</c> = JSON lỗi từng SV.
/// </summary>
public sealed class XetGiaoDotHangfireJob(IXetGiaoBatchRunner runner)
{
    internal const string ThamSoDot = "DotId";
    internal const string ThamSoTienDo = "Progress";
    internal const string ThamSoLoi = "Errors";

    [Queue("default")]
    [AutomaticRetry(Attempts = 0)]
    [JobDisplayName("Xét điều kiện giao ĐATN – đợt {0}")]
    public async Task ChayAsync(long dotId, long? nguoiChayId, PerformContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetJobParameter(ThamSoDot, dotId.ToString(CultureInfo.InvariantCulture));
        var soLoiDaGhi = -1;
        var progress = new DongBoProgress<XetGiaoTienDo>(item =>
        {
            context.SetJobParameter(ThamSoTienDo, $"{item.DaXuLy}/{item.TongSo}/{item.Loi.Count}");
            if (item.Loi.Count != soLoiDaGhi)
            {
                soLoiDaGhi = item.Loi.Count;
                context.SetJobParameter(ThamSoLoi, JsonSerializer.Serialize(item.Loi));
            }
        });
        await runner.ChayAsync(dotId, nguoiChayId, progress, ct);
    }

    /// <summary>IProgress gọi đồng bộ (Progress&lt;T&gt; mặc định đẩy qua SynchronizationContext/ThreadPool – sai thứ tự).</summary>
    private sealed class DongBoProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}

/// <summary>Đưa job xét giao vào hàng đợi, nhớ job gần nhất của từng đợt (hash Hangfire) và đọc tiến độ.</summary>
public sealed class XetGiaoJobScheduler(IBackgroundJobClient jobs, JobStorage storage) : IXetGiaoJobScheduler
{
    private static readonly HashSet<string> TrangThaiDangChay =
        new([EnqueuedState.StateName, ProcessingState.StateName, ScheduledState.StateName, AwaitingState.StateName],
            StringComparer.OrdinalIgnoreCase);

    public XetGiaoJobItem DuaVaoHangDoi(long dotId, long? nguoiChayId)
    {
        if (LayGanNhat(dotId) is { } dangCo && TrangThaiDangChay.Contains(dangCo.TrangThai))
            throw new DomainException("UPM_XET_GIAO_DANG_CHAY", "Đợt đang có lượt xét giao chưa chạy xong. Hãy chờ lượt đó kết thúc.");

        var jobId = jobs.Create<XetGiaoDotHangfireJob>(
            job => job.ChayAsync(dotId, nguoiChayId, null!, CancellationToken.None), new EnqueuedState());
        using (var connection = storage.GetConnection())
        using (var tx = connection.CreateWriteTransaction())
        {
            tx.SetRangeInHash(KhoaHash(dotId), [new KeyValuePair<string, string>("JobId", jobId)]);
            tx.Commit();
        }
        return Lay(dotId, jobId) ?? new XetGiaoJobItem(jobId, dotId, EnqueuedState.StateName, 0, 0, 0, []);
    }

    public XetGiaoJobItem? LayGanNhat(long dotId)
    {
        using var connection = storage.GetConnection();
        var hash = connection.GetAllEntriesFromHash(KhoaHash(dotId));
        return hash is not null && hash.TryGetValue("JobId", out var jobId) ? Lay(dotId, jobId) : null;
    }

    public XetGiaoJobItem? Lay(long dotId, string jobId)
    {
        using var connection = storage.GetConnection();
        // Job của đợt khác (hoặc không phải job xét giao) → coi như không tồn tại: không lộ tiến độ ngoài phạm vi.
        var dotCuaJob = DocThamSo(connection, jobId, XetGiaoDotHangfireJob.ThamSoDot);
        if (!string.Equals(dotCuaJob, dotId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)) return null;

        var trangThai = connection.GetStateData(jobId)?.Name ?? EnqueuedState.StateName;
        var tienDo = (DocThamSo(connection, jobId, XetGiaoDotHangfireJob.ThamSoTienDo) ?? "0/0/0").Split('/');
        var loiJson = DocThamSo(connection, jobId, XetGiaoDotHangfireJob.ThamSoLoi);
        var loi = string.IsNullOrEmpty(loiJson)
            ? []
            : JsonSerializer.Deserialize<List<XetGiaoLoiItem>>(loiJson) ?? [];
        return new XetGiaoJobItem(jobId, dotId, trangThai, So(tienDo, 0), So(tienDo, 1), So(tienDo, 2), loi);
    }

    /// <summary>SetJobParameter lưu giá trị dạng JSON (chuỗi có dấu nháy) – connection trả chuỗi thô nên phải giải mã.</summary>
    private static string? DocThamSo(IStorageConnection connection, string jobId, string name)
    {
        var raw = connection.GetJobParameter(jobId, name);
        return raw is not null && raw.StartsWith('"') ? JsonSerializer.Deserialize<string>(raw) : raw;
    }

    private static int So(string[] parts, int index) =>
        parts.Length > index && int.TryParse(parts[index], CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static string KhoaHash(long dotId) => $"upm:xet-giao:dot:{dotId.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>Gắn DotId ngay khi tạo job để API tiến độ kiểm tra phạm vi cả khi job còn Enqueued.</summary>
public sealed class XetGiaoJobMetadataFilter : Hangfire.Client.IClientFilter
{
    public void OnCreating(Hangfire.Client.CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Job.Type != typeof(XetGiaoDotHangfireJob) || context.Job.Args.Count == 0) return;
        if (context.Job.Args[0] is long dotId)
        {
            context.SetJobParameter(XetGiaoDotHangfireJob.ThamSoDot, dotId.ToString(CultureInfo.InvariantCulture));
            context.SetJobParameter(XetGiaoDotHangfireJob.ThamSoTienDo, "0/0/0");
        }
    }

    public void OnCreated(Hangfire.Client.CreatedContext context) { }
}
