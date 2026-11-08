using System.Reflection;
using Upm.Domain.PhanQuyen;

namespace Upm.Domain.Tests.PhanQuyen;

/// <summary>
/// docs/PHAN-QUYEN.md phải khớp <see cref="BangQuyen"/>: đổi code mà quên sửa tài liệu (hoặc ngược lại) thì test đỏ.
/// Ô chứa "✔" (kể cả "✔⚠") = có quyền; ô trống hoặc chỉ "⚠" = không có quyền.
/// </summary>
public class MaTranPhanQuyenTests
{
    private static readonly IReadOnlyList<string> TatCaQuyen = typeof(Quyen)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    [Fact]
    public void Ma_tran_trong_tai_lieu_khop_BangQuyen_trong_code()
    {
        var (vaiTroCot, dong) = DocMaTran();

        Assert.Equal(VaiTroMa.TatCa.Order(), vaiTroCot.Order());
        Assert.Equal(TatCaQuyen.Order(), dong.Keys.Order());

        foreach (var (quyen, theoVaiTro) in dong)
        {
            foreach (var vaiTro in vaiTroCot)
            {
                var trongCode = BangQuyen.ChoVaiTro([vaiTro]).Contains(quyen);
                Assert.True(theoVaiTro[vaiTro] == trongCode,
                    $"Lệch ma trận: {vaiTro} × {quyen} – tài liệu {(theoVaiTro[vaiTro] ? "✔" : "trống")}, code {(trongCode ? "có" : "không")}.");
            }
        }
    }

    [Fact]
    public void Moi_quyen_co_pham_vi_va_use_case()
    {
        var bang = DocBang();
        Assert.All(bang.Rows, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r[^2]), $"Thiếu phạm vi cho {r[0]}");
            Assert.False(string.IsNullOrWhiteSpace(r[^1]), $"Thiếu use case cho {r[0]}");
        });
    }

    private static (List<string> VaiTro, Dictionary<string, Dictionary<string, bool>> Dong) DocMaTran()
    {
        var bang = DocBang();
        // Cột 0 = quyền; 2 cột cuối = phạm vi, use case; giữa là vai trò.
        var vaiTro = bang.Header.Skip(1).Take(bang.Header.Count - 3).Select(BoDauNhay).ToList();
        var dong = bang.Rows.ToDictionary(
            r => BoDauNhay(r[0]),
            r => vaiTro.Select((v, i) => (v, i)).ToDictionary(x => x.v, x => r[x.i + 1].Contains('✔')));
        return (vaiTro, dong);
    }

    private static (List<string> Header, List<List<string>> Rows) DocBang()
    {
        var lines = File.ReadAllLines(Path.Combine(TimGocRepo(), "docs", "PHAN-QUYEN.md"));
        var batDau = Array.FindIndex(lines, l => l.StartsWith("## 1.", StringComparison.Ordinal));
        Assert.True(batDau >= 0, "Không tìm thấy mục '## 1.' (ma trận) trong docs/PHAN-QUYEN.md");

        var bangLines = lines.Skip(batDau + 1)
            .SkipWhile(l => !l.StartsWith('|'))
            .TakeWhile(l => l.StartsWith('|'))
            .ToList();
        var header = TachO(bangLines[0]);
        var rows = bangLines.Skip(2).Select(TachO).ToList(); // bỏ dòng |---|
        Assert.All(rows, r => Assert.Equal(header.Count, r.Count));
        return (header, rows);
    }

    private static List<string> TachO(string line) =>
        line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

    private static string BoDauNhay(string s) => s.Trim('`');

    private static string TimGocRepo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Upm.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Không tìm thấy thư mục gốc repo (Upm.sln).");
    }
}
