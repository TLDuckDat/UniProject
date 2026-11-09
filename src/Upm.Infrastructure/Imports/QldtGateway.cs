using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Upm.Application.DongBo;
using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;

namespace Upm.Infrastructure.Importing;

public sealed class QldtGateway(UpmDbContext db, IMemoryCache cache, TimeProvider time) : IQldtGateway
{
    private static readonly TimeSpan PreviewTtl = TimeSpan.FromMinutes(30);

    public byte[] TaoFileMau(LoaiImport loai)
    {
        var headers = Headers(loai);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join(';', headers) + "\r\n")).ToArray();
    }

    public async Task<ImportPreview> XemTruocAsync(LoaiImport loai, string fileName, Stream file, CancellationToken ct = default)
    {
        if (!Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase) && !Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UPM_IMPORT_DINH_DANG_KHONG_HOP_LE", "Chỉ hỗ trợ file .csv và .xlsx.");
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();
        CsvTable table;
        try { table = Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ? DocExcel(bytes) : DocCsv(bytes); }
        catch (Exception exception) when (exception is InvalidDataException or IOException or System.Xml.XmlException)
        { throw new DomainException("UPM_IMPORT_FILE_KHONG_DOC_DUOC", "Không đọc được file import; hãy kiểm tra file Excel/CSV hợp lệ."); }
        var errors = new List<ImportRowError>();
        var rows = new List<Dictionary<string, string>>();
        var valid = new List<object>();
        var naturalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headers = table.Headers.Select(NormalizeHeader).ToArray();
        var requiredMissing = Headers(loai).Where(h => Required(loai).Contains(h) && !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToArray();
        foreach (var missing in requiredMissing) errors.Add(new ImportRowError(1, missing, $"Thiếu cột bắt buộc '{missing}'."));
        for (var index = 0; index < table.Rows.Count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var line = index + 2;
            var raw = table.Rows[index];
            if (raw.All(string.IsNullOrWhiteSpace)) continue;
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Length; c++) row[headers[c]] = c < raw.Count ? raw[c].Trim() : string.Empty;
            rows.Add(row);
            var before = errors.Count;
            var parsed = await ValidateRowAsync(loai, line, row, errors, ct);
            if (parsed is not null && errors.Count == before)
            {
                var key = NaturalKey(loai, parsed);
                if (!naturalKeys.Add(key)) errors.Add(new ImportRowError(line, "MaSV/MaHP", "Khóa tự nhiên bị trùng trong file import."));
                else valid.Add(parsed);
            }
        }
        var id = Guid.NewGuid();
        var expires = time.GetUtcNow().UtcDateTime.Add(PreviewTtl);
        cache.Set(id, new ImportBatch(loai, valid, errors.Count, rows.Count), PreviewTtl);
        return new ImportPreview(id, loai, Path.GetFileName(fileName), rows.Count, valid.Count, errors, expires);
    }

    public async Task<ImportCommitResult> XacNhanAsync(Guid previewId, CancellationToken ct = default)
    {
        if (!cache.TryGetValue<ImportBatch>(previewId, out var batch) || batch is null)
            throw new DomainException("UPM_IMPORT_PREVIEW_HET_HAN", "Phiên xem trước không tồn tại hoặc đã hết hạn. Hãy tải file lên lại.");
        var inserted = 0;
        var updated = 0;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = time.GetUtcNow().UtcDateTime;
        foreach (var item in batch.Items)
        {
            switch (item)
            {
                case StudentImport r:
                {
                    var entity = await db.SinhVien.SingleOrDefaultAsync(x => x.MaSV == r.MaSV, ct);
                    var ctdt = await db.ChuongTrinhDaoTao.SingleAsync(x => x.MaCTDT == r.MaCTDT, ct);
                    var coSo = await db.CoSo.SingleAsync(x => x.MaCoSo == r.MaCoSo, ct);
                    var isNew = entity is null;
                    entity ??= new SinhVien { MaSV = r.MaSV };
                    entity.HoTen = r.HoTen; entity.NgaySinh = r.NgaySinh; entity.KhoaHoc = r.KhoaHoc; entity.CTDTId = ctdt.Id;
                    entity.CoSoId = coSo.Id; entity.TrangThaiHocTap = r.TrangThai; entity.NgayNhapHoc = r.NgayNhapHoc; entity.Email = r.Email;
                    entity.NguonDongBo = "QLDT_IMPORT_FILE"; entity.DongBoLuc = now;
                    if (isNew) db.SinhVien.Add(entity);
                    if (isNew) inserted++; else updated++;
                    break;
                }
                case GradeImport r:
                {
                    var sv = await db.SinhVien.SingleAsync(x => x.MaSV == r.MaSV, ct);
                    var hp = await db.HocPhan.SingleAsync(x => x.MaHP == r.MaHP, ct);
                    var entity = await db.BangDiem.SingleOrDefaultAsync(x => x.SinhVienId == sv.Id && x.HocPhanId == hp.Id && x.HocKy == r.HocKy && x.LanHoc == r.LanHoc, ct);
                    var isNew = entity is null;
                    entity ??= new BangDiem { SinhVienId = sv.Id, HocPhanId = hp.Id, HocKy = r.HocKy, LanHoc = r.LanHoc };
                    entity.Diem10 = r.Diem10; entity.DiemChu = r.DiemChu; entity.Diem4 = r.Diem4; entity.TrangThaiDiem = r.TrangThai; entity.NguonDongBo = "QLDT_IMPORT_FILE"; entity.DongBoLuc = now;
                    if (isNew) db.BangDiem.Add(entity);
                    if (isNew) inserted++; else updated++;
                    break;
                }
                case DisciplineImport r:
                {
                    var sv = await db.SinhVien.SingleAsync(x => x.MaSV == r.MaSV, ct);
                    var entity = await db.KyLuatSV.SingleOrDefaultAsync(x => x.SinhVienId == sv.Id && x.MucKyLuat == r.MucKyLuat && x.TuNgay == r.TuNgay, ct);
                    var isNew = entity is null;
                    entity ??= new KyLuatSV { SinhVienId = sv.Id, MucKyLuat = r.MucKyLuat, TuNgay = r.TuNgay };
                    entity.DenNgay = r.DenNgay; entity.DinhChiHocTap = r.DinhChi; entity.TruyCuuHinhSu = r.TruyCuu;
                    entity.NguonCTSV = "CTSV"; entity.NguonDongBo = "CTSV_IMPORT_FILE"; entity.DongBoLuc = now;
                    if (isNew) db.KyLuatSV.Add(entity);
                    if (isNew) inserted++; else updated++;
                    break;
                }
                case ProgressImport r:
                {
                    var sv = await db.SinhVien.SingleAsync(x => x.MaSV == r.MaSV, ct);
                    var entity = await db.ThongTinTienDoSV.SingleOrDefaultAsync(x => x.SinhVienId == sv.Id, ct);
                    var isNew = entity is null;
                    entity ??= new ThongTinTienDoSV { SinhVienId = sv.Id };
                    entity.KyCuoiKeHoach = r.KyCuoiKeHoach; entity.HanToiDa = r.HanToiDa; entity.DungTienDo = r.DungTienDo;
                    entity.NguonDongBo = "QLDT_IMPORT_FILE"; entity.DongBoLuc = now;
                    if (isNew) db.ThongTinTienDoSV.Add(entity);
                    if (isNew) inserted++; else updated++;
                    break;
                }
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        cache.Remove(previewId);
        return new ImportCommitResult(inserted, updated, batch.TotalRows - batch.Items.Count);
    }

    private async Task<object?> ValidateRowAsync(LoaiImport loai, int line, IReadOnlyDictionary<string, string> r, List<ImportRowError> errors, CancellationToken ct)
    {
        string V(string key) => r.GetValueOrDefault(key) ?? string.Empty;
        void RequireField(string key) { if (string.IsNullOrWhiteSpace(V(key))) errors.Add(new ImportRowError(line, key, $"'{key}' là trường bắt buộc.")); }
        bool Date(string key, out DateOnly date) { if (ImportValueParser.TryDate(V(key), out date)) return true; errors.Add(new ImportRowError(line, key, $"Ngày '{V(key)}' không hợp lệ.")); return false; }
        bool Bool(string key, out bool val) { if (bool.TryParse(V(key), out val)) return true; if (V(key) is "1" or "Có" or "Co") { val = true; return true; } if (V(key) is "0" or "Không" or "Khong") { val = false; return true; } errors.Add(new ImportRowError(line, key, $"Giá trị boolean '{V(key)}' không hợp lệ.")); return false; }
        foreach (var k in Required(loai)) RequireField(k);
        if (errors.Any(e => e.Row == line)) return null;
        switch (loai)
        {
            case LoaiImport.SinhVien:
            {
                var okDate = Date("NgaySinh", out var dob) & Date("NgayNhapHoc", out var enroll);
                var okKhoa = int.TryParse(V("KhoaHoc"), out var cohort);
                if (!okKhoa) errors.Add(new ImportRowError(line, "KhoaHoc", "Khóa học phải là số nguyên."));
                if (!Enum.TryParse<TrangThaiHocTap>(V("TrangThaiHocTap"), true, out var status)) { errors.Add(new ImportRowError(line, "TrangThaiHocTap", "Trạng thái học tập không hợp lệ.")); }
                if (okDate && okKhoa && Enum.TryParse<TrangThaiHocTap>(V("TrangThaiHocTap"), true, out status))
                {
                    var maCtdt = V("MaCTDT"); var maCoSo = V("MaCoSo");
                    if (!await db.ChuongTrinhDaoTao.AnyAsync(x => x.MaCTDT == maCtdt, ct)) errors.Add(new ImportRowError(line, "MaCTDT", "Không tìm thấy chương trình đào tạo."));
                    if (!await db.CoSo.AnyAsync(x => x.MaCoSo == maCoSo, ct)) errors.Add(new ImportRowError(line, "MaCoSo", "Không tìm thấy cơ sở."));
                    return errors.Any(e => e.Row == line) ? null : new StudentImport(V("MaSV"), V("HoTen"), dob, cohort, V("MaCTDT"), V("MaCoSo"), status, enroll, NullIfEmpty(V("Email")));
                }
                return null;
            }
            case LoaiImport.BangDiem:
            {
                if (!int.TryParse(V("LanHoc"), out var attempt) || attempt < 1) errors.Add(new ImportRowError(line, "LanHoc", "Lần học phải là số nguyên dương."));
                decimal? d10 = ParseOptionalDecimal(V("Diem10"), "Diem10"); decimal? d4 = ParseOptionalDecimal(V("Diem4"), "Diem4");
                if (d10 is < 0 or > 10) errors.Add(new ImportRowError(line, "Diem10", "Điểm hệ 10 phải nằm trong khoảng 0–10."));
                if (d4 is < 0 or > 4) errors.Add(new ImportRowError(line, "Diem4", "Điểm hệ 4 phải nằm trong khoảng 0–4."));
                if (!Enum.TryParse<TrangThaiDiem>(V("TrangThaiDiem"), true, out var status)) errors.Add(new ImportRowError(line, "TrangThaiDiem", "Trạng thái điểm không hợp lệ."));
                var maSv = V("MaSV"); var maHp = V("MaHP");
                if (!await db.SinhVien.AnyAsync(x => x.MaSV == maSv, ct)) errors.Add(new ImportRowError(line, "MaSV", "Không tìm thấy sinh viên."));
                if (!await db.HocPhan.AnyAsync(x => x.MaHP == maHp, ct)) errors.Add(new ImportRowError(line, "MaHP", "Không tìm thấy học phần."));
                return errors.Any(e => e.Row == line) ? null : new GradeImport(V("MaSV"), V("MaHP"), V("HocKy"), attempt, d10, NullIfEmpty(V("DiemChu")), d4, status);
            }
            case LoaiImport.KyLuatSV:
            {
                var okFrom = Date("TuNgay", out var from); DateOnly? to = null;
                if (!string.IsNullOrWhiteSpace(V("DenNgay"))) { if (Date("DenNgay", out var parsedTo)) to = parsedTo; }
                var okSuspend = Bool("DinhChiHocTap", out var suspend); var okCriminal = Bool("TruyCuuHinhSu", out var criminal);
                var maSv = V("MaSV");
                if (!await db.SinhVien.AnyAsync(x => x.MaSV == maSv, ct)) errors.Add(new ImportRowError(line, "MaSV", "Không tìm thấy sinh viên."));
                return errors.Any(e => e.Row == line) || !okFrom || !okSuspend || !okCriminal ? null : new DisciplineImport(V("MaSV"), V("MucKyLuat"), from, to, suspend, criminal);
            }
            case LoaiImport.ThongTinTienDoSV:
            {
                var okDate = Date("HanToiDa", out var deadline); var okRun = Bool("DungTienDo", out var onTrack);
                var maSv = V("MaSV");
                if (!await db.SinhVien.AnyAsync(x => x.MaSV == maSv, ct)) errors.Add(new ImportRowError(line, "MaSV", "Không tìm thấy sinh viên."));
                return errors.Any(e => e.Row == line) || !okDate || !okRun ? null : new ProgressImport(V("MaSV"), V("KyCuoiKeHoach"), deadline, onTrack);
            }
            default: return null;
        }

        decimal? ParseOptionalDecimal(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (ImportValueParser.TryDecimal(value, out var parsed)) return parsed;
            errors.Add(new ImportRowError(line, field, $"Điểm '{value}' không hợp lệ."));
            return null;
        }
    }

    private static CsvTable DocCsv(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        var first = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).FirstOrDefault() ?? string.Empty;
        var delimiter = first.Count(c => c == ';') > first.Count(c => c == ',') ? ';' : ',';
        var records = ParseCsv(text, delimiter);
        return new CsvTable(records.FirstOrDefault() ?? [], records.Skip(1).ToList());
    }

    private static List<List<string>> ParseCsv(string input, char delimiter)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '"') { if (quoted && i + 1 < input.Length && input[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
            else if (!quoted && c == delimiter) { row.Add(cell.ToString()); cell.Clear(); }
            else if (!quoted && c is '\r' or '\n') { if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = []; }
            else cell.Append(c);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }

    private static CsvTable DocExcel(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var shared = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null) { using var s = sharedEntry.Open(); var doc = XDocument.Load(s); shared = doc.Descendants().Where(x => x.Name.LocalName == "si").Select(si => string.Concat(si.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value))).ToList(); }
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("Không tìm thấy worksheet đầu tiên trong file Excel.");
        using var stream = sheet.Open(); var xml = XDocument.Load(stream);
        var rows = new List<List<string>>();
        foreach (var row in xml.Descendants().Where(x => x.Name.LocalName == "row"))
        {
            var cells = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements().Where(x => x.Name.LocalName == "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? "A1"; var column = ColumnNumber(reference);
                var type = (string?)cell.Attribute("t"); var value = cell.Elements().FirstOrDefault(x => x.Name.LocalName == "v")?.Value ?? string.Empty;
                if (type == "s" && int.TryParse(value, out var ix) && ix < shared.Count) value = shared[ix];
                if (type == "inlineStr") value = string.Concat(cell.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value));
                cells[column] = value;
            }
            var max = cells.Keys.DefaultIfEmpty(-1).Max(); rows.Add(Enumerable.Range(0, max + 1).Select(i => cells.GetValueOrDefault(i, string.Empty)).ToList());
        }
        return new CsvTable(rows.FirstOrDefault() ?? [], rows.Skip(1).ToList());
    }

    private static int ColumnNumber(string cellRef)
    {
        var value = 0;
        foreach (var c in cellRef.TakeWhile(char.IsLetter)) value = value * 26 + char.ToUpperInvariant(c) - 'A' + 1;
        return value - 1;
    }

    private static string NormalizeHeader(string value) => Regex.Replace(value.Trim(), "\\s+", "");
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string NaturalKey(LoaiImport loai, object row) => row switch
    {
        StudentImport x => $"{loai}:{x.MaSV}",
        GradeImport x => $"{loai}:{x.MaSV}:{x.MaHP}:{x.HocKy}:{x.LanHoc}",
        DisciplineImport x => $"{loai}:{x.MaSV}:{x.MucKyLuat}:{x.TuNgay:yyyy-MM-dd}",
        ProgressImport x => $"{loai}:{x.MaSV}",
        _ => throw new ArgumentOutOfRangeException(nameof(row)),
    };
    private static string[] Headers(LoaiImport x) => x switch
    {
        LoaiImport.SinhVien => ["MaSV", "HoTen", "NgaySinh", "KhoaHoc", "MaCTDT", "MaCoSo", "TrangThaiHocTap", "NgayNhapHoc", "Email"],
        LoaiImport.BangDiem => ["MaSV", "MaHP", "HocKy", "LanHoc", "Diem10", "DiemChu", "Diem4", "TrangThaiDiem"],
        LoaiImport.KyLuatSV => ["MaSV", "MucKyLuat", "TuNgay", "DenNgay", "DinhChiHocTap", "TruyCuuHinhSu"],
        LoaiImport.ThongTinTienDoSV => ["MaSV", "KyCuoiKeHoach", "HanToiDa", "DungTienDo"],
        _ => throw new ArgumentOutOfRangeException(nameof(x)),
    };
    private static HashSet<string> Required(LoaiImport x) => x switch
    {
        LoaiImport.SinhVien => ["MaSV", "HoTen", "NgaySinh", "KhoaHoc", "MaCTDT", "MaCoSo", "TrangThaiHocTap", "NgayNhapHoc"],
        LoaiImport.BangDiem => ["MaSV", "MaHP", "HocKy", "LanHoc", "TrangThaiDiem"],
        LoaiImport.KyLuatSV => ["MaSV", "MucKyLuat", "TuNgay", "DinhChiHocTap", "TruyCuuHinhSu"],
        LoaiImport.ThongTinTienDoSV => ["MaSV", "KyCuoiKeHoach", "HanToiDa", "DungTienDo"],
        _ => [],
    };

    private sealed record CsvTable(IReadOnlyList<string> Headers, IReadOnlyList<List<string>> Rows);
    private sealed record ImportBatch(LoaiImport Loai, IReadOnlyList<object> Items, int ErrorCount, int TotalRows);
    private sealed record StudentImport(string MaSV, string HoTen, DateOnly NgaySinh, int KhoaHoc, string MaCTDT, string MaCoSo, TrangThaiHocTap TrangThai, DateOnly NgayNhapHoc, string? Email);
    private sealed record GradeImport(string MaSV, string MaHP, string HocKy, int LanHoc, decimal? Diem10, string? DiemChu, decimal? Diem4, TrangThaiDiem TrangThai);
    private sealed record DisciplineImport(string MaSV, string MucKyLuat, DateOnly TuNgay, DateOnly? DenNgay, bool DinhChi, bool TruyCuu);
    private sealed record ProgressImport(string MaSV, string KyCuoiKeHoach, DateOnly HanToiDa, bool DungTienDo);
}
