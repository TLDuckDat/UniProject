using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Common;
using Upm.Application.DeTaiPhanCong;
using Upm.Application.QuyenDocDuyet;
using Upm.Application.TienDo;
using Upm.Application.Workflow;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Workflow;

namespace Upm.Api.IntegrationTests.QuyenDocDuyet;

/// <summary>
/// UC-3b trên SQL Server thật: nộp quyển + hash, ý kiến GVHD (D05), đọc duyệt và sửa gấp (D06, D08, D10),
/// đạo văn (D11), bản bảo vệ (D13), nộp muộn có gia hạn (D12), GVĐD trễ hạn và thay GVĐD (D09).
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class QuyenDocDuyetTests(UpmApiFactory factory)
{
    private const long HN = 1;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ================================================================= Nộp quyển, hash, T07

    [Fact]
    public async Task Nop_quyen_luu_hash_chuyen_T07_va_khong_nop_lai_khi_da_nop()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN001", gvdd: "GV003");
        var sv = await Login("sv.65hn001");

        var pb = await NopQuyenAsync(sv, hoSo, "quyen-v1.pdf", "%PDF-1.4 quyen v1");
        Assert.Equal(1, pb.SoPhienBan);
        Assert.Equal(LoaiBanQuyen.NopQuyen, pb.LoaiBan);
        Assert.Equal(64, pb.HashSHA256.Length);
        Assert.Equal(TrangThaiSVTrongDot.DA_NOP_QUYEN, await TrangThaiAsync(hoSo));

        // Đã nộp (DA_NOP_QUYEN): không được nộp thêm.
        using (var lai = new MultipartFormDataContent())
        {
            lai.Add(new ByteArrayContent("%PDF lan 2"u8.ToArray()), "File", "quyen-lai.pdf");
            var res = await sv.PostAsync($"/api/v1/svtrongdot/{hoSo}/quyen", lai, Ct);
            Assert.Contains("UPM_QUYEN_TRANG_THAI", await res.Content.ReadAsStringAsync(Ct));
        }
        var lichSu = await Doc<List<PhienBanQuyenItem>>(await sv.GetAsync($"/api/v1/svtrongdot/{hoSo}/quyen/lich-su", Ct));
        Assert.Single(lichSu);
        Assert.Equal("quyen-v1.pdf", lichSu[0].TenFile);
    }

    // ================================================================= D05 + D07: ý kiến GVHD

    [Fact]
    public async Task D05_GVHD_khong_dong_y_bat_buoc_ly_do_va_chuyen_PHAI_LAM_LAI()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN004", gvdd: "GV003");
        await NopQuyenAsync(await Login("sv.65hn004"), hoSo, "quyen.pdf", "%PDF quyen");
        var gv = await Login("gv001");

        await AssertLoiAsync(await gv.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/y-kien-gvhd",
            new YKienGvhdRequest(false, null, null, null), Json, Ct), "UPM_D05_THIEU_LY_DO");

        var yKien = await Doc<YKienGvhdItem>(await gv.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/y-kien-gvhd",
            new YKienGvhdRequest(false, "Chưa đạt yêu cầu nội dung chương 3.", 5m, null), Json, Ct));
        Assert.False(yKien.DongY);
        Assert.Equal(TrangThaiSVTrongDot.PHAI_LAM_LAI, await TrangThaiAsync(hoSo));
    }

    // ================================================================= D06, D08, D10: đọc duyệt, sửa gấp, vòng 2

    [Fact]
    public async Task D06_D08_D10_doc_duyet_sua_gap_mot_vong_va_diem_GVDD_rieng()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN001", gvdd: "GV003");
        var sv = await Login("sv.65hn001");
        await NopQuyenAsync(sv, hoSo, "quyen-v1.pdf", "%PDF v1 noi dung");
        var gvdd = await Login("gv003");

        // D10: đồng ý thì phải có điểm.
        await AssertLoiAsync(await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet",
            new DocDuyetRequest(KetQuaDocDuyetLoai.DongY, null, null, null), Json, Ct), "UPM_D10_THIEU_DIEM_GVDD");

        // D06: hạn sửa gấp ngoài khoảng cho phép (1–số ngày cấu hình) bị từ chối.
        await AssertLoiAsync(await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet",
            new DocDuyetRequest(KetQuaDocDuyetLoai.SuaGap, "Sửa bảng số liệu.", null, Today(40)), Json, Ct), "UPM_D06_HAN_SUA_KHONG_HOP_LE");

        // Sửa gấp vòng 1 → T08 (YEU_CAU_CHINH_SUA).
        var sua = await Doc<DocDuyetItem>(await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet",
            new DocDuyetRequest(KetQuaDocDuyetLoai.SuaGap, "Sửa bảng số liệu.", null, Today(2)), Json, Ct));
        Assert.Equal(1, sua.VongDoc);
        Assert.Equal(TrangThaiSVTrongDot.YEU_CAU_CHINH_SUA, await TrangThaiAsync(hoSo));

        // SV nộp bản sửa gấp → T09, bản mới có số phiên bản 2.
        var ban2 = await NopQuyenAsync(sv, hoSo, "quyen-v2.pdf", "%PDF v2 noi dung da sua");
        Assert.Equal(2, ban2.SoPhienBan);
        Assert.Equal(LoaiBanQuyen.BanSuaGap, ban2.LoaiBan);
        Assert.Equal(TrangThaiSVTrongDot.DA_NOP_QUYEN, await TrangThaiAsync(hoSo));

        // D08: vòng 2 không còn sửa gấp; GVĐD phải chọn đồng ý / không đồng ý.
        await AssertLoiAsync(await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet",
            new DocDuyetRequest(KetQuaDocDuyetLoai.SuaGap, "Sửa thêm.", null, Today(2)), Json, Ct), "UPM_D08_HET_VONG_SUA_GAP");

        var dongY = await Doc<DocDuyetItem>(await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet",
            new DocDuyetRequest(KetQuaDocDuyetLoai.DongY, null, 8.5m, null), Json, Ct));
        Assert.Equal(2, dongY.VongDoc);
        Assert.Equal(ban2.Id, dongY.PhienBanQuyenId);
    }

    // ================================================================= D11: đạo văn vượt ngưỡng

    [Fact]
    public async Task D11_Dao_van_vuot_nguong_chan_bao_ve()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN005", gvdd: "GV003");
        var pb = await NopQuyenAsync(await Login("sv.65hn005"), hoSo, "quyen.pdf", "%PDF quyen dao van");
        var ctdtId = await CtdtIdAsync("65HN005");
        var tsId = await BatDaoVanAsync(ctdtId, 20m);
        try
        {
            var bm = await Login("tbm.cau.hn");
            var kq = await Doc<KiemTraDaoVanItem>(await bm.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/dao-van/nhap-tay",
                new NhapTayDaoVanRequest("Turnitin (khoa)", 35m), Json, Ct));
            Assert.True(kq.VuotNguong);
            Assert.Equal(20m, kq.NguongApDungPct);

            var baoVe = await Doc<KiemTraBaoVeItem>(await bm.GetAsync($"/api/v1/svtrongdot/{hoSo}/kiem-tra-bao-ve", Ct));
            Assert.False(baoVe.DuocBaoVe);
            Assert.Contains(baoVe.LyDo, x => x.StartsWith("D11", StringComparison.Ordinal));

            // Dưới ngưỡng thì không vướng D11 (bản ghi mới nhất là kết quả quét mới).
            var sach = await Doc<KiemTraDaoVanItem>(await bm.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/dao-van/nhap-tay",
                new NhapTayDaoVanRequest("Turnitin (khoa)", 5m), Json, Ct));
            Assert.False(sach.VuotNguong);
            Assert.DoesNotContain(
                (await Doc<KiemTraBaoVeItem>(await bm.GetAsync($"/api/v1/svtrongdot/{hoSo}/kiem-tra-bao-ve", Ct))).LyDo,
                x => x.StartsWith("D11", StringComparison.Ordinal));
            Assert.True(pb.Id > 0);
        }
        finally
        {
            await XoaThamSoDaoVanAsync(tsId);
        }
    }

    // ================================================================= D13: bản bảo vệ và hash

    [Fact]
    public async Task D13_Ban_bao_ve_la_ban_GVDD_da_doc_va_hash_doi_khi_nop_ban_khac()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN001", gvdd: "GV003");
        var sv = await Login("sv.65hn001");
        var v1 = await NopQuyenAsync(sv, hoSo, "quyen-v1.pdf", "%PDF ban thu nhat");
        var gvdd = await Login("gv003");
        var suaGap = await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet", new DocDuyetRequest(KetQuaDocDuyetLoai.SuaGap, "Sửa.", null, Today(2)), Json, Ct);
        Assert.True(suaGap.IsSuccessStatusCode, await suaGap.Content.ReadAsStringAsync(Ct));
        var v2 = await NopQuyenAsync(sv, hoSo, "quyen-v2.pdf", "%PDF ban thu hai da sua");
        await gvdd.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/doc-duyet", new DocDuyetRequest(KetQuaDocDuyetLoai.DongY, null, 9m, null), Json, Ct);

        // Hash thay đổi khi nộp bản khác.
        Assert.NotEqual(v1.HashSHA256, v2.HashSHA256);

        // Chưa đánh dấu → chưa đủ điều kiện bảo vệ.
        var bm = await Login("tbm.cau.hn");
        var truocDanhDau = await Doc<KiemTraBaoVeItem>(await bm.GetAsync($"/api/v1/svtrongdot/{hoSo}/kiem-tra-bao-ve", Ct));
        Assert.Contains(truocDanhDau.LyDo, x => x.Contains("bản bảo vệ", StringComparison.Ordinal));

        // Đánh dấu bản GVĐD đã đọc và đồng ý (v2), không phải bản cũ.
        var danhDau = await Doc<BanBaoVeItem>(await bm.PostAsync($"/api/v1/svtrongdot/{hoSo}/ban-bao-ve", null, Ct));
        Assert.Equal(v2.Id, danhDau.PhienBanQuyenId);
        Assert.Equal(v2.HashSHA256, danhDau.HashSHA256);

        var sauDanhDau = await Doc<KiemTraBaoVeItem>(await bm.GetAsync($"/api/v1/svtrongdot/{hoSo}/kiem-tra-bao-ve", Ct));
        Assert.True(sauDanhDau.DuocBaoVe, string.Join("; ", sauDanhDau.LyDo));
    }

    // ================================================================= D12: nộp muộn cần gia hạn

    [Fact]
    public async Task D12_Nop_qua_han_bi_chan_tru_khi_co_gia_han_duyet()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-100));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN004", gvdd: "GV003");
        var sv = await Login("sv.65hn004");

        using (var muon = new MultipartFormDataContent())
        {
            muon.Add(new ByteArrayContent("%PDF muon"u8.ToArray()), "File", "quyen.pdf");
            var res = await sv.PostAsync($"/api/v1/svtrongdot/{hoSo}/quyen", muon, Ct);
            Assert.Contains("UPM_D12_QUA_HAN_NOP", await res.Content.ReadAsStringAsync(Ct));
        }

        var don = await Doc<DonTienDoItem>(await sv.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/don-tien-do",
            new DonTienDoTaoRequest(LoaiDon.GiaHan, "Thiếu dữ liệu thực nghiệm."), Json, Ct));
        Assert.Equal(HttpStatusCode.NoContent, (await (await Login("gv001")).PostAsJsonAsync($"/api/v1/tien-do/don/{don.DonTuId}/duyet",
            new DuyetDonTienDoRequest(true, Today(7), null), Json, Ct)).StatusCode);

        var pb = await NopQuyenAsync(sv, hoSo, "quyen-tre.pdf", "%PDF nop tre co gia han");
        Assert.True(pb.NopQuaHan);
        Assert.Equal(TrangThaiSVTrongDot.DA_NOP_QUYEN, await TrangThaiAsync(hoSo));
    }

    // ================================================================= D09: GVĐD trễ hạn → BM thay

    [Fact]
    public async Task D09_GVDD_tre_han_BM_thay_duoc_con_SV_khong_bi_loi()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hoSo = await ChuanBiAsync(pdt, dot.Id, "65HN005", gvdd: "GV003");
        await NopQuyenAsync(await Login("sv.65hn005"), hoSo, "quyen.pdf", "%PDF quyen cho doc");
        var tbm = await Login("tbm.cau.hn");
        var gvdd = await Login("gv003");

        // Vừa nộp: chưa quá hạn đọc → không thay được; hàng đợi GVĐD có hạn đọc.
        await AssertLoiAsync(await tbm.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/thay-gvdd",
            new ThayGvddRequest(await GiangVienIdAsync("GV002"), "GVĐD đi công tác."), Json, Ct), "UPM_D09_CHUA_QUA_HAN");
        var hangDoi = await Doc<List<QuyenCanDocItem>>(await gvdd.GetAsync($"/api/v1/quyen/can-doc?dotId={dot.Id}", Ct));
        var muc = Assert.Single(hangDoi);
        Assert.Equal(1, muc.VongDoc);
        Assert.InRange(muc.ConLaiNgay, 6, 7);

        // Đồng hồ chạy qua hạn đọc (7 ngày): BM thấy GVĐD trễ hạn và thay được.
        factory.Clock.Lech = factory.Clock.Lech + TimeSpan.FromDays(10);
        try
        {
            var treHan = await Doc<List<GvddTreHanItem>>(await tbm.GetAsync($"/api/v1/quyen/gvdd-tre-han?dotId={dot.Id}", Ct));
            Assert.Contains(treHan, x => x.SVTrongDotId == hoSo && x.MaGVDD == "GV003");

            Assert.Equal(HttpStatusCode.NoContent, (await tbm.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/thay-gvdd",
                new ThayGvddRequest(await GiangVienIdAsync("GV002"), "GVĐD quá hạn đọc quyển."), Json, Ct)).StatusCode);

            await using var scope = factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var gvdds = await db.PhanCong.AsNoTracking().Where(p => p.SVTrongDotId == hoSo && p.VaiTro == VaiTroPhanCong.GVDD)
                .OrderBy(p => p.Id).ToListAsync(Ct);
            Assert.Equal(2, gvdds.Count);
            Assert.NotNull(gvdds[0].NgayKetThuc);
            Assert.Null(gvdds[1].NgayKetThuc);
            Assert.Equal(TrangThaiSVTrongDot.DA_NOP_QUYEN,
                await db.SVTrongDot.Where(x => x.Id == hoSo).Select(x => x.TrangThai).SingleAsync(Ct));
        }
        finally
        {
            factory.Clock.Lech = factory.Clock.Lech - TimeSpan.FromDays(10);
        }
    }

    // ================================================================= Helpers

    private static DateOnly Today(int offset) => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).AddDays(offset);

    private async Task<HttpClient> Login(string tk)
    {
        var (client, _, _) = await factory.DangNhapAsync(tk, Ct);
        return client;
    }

    private static async Task<DotDATNItem> TaoDotAsync(HttpClient client, DateOnly batDau)
    {
        var req = new DotDATNRequest(HN, $"QD{Guid.NewGuid():N}"[..14], "Đợt UC-3b", "2026-2027", 1, batDau, 12, batDau.AddDays(84));
        var res = await client.PostAsJsonAsync("/api/v1/dot-datn", req, Json, Ct);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<DotDATNItem>(Json, Ct))!;
    }

    /// <summary>SV vào đợt + đề tài LyThuyet đã duyệt + GVHD GV001 + GVĐD (mã) + xét giao + QĐ + T04 (đang thực hiện).</summary>
    private async Task<long> ChuanBiAsync(HttpClient pdt, long dotId, string maSV, string gvdd)
    {
        var hoSo = (await ThemAsync(pdt, dotId, maSV)).Single();
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var de = await Doc<DeTaiItem>(await gv.PostAsJsonAsync("/api/v1/de-tai", new
        {
            dotId, boMonId = boMon, tenDeTai = $"Đề tài QD {Guid.NewGuid():N}"[..24], loaiDeTai = "LyThuyet",
        }, Json, Ct));
        await tbm.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/duyet", new { dongY = true }, Json, Ct);
        var gvhdId = await GiangVienIdAsync("GV001");
        var gvddId = await GiangVienIdAsync(gvdd);
        var pc = await tbm.PostAsJsonAsync("/api/v1/de-tai/phan-cong", new
        {
            svTrongDotId = hoSo, gvhdId, gvddId = (long?)gvddId, dongHDIds = Array.Empty<long>(), deTaiId = de.Id, vuotQuotaXacNhan = false,
        }, Json, Ct);
        Assert.Equal(HttpStatusCode.OK, pc.StatusCode);
        await using (var scope = factory.CreateScope())
        {
            var kq = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dotId, null, ct: Ct);
            Assert.Empty(kq.Loi);
        }
        var qd = await pdt.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/quyet-dinh-giao",
            new QuyetDinhGiaoRequest($"QD-QD-{Guid.NewGuid():N}"[..20], Today(0), null, [hoSo], "Hiệu trưởng"), Json, Ct);
        Assert.Equal(HttpStatusCode.OK, qd.StatusCode);
        await using (var scope = factory.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<INhacViecService>().ChayAsync(factory.Clock.Today, Ct);
        }
        Assert.Equal(TrangThaiSVTrongDot.DANG_THUC_HIEN, await TrangThaiAsync(hoSo));
        return hoSo;
    }

    private static async Task<PhienBanQuyenItem> NopQuyenAsync(HttpClient sv, long hoSoId, string tenFile, string noiDung)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(noiDung)), "File", tenFile);
        return await Doc<PhienBanQuyenItem>(await sv.PostAsync($"/api/v1/svtrongdot/{hoSoId}/quyen", form, Ct));
    }

    private async Task<List<long>> ThemAsync(HttpClient pdt, long dotId, string maSV)
    {
        var ids = await IdsAsync(maSV);
        var res = await pdt.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/sinh-vien", new[] { new HoSoTrongDotRequest(ids.SinhVienId, ids.CTDTId) }, Json, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot
            .Where(x => x.DotId == dotId && x.SinhVienId == ids.SinhVienId).Select(x => x.Id).ToListAsync(Ct);
    }

    private async Task<(long SinhVienId, long CTDTId)> IdsAsync(string maSV)
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var sv = await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SinhVien
            .Where(x => x.MaSV == maSV).Select(x => new { x.Id, x.CTDTId }).SingleAsync(Ct);
        return (sv.Id, sv.CTDTId);
    }

    private async Task<long> CtdtIdAsync(string maSV) => (await IdsAsync(maSV)).CTDTId;

    private async Task<long> BoMonIdAsync(string maBM)
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().BoMon
            .Where(x => x.MaBM == maBM && x.Khoa.CoSoId == HN && x.Khoa.MaKhoa == "CT").Select(x => x.Id).SingleAsync(Ct);
    }

    private async Task<long> GiangVienIdAsync(string maGV)
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().GiangVien
            .Where(x => x.MaGV == maGV).Select(x => x.Id).SingleAsync(Ct);
    }

    /// <summary>
    /// Bật kiểm tra đạo văn cho khoa × chương trình bằng CHÍNH dòng ThamSoDaoVan đã seed (không thêm dòng trùng khóa).
    /// Trả về Id; <see cref="XoaThamSoDaoVanAsync"/> khôi phục trạng thái ban đầu.
    /// </summary>
    private async Task<long> BatDaoVanAsync(long ctdtId, decimal nguong)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var ct = await db.ChuongTrinhDaoTao.AsNoTracking().Where(x => x.Id == ctdtId).Select(x => new { x.KhoaId, x.ChuongTrinh }).SingleAsync(Ct);
        var ts = await db.ThamSoDaoVan.SingleAsync(x => x.KhoaId == ct.KhoaId && x.ChuongTrinh == ct.ChuongTrinh, Ct);
        ts.Bat = true;
        ts.NguongPhanTram = nguong;
        await db.SaveChangesAsync(Ct);
        return ts.Id;
    }

    private async Task XoaThamSoDaoVanAsync(long id)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var ts = await db.ThamSoDaoVan.SingleAsync(x => x.Id == id, Ct);
        ts.Bat = false;
        ts.NguongPhanTram = 25m;
        await db.SaveChangesAsync(Ct);
    }

    private async Task<TrangThaiSVTrongDot> TrangThaiAsync(long hoSoId)
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot
            .Where(x => x.Id == hoSoId).Select(x => x.TrangThai).SingleAsync(Ct);
    }

    private static async Task<T> Doc<T>(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync(Ct);
        Assert.True(res.IsSuccessStatusCode, $"HTTP {(int)res.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    private static async Task AssertLoiAsync(HttpResponseMessage res, string code)
    {
        var body = await res.Content.ReadAsStringAsync(Ct);
        var mongDoi = Upm.Domain.Common.DomainException.LaLoiTuChoiQuyen(code) ? HttpStatusCode.Forbidden : HttpStatusCode.UnprocessableEntity;
        Assert.True(res.StatusCode == mongDoi, $"Mong đợi {(int)mongDoi} {code}, nhận {(int)res.StatusCode}: {body}");
        Assert.Equal(code, JsonDocument.Parse(body).RootElement.GetProperty("code").GetString());
    }
}
