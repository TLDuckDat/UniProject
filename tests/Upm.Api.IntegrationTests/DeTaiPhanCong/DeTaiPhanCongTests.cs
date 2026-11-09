using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Common;
using Upm.Application.DeTaiPhanCong;
using Upm.Application.Workflow;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Workflow;

namespace Upm.Api.IntegrationTests.DeTaiPhanCong;

/// <summary>
/// UC-2 trên SQL Server thật: đề xuất, nguyện vọng khóa đề tài, gợi ý phân công, B03/B06/B07/B11,
/// đổi GVHD giữ lịch sử (B08), đổi đề tài trước/sau hạn (B09) và T03 sau khi phân công.
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class DeTaiPhanCongTests(UpmApiFactory factory)
{
    private const long HN = 1;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ================================================================= B01, B02, B10

    [Fact]
    public async Task B01_B02_B10_GV_de_xuat_mac_dinh_1_SV_nhom_chi_cho_che_tao_va_canh_bao_trung_nam_truoc()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var tag = Tag();

        var dotCu = await TaoDotAsync(pdt, HN, Today(-400), "2025-2026");
        var tenCu = $"Thiết kế hệ thống quản lý kho hàng {tag}";
        var cu = await TaoDeTaiAsync(gv, dotCu.Id, boMon, tenCu, "LyThuyet", null);

        // B10: đợt mới, tên giống hệt sau khi bỏ dấu → cảnh báo trùng và liệt kê đề tài năm trước.
        var dotMoi = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var moi = await TaoDeTaiRawAsync(gv, new { dotId = dotMoi.Id, boMonId = boMon, tenDeTai = $"Thiet ke he thong quan ly kho hang {tag}", loaiDeTai = "LyThuyet" });
        Assert.Equal(HttpStatusCode.Created, moi.StatusCode);
        var item = await Doc<DeTaiItem>(moi);
        Assert.True(item.CanhBaoTrung);
        Assert.Contains(item.TrungVoi, x => x.Id == cu.Id && x.TrungTen);
        // B01: không truyền maxSV → mặc định 1 SV.
        Assert.Equal(1, item.MaxSV);
        Assert.Equal(TrangThaiDuyetDeTai.ChoDuyet, item.TrangThaiDuyet);

        // B02: đề tài lý thuyết không nhận nhóm.
        await AssertLoiAsync(await TaoDeTaiRawAsync(gv, new { dotId = dotMoi.Id, boMonId = boMon, tenDeTai = "Đề tài nhóm " + tag, loaiDeTai = "LyThuyet", maxSV = 2 }),
            "UPM_B02_NHOM_KHONG_HOP_LE");
        // B01: vượt 3 SV bị từ chối.
        await AssertLoiAsync(await TaoDeTaiRawAsync(gv, new { dotId = dotMoi.Id, boMonId = boMon, tenDeTai = "Đề tài 4 " + tag, loaiDeTai = "CheTao", maxSV = 4 }),
            "UPM_B01_SO_SV_KHONG_HOP_LE");

        // B02: chế tạo nhóm 2 SV được tạo nhưng phải chờ Bộ môn duyệt.
        var nhom = await Doc<DeTaiItem>(await TaoDeTaiRawAsync(gv, new { dotId = dotMoi.Id, boMonId = boMon, tenDeTai = "Chế tạo nhóm " + tag, loaiDeTai = "CheTao", maxSV = 2 }));
        Assert.Equal(2, nhom.MaxSV);
        Assert.Equal(TrangThaiDuyetDeTai.ChoDuyet, nhom.TrangThaiDuyet);
        var duyet = await Doc<DeTaiItem>(await tbm.PostAsJsonAsync($"/api/v1/de-tai/{nhom.Id}/duyet", new { dongY = true }, Json, Ct));
        Assert.Equal(TrangThaiDuyetDeTai.DaDuyet, duyet.TrangThaiDuyet);
    }

    // ================================================================= B04

    [Fact]
    public async Task B04_SV_tu_de_xuat_GV_xac_nhan_roi_Bo_mon_duyet()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var gv2 = await Login("gv003");
        var sv = await Login("sv.65hn001");
        var tbm = await Login("tbm.cau.hn");
        var dot = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var hoSo = (await ThemAsync(pdt, dot.Id, "65HN001")).Single();
        var gvId = await GiangVienIdAsync("GV001");
        var gv2Id = await GiangVienIdAsync("GV002");

        var de = await Doc<DeTaiItem>(await sv.PostAsJsonAsync("/api/v1/de-tai/sv-de-xuat", new
        {
            svTrongDotId = hoSo, giangVienDeNghiId = gvId, tenDeTai = "SV tự đề xuất " + Tag(), moTa = "Mô tả", loaiDeTai = "LyThuyet",
        }, Json, Ct));
        Assert.Equal(NguonDeXuat.SV, de.NguonDeXuat);
        Assert.Equal(TrangThaiDuyetDeTai.Nhap, de.TrangThaiDuyet); // chờ GV xác nhận

        // Sai GV được đề nghị không xác nhận được.
        await AssertLoiAsync(await gv2.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/gv-xac-nhan", new { dongY = true }, Json, Ct),
            "UPM_DE_TAI_KHONG_CHO_XAC_NHAN");

        var xacNhan = await Doc<DeTaiItem>(await gv.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/gv-xac-nhan", new { dongY = true }, Json, Ct));
        Assert.Equal(TrangThaiDuyetDeTai.ChoDuyet, xacNhan.TrangThaiDuyet);

        var boMonDuyet = await Doc<DeTaiItem>(await tbm.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/duyet", new { dongY = true }, Json, Ct));
        Assert.Equal(TrangThaiDuyetDeTai.DaDuyet, boMonDuyet.TrangThaiDuyet);
        Assert.NotNull(boMonDuyet.SVDeXuatId);
    }

    // ================================================================= B05, nguyện vọng khóa đề tài

    [Fact]
    public async Task B05_Goi_y_phan_cong_khong_ghi_du_lieu_va_nguyen_vong_khoa_de_tai_khi_du_SV()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var dot = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var hsA = (await ThemAsync(pdt, dot.Id, "65HN001")).Single();
        var hsB = (await ThemAsync(pdt, dot.Id, "65HN004")).Single();
        var deTai1 = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var deTai3 = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);

        // A đăng ký đề tài 1 và trúng; đề tài 1 đủ 1 SV → khóa (B02 đủ MaxSV).
        var svA = await Login("sv.65hn001");
        await svA.PostAsJsonAsync($"/api/v1/svtrongdot/{hsA}/nguyen-vong", new { deTaiIds = new[] { deTai1.Id } }, Json, Ct);
        await Doc<DangKyNguyenVongItem>(await gv.PostAsJsonAsync($"/api/v1/de-tai/{deTai1.Id}/duyet-dang-ky",
            new { svTrongDotId = hsA, dongY = true }, Json, Ct));

        // B sau đó cũng xin đề tài 1 → không được nhận.
        var svB = await Login("sv.65hn004");
        var dkB = await svB.PostAsJsonAsync($"/api/v1/svtrongdot/{hsB}/nguyen-vong", new { deTaiIds = new[] { deTai1.Id } }, Json, Ct);
        await AssertLoiAsync(dkB, "UPM_DE_TAI_DA_DU_SV");
        await svB.PostAsJsonAsync($"/api/v1/svtrongdot/{hsB}/nguyen-vong", new { deTaiIds = new[] { deTai3.Id } }, Json, Ct);
        var lenBm = await Doc<List<DangKyNguyenVongItem>>(await gv.GetAsync($"/api/v1/de-tai/{deTai3.Id}/dang-ky", Ct));
        Assert.Contains(lenBm, x => x.SVTrongDotId == hsB);

        // B05: gợi ý cho SV chưa có GVHD – chỉ đề xuất, không ghi.
        var goiY = await Doc<List<GoiYPhanCongItem>>(await tbm.GetAsync($"/api/v1/de-tai/goi-y-phan-cong?dotId={dot.Id}", Ct));
        var gyA = goiY.Single(x => x.SVTrongDotId == hsA);
        Assert.Equal(deTai1.Id, gyA.GoiYDeTaiId);
        Assert.NotNull(gyA.GoiYGVHDId);
        Assert.Equal("GV001", gyA.MaGVHD);
        Assert.Equal(deTai3.Id, goiY.Single(x => x.SVTrongDotId == hsB).GoiYDeTaiId);
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            Assert.Equal(0, await db.PhanCong.CountAsync(x => x.SVTrongDot.DotId == dot.Id, Ct));
        }
    }

    // ================================================================= B03, B06, B07, B11

    [Fact]
    public async Task B03_B06_B07_B11_phan_cong_bi_chan_dung_quy_tac_va_truong_BM_duyet_ngoai_le_quota()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var dot = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var hsX = (await ThemAsync(pdt, dot.Id, "65HN001")).Single();
        var hsY = (await ThemAsync(pdt, dot.Id, "65HN004")).Single();
        var hsZ = (await ThemAsync(pdt, dot.Id, "65HN005")).Single();
        var hsW = (await ThemAsync(pdt, dot.Id, "65HN002")).Single();
        var deX = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var deY = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var deZ = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var deW = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var gv1 = await GiangVienIdAsync("GV001");
        var gv2 = await GiangVienIdAsync("GV003");
        var gv6 = await GiangVienIdAsync("GV006");
        var gv2GocQuota = await QuotaAsync("GV003");

        try
        {
            // B06: GVĐD trùng GVHD → chặn cứng.
            await AssertLoiAsync(await PhanCongAsync(tbm, hsX, deX.Id, gv1, gv1, []), "UPM_B06_GVDD_TRUNG_GVHD");

            // B07: GV002 khai báo xung đột với SV X → không được làm GVĐD của X.
            var khaiBao = await Login("gv003");
            var khai = await khaiBao.PostAsJsonAsync("/api/v1/giang-vien/xung-dot", new { sinhVienId = await SinhVienIdAsync("65HN001"), lyDo = "Người thân" }, Json, Ct);
            Assert.Equal(HttpStatusCode.OK, khai.StatusCode);
            await AssertLoiAsync(await PhanCongAsync(tbm, hsX, deX.Id, gv1, gv2, []), "UPM_B07_XUNG_DOT");

            // B11: GV ngoài trường (GV006) đứng một mình → bị chặn; có GV cơ hữu đi kèm → được.
            await AssertLoiAsync(await PhanCongAsync(tbm, hsY, deY.Id, gv6, null, []), "UPM_B11_NGOAI_TRUONG_CAN_CO_HUU");
            Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hsY, deY.Id, gv6, gv1, [])).StatusCode);

            // B03: GV002 quota 1 → nhận SV Z được; nhận SV W bị chặn; Trưởng BM duyệt ngoại lệ thì được và có log.
            await DatQuotaAsync("GV003", 1);
            Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hsZ, deZ.Id, gv2, null, [])).StatusCode);
            await AssertLoiAsync(await PhanCongAsync(tbm, hsW, deW.Id, gv2, null, []), "UPM_B03_VUOT_QUOTA");
            var ngoaiLe = await tbm.PostAsJsonAsync("/api/v1/de-tai/phan-cong", new
            {
                svTrongDotId = hsW, gvhdId = gv2, gvddId = (long?)null, dongHDIds = Array.Empty<long>(), deTaiId = deW.Id, vuotQuotaXacNhan = true,
            }, Json, Ct);
            Assert.Equal(HttpStatusCode.OK, ngoaiLe.StatusCode);
            var lichSu = await Doc<List<PhanCongLichSuItem>>(await tbm.GetAsync($"/api/v1/svtrongdot/{hsW}/lich-su-phan-cong", Ct));
            Assert.Contains(lichSu, x => x.VaiTro == VaiTroPhanCong.GVHD && x.VuotQuotaDuyetBoi);

            // Chỉ Trưởng BM mới được duyệt ngoại lệ: PĐT không có quyền phân công.
            var pdtPhanCong = await pdt.PostAsJsonAsync("/api/v1/de-tai/phan-cong", new { svTrongDotId = hsX, gvhdId = gv1, deTaiId = deX.Id }, Json, Ct);
            Assert.Equal(HttpStatusCode.Forbidden, pdtPhanCong.StatusCode);
        }
        finally
        {
            await DatQuotaAsync("GV003", gv2GocQuota);
        }
    }

    // ================================================================= B08 đổi GVHD giữ lịch sử, T03

    [Fact]
    public async Task B08_Doi_GVHD_sau_QD_giu_lich_su_va_T03_sau_phan_cong()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var dot = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var hoSo = (await ThemAsync(pdt, dot.Id, "65HN001")).Single();
        var deTai = await TaoDeTaiDaDuyetAsync(gv, tbm, dot.Id, boMon);
        var gv1 = await GiangVienIdAsync("GV001");
        var gv2 = await GiangVienIdAsync("GV002");
        await ChayTrucTiepAsync(dot.Id);

        // Chưa phân công GVHD → T03 (QĐ giao) bị chặn.
        await AssertLoiAsync(await GhiQuyetDinhAsync(pdt, dot.Id, hoSo, $"QD-A-{Tag()}"), "UPM_QUYET_DINH_CHUA_CO_DE_TAI");

        Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hoSo, deTai.Id, gv1, null, [])).StatusCode);
        var qd = await GhiQuyetDinhAsync(pdt, dot.Id, hoSo, $"QD-A-{Tag()}");
        Assert.Equal(HttpStatusCode.OK, qd.StatusCode);
        Assert.Equal(TrangThaiSVTrongDot.DA_GIAO, await TrangThaiAsync(hoSo));

        // SV xin đổi GVHD sau QĐ.
        var sv = await Login("sv.65hn001");
        var don = await sv.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/don-dieu-chinh",
            new DonDieuChinhTaoRequest(LoaiDon.DoiGVHD, "Thầy GVHD đi công tác dài hạn."), Json, Ct);
        Assert.Equal(HttpStatusCode.Created, don.StatusCode);
        var donId = (await don.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("donTuId").GetInt64();

        // Trưởng BM duyệt: ghi QĐ điều chỉnh, GV mới.
        var duyet = await tbm.PostAsJsonAsync($"/api/v1/de-tai/don-dieu-chinh/{donId}/duyet", new
        {
            dongY = true, giangVienMoiId = gv2, soQuyetDinh = $"QD-DC-{Tag()}", ngayKy = Today(0).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        }, Json, Ct);
        Assert.Equal(HttpStatusCode.NoContent, duyet.StatusCode);

        // Giữ lịch sử: dòng GVHD cũ được đóng (không xóa), dòng mới gắn QĐ điều chỉnh.
        var lichSu = await Doc<List<PhanCongLichSuItem>>(await tbm.GetAsync($"/api/v1/svtrongdot/{hoSo}/lich-su-phan-cong", Ct));
        var gvhd = lichSu.Where(x => x.VaiTro == VaiTroPhanCong.GVHD).OrderBy(x => x.NgayBatDau).ThenBy(x => x.Id).ToList();
        Assert.Equal(2, gvhd.Count);
        Assert.Equal("GV001", gvhd[0].MaGV);
        Assert.NotNull(gvhd[0].NgayKetThuc);
        Assert.Contains("Đổi GVHD", gvhd[0].LyDoThayDoi);
        Assert.Equal("GV002", gvhd[1].MaGV);
        Assert.Null(gvhd[1].NgayKetThuc);
        Assert.StartsWith("QD-DC-", gvhd[1].SoQuyetDinh);
        Assert.Equal(TrangThaiSVTrongDot.DA_GIAO, await TrangThaiAsync(hoSo));

        // Đơn đã duyệt; không thể duyệt lại.
        await AssertLoiAsync(await tbm.PostAsJsonAsync($"/api/v1/de-tai/don-dieu-chinh/{donId}/duyet", new { dongY = true, giangVienMoiId = gv1, soQuyetDinh = "QD-X", ngayKy = Today(0).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }, Json, Ct),
            "UPM_DON_DA_XU_LY");
    }

    // ================================================================= B09 trước hạn / quá hạn

    [Fact]
    public async Task B09_Doi_ten_de_tai_duoc_trong_han_va_bi_chan_qua_han()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var sv = await Login("sv.65hn001");
        var boMon = await BoMonIdAsync("CAU");

        // Trong hạn: đợt bắt đầu hôm qua → hạn đổi đề tài còn ~4 tuần.
        var dotTrong = await TaoDotAsync(pdt, HN, Today(-1), "2026-2027");
        var hsTrong = (await ThemAsync(pdt, dotTrong.Id, "65HN001")).Single();
        var deTrong = await TaoDeTaiDaDuyetAsync(gv, tbm, dotTrong.Id, boMon);
        await ChayTrucTiepAsync(dotTrong.Id);
        await PhanCongAsync(tbm, hsTrong, deTrong.Id, await GiangVienIdAsync("GV001"), null, []);
        Assert.Equal(HttpStatusCode.OK, (await GhiQuyetDinhAsync(pdt, dotTrong.Id, hsTrong, $"QD-B-{Tag()}")).StatusCode);
        var donTrong = await sv.PostAsJsonAsync($"/api/v1/svtrongdot/{hsTrong}/don-dieu-chinh",
            new DonDieuChinhTaoRequest(LoaiDon.DoiDeTai, "Đổi tên cho sát nội dung."), Json, Ct);
        var idTrong = (await donTrong.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("donTuId").GetInt64();
        var tenMoi = "Tên đề tài đã đổi " + Tag();
        var ok = await tbm.PostAsJsonAsync($"/api/v1/de-tai/don-dieu-chinh/{idTrong}/duyet", new
        {
            dongY = true, soQuyetDinh = $"QD-B-DT-{Tag()}", ngayKy = Today(0).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), tenDeTaiMoi = tenMoi,
        }, Json, Ct);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        var ds = await Doc<List<DeTaiItem>>(await gv.GetAsync($"/api/v1/de-tai/cua-toi?dotId={dotTrong.Id}", Ct));
        Assert.Equal(tenMoi, ds.Single(x => x.Id == deTrong.Id).TenDeTai);

        // Quá hạn: đợt bắt đầu 60 ngày trước → mốc tuần 3–4 đã qua → bị chặn.
        var dotQua = await TaoDotAsync(pdt, HN, Today(-60), "2026-2027");
        var hsQua = (await ThemAsync(pdt, dotQua.Id, "65HN004")).Single();
        var deQua = await TaoDeTaiDaDuyetAsync(gv, tbm, dotQua.Id, boMon);
        await ChayTrucTiepAsync(dotQua.Id);
        await PhanCongAsync(tbm, hsQua, deQua.Id, await GiangVienIdAsync("GV001"), null, []);
        Assert.Equal(HttpStatusCode.OK, (await GhiQuyetDinhAsync(pdt, dotQua.Id, hsQua, $"QD-B-Q-{Tag()}")).StatusCode);
        var sv4 = await Login("sv.65hn004");
        var donQua = await sv4.PostAsJsonAsync($"/api/v1/svtrongdot/{hsQua}/don-dieu-chinh",
            new DonDieuChinhTaoRequest(LoaiDon.DoiDeTai, "Đổi tên."), Json, Ct);
        var idQua = (await donQua.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("donTuId").GetInt64();
        await AssertLoiAsync(await tbm.PostAsJsonAsync($"/api/v1/de-tai/don-dieu-chinh/{idQua}/duyet", new
        {
            dongY = true, soQuyetDinh = $"QD-B-Q2-{Tag()}", ngayKy = Today(0).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), tenDeTaiMoi = "Tên mới",
        }, Json, Ct), "UPM_B09_QUA_HAN_DOI_DE_TAI");
    }

    // ================================================================= Helpers

    private static string Tag() => Guid.NewGuid().ToString("N")[..8];

    private static DateOnly Today(int offset) => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).AddDays(offset);

    private async Task<HttpClient> Login(string tk)
    {
        var (client, _, _) = await factory.DangNhapAsync(tk, Ct);
        return client;
    }

    private static async Task<DotDATNItem> TaoDotAsync(HttpClient client, long coSoId, DateOnly batDau, string namHoc)
    {
        var req = new DotDATNRequest(coSoId, $"DT{Tag()}", "Đợt UC-2", namHoc, 1, batDau, 12, batDau.AddDays(84));
        var res = await client.PostAsJsonAsync("/api/v1/dot-datn", req, Json, Ct);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<DotDATNItem>(Json, Ct))!;
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

    private async Task<long> SinhVienIdAsync(string maSV) => (await IdsAsync(maSV)).SinhVienId;

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

    private async Task<int> QuotaAsync(string maGV)
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().GiangVien
            .Where(x => x.MaGV == maGV).Select(x => x.QuotaToiDa).SingleAsync(Ct);
    }

    private async Task DatQuotaAsync(string maGV, int quota)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var gv = await db.GiangVien.SingleAsync(x => x.MaGV == maGV, Ct);
        gv.QuotaToiDa = quota;
        await db.SaveChangesAsync(Ct);
    }

    private static async Task<HttpResponseMessage> TaoDeTaiRawAsync(HttpClient gv, object body) =>
        await gv.PostAsJsonAsync("/api/v1/de-tai", body, Json, Ct);

    private static async Task<DeTaiItem> TaoDeTaiAsync(HttpClient gv, long dotId, long boMonId, string ten, string loai, int? maxSV)
    {
        var body = maxSV is null
            ? (object)new { dotId, boMonId, tenDeTai = ten, loaiDeTai = loai }
            : new { dotId, boMonId, tenDeTai = ten, loaiDeTai = loai, maxSV };
        return await Doc<DeTaiItem>(await TaoDeTaiRawAsync(gv, body));
    }

    /// <summary>GV đề xuất đề tài 1 SV (lý thuyết) và Trưởng BM duyệt → sẵn sàng cho nguyện vọng / phân công.</summary>
    private static async Task<DeTaiItem> TaoDeTaiDaDuyetAsync(HttpClient gv, HttpClient tbm, long dotId, long boMonId)
    {
        var de = await TaoDeTaiAsync(gv, dotId, boMonId, "Đề tài kiểm thử " + Tag(), "LyThuyet", null);
        return await Doc<DeTaiItem>(await tbm.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/duyet", new { dongY = true }, Json, Ct));
    }

    private static async Task<HttpResponseMessage> PhanCongAsync(HttpClient tbm, long hoSoId, long deTaiId, long gvhd, long? gvdd, long[] dongHd) =>
        await tbm.PostAsJsonAsync("/api/v1/de-tai/phan-cong", new
        {
            svTrongDotId = hoSoId, gvhdId = gvhd, gvddId = gvdd, dongHDIds = dongHd, deTaiId, vuotQuotaXacNhan = false,
        }, Json, Ct);

    private static async Task<HttpResponseMessage> GhiQuyetDinhAsync(HttpClient pdt, long dotId, long hoSoId, string soQd) =>
        await pdt.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/quyet-dinh-giao", new QuyetDinhGiaoRequest(soQd, Today(0), null, [hoSoId], "Hiệu trưởng"), Json, Ct);

    private async Task ChayTrucTiepAsync(long dotId)
    {
        await using var scope = factory.CreateScope();
        var kq = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dotId, null, ct: Ct);
        Assert.Empty(kq.Loi);
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
