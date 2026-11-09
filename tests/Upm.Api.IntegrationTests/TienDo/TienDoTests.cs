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
using Upm.Application.TienDo;
using Upm.Application.Workflow;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Workflow;

namespace Upm.Api.IntegrationTests.TienDo;

/// <summary>
/// UC-3a trên SQL Server thật: mốc tự sinh, phiếu riêng cho từng SV (nhóm), báo cáo + cam kết AI (C07), nhận xét,
/// job nhắc việc không gửi trùng, C01/C02 gửi SV+GVHD+BM, bỏ 2 mốc chỉ gợi ý RUT, gia hạn (C05), xin dừng → RUT (C04).
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class TienDoTests(UpmApiFactory factory)
{
    private const long HN = 1;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ================================================================= Mốc tiến độ

    [Fact]
    public async Task Dot_moi_sinh_3_moc_voi_han_mac_dinh_theo_tuan()
    {
        var pdt = await Login("pdt.hn");
        var batDau = Today(-1);
        var dot = await TaoDotAsync(pdt, batDau);

        var mocs = await Doc<List<MocTienDoItem>>(await pdt.GetAsync($"/api/v1/tien-do/moc?dotId={dot.Id}", Ct));

        Assert.Equal(3, mocs.Count);
        Assert.Equal(batDau.AddDays(27), mocs.Single(x => x.MaMoc == MaMocTienDo.M1).NgayHan);
        Assert.Equal(batDau.AddDays(62), mocs.Single(x => x.MaMoc == MaMocTienDo.M2).NgayHan);
        Assert.Equal(batDau.AddDays(84), mocs.Single(x => x.MaMoc == MaMocTienDo.M3).NgayHan);
        Assert.Equal(60m, mocs.Single(x => x.MaMoc == MaMocTienDo.M2).YeuCauToiThieuPct);
    }

    // ================================================================= Phiếu giao nhiệm vụ cho nhóm

    [Fact]
    public async Task Nhom_2_SV_co_phieu_rieng_va_PDF_dung_dinh_dang()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var hsA = (await ThemAsync(pdt, dot.Id, "65HN001")).Single();
        var hsB = (await ThemAsync(pdt, dot.Id, "65HN005")).Single();
        var deTai = await TaoDeTaiNhomAsync(gv, tbm, dot.Id, boMon);
        var gv1 = await GiangVienIdAsync("GV001");
        Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hsA, deTai.Id, gv1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hsB, deTai.Id, gv1)).StatusCode);

        var phieuA = await Doc<PhieuGiaoNhiemVuItem>(await gv.PutAsJsonAsync($"/api/v1/svtrongdot/{hsA}/phieu-giao-nhiem-vu",
            new PhieuGiaoNhiemVuRequest("Thiết kế cầu thép phần A", "Bản vẽ và thuyết minh", Today(0)), Json, Ct));
        var phieuB = await Doc<PhieuGiaoNhiemVuItem>(await gv.PutAsJsonAsync($"/api/v1/svtrongdot/{hsB}/phieu-giao-nhiem-vu",
            new PhieuGiaoNhiemVuRequest("Khảo sát đường bộ phần B", null, Today(0)), Json, Ct));

        // Mỗi SV một phiếu riêng, nội dung không lẫn.
        Assert.NotEqual(phieuA.Id, phieuB.Id);
        Assert.Equal("Thiết kế cầu thép phần A", (await Doc<PhieuGiaoNhiemVuItem>(await gv.GetAsync($"/api/v1/svtrongdot/{hsA}/phieu-giao-nhiem-vu", Ct))).NoiDungNhiemVu);
        Assert.Equal("Khảo sát đường bộ phần B", (await Doc<PhieuGiaoNhiemVuItem>(await gv.GetAsync($"/api/v1/svtrongdot/{hsB}/phieu-giao-nhiem-vu", Ct))).NoiDungNhiemVu);

        // GV không phải GVHD của hồ sơ không lập được phiếu.
        var gv3 = await Login("gv003");
        await AssertLoiAsync(await gv3.PutAsJsonAsync($"/api/v1/svtrongdot/{hsA}/phieu-giao-nhiem-vu",
            new PhieuGiaoNhiemVuRequest("Lập trái phép", null, Today(0)), Json, Ct), "UPM_PERMISSION_DENIED");

        var pdf = await gv.GetAsync($"/api/v1/svtrongdot/{hsA}/phieu-giao-nhiem-vu/pdf", Ct);
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync(Ct);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    // ================================================================= Nộp báo cáo, C07, C02, nhận xét

    [Fact]
    public async Task Nop_bao_cao_can_cam_ket_AI_C07_va_duoi_yeu_cau_la_CanhBao_C02()
    {
        var pdt = await Login("pdt.hn");
        var dot = await TaoDotAsync(pdt, Today(-1));
        var (hoSo, gvId, _) = await DuaDenDangThucHienAsync(pdt, dot.Id, "65HN001");
        var sv = await Login("sv.65hn001");
        var mocs = await Doc<List<MocTienDoItem>>(await pdt.GetAsync($"/api/v1/tien-do/moc?dotId={dot.Id}", Ct));
        var m2 = mocs.Single(x => x.MaMoc == MaMocTienDo.M2).Id;

        // C07: thiếu cam kết AI → bị chặn.
        using (var thieu = new MultipartFormDataContent())
        {
            thieu.Add(new StringContent(m2.ToString(System.Globalization.CultureInfo.InvariantCulture)), "mocTienDoId");
            thieu.Add(new StringContent("40"), "tyLeHoanThanhPct");
            thieu.Add(new StringContent("false"), "camKetAI");
            thieu.Add(new ByteArrayContent("%PDF-1.4 test"u8.ToArray()), "file", "bao-cao.pdf");
            var res = await sv.PostAsync($"/api/v1/svtrongdot/{hoSo}/bao-cao", thieu, Ct);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
            Assert.Contains("UPM_C07_CAM_KET_BAT_BUOC", await res.Content.ReadAsStringAsync(Ct));
        }

        // File không đúng định dạng bị chặn.
        using (var exe = new MultipartFormDataContent())
        {
            exe.Add(new StringContent(m2.ToString(System.Globalization.CultureInfo.InvariantCulture)), "mocTienDoId");
            exe.Add(new StringContent("40"), "tyLeHoanThanhPct");
            exe.Add(new StringContent("true"), "camKetAI");
            exe.Add(new ByteArrayContent(new byte[] { 0x4D, 0x5A }), "file", "virus.exe");
            var res = await sv.PostAsync($"/api/v1/svtrongdot/{hoSo}/bao-cao", exe, Ct);
            Assert.Contains("UPM_TEP_DINH_DANG", await res.Content.ReadAsStringAsync(Ct));
        }

        // Hợp lệ nhưng 40% < 60% yêu cầu M2 → CanhBao (C02).
        using (var hop = new MultipartFormDataContent())
        {
            hop.Add(new StringContent(m2.ToString(System.Globalization.CultureInfo.InvariantCulture)), "mocTienDoId");
            hop.Add(new StringContent("40"), "tyLeHoanThanhPct");
            hop.Add(new StringContent("true"), "camKetAI");
            hop.Add(new ByteArrayContent("%PDF-1.4 bao cao"u8.ToArray()), "file", "bao-cao.pdf");
            var bao = await Doc<BaoCaoTienDoItem>(await sv.PostAsync($"/api/v1/svtrongdot/{hoSo}/bao-cao", hop, Ct));
            Assert.Equal(KetQuaTienDo.CanhBao, bao.KetQua);
            Assert.True(bao.CamKetAI);
            Assert.NotNull(bao.FileId);

            // Lưu hash SHA-256 của file (không để trống).
            await using var scope = factory.CreateScope();
            var tep = await scope.ServiceProvider.GetRequiredService<UpmDbContext>().TepDinhKem
                .SingleAsync(x => x.Id == bao.FileId!.Value, Ct);
            Assert.Equal(64, tep.HashSHA256.Length);

            // GVHD nhận xét và chấm điểm.
            var gv = await Login("gv001");
            var nhanXet = await Doc<BaoCaoTienDoItem>(await gv.PostAsJsonAsync($"/api/v1/bao-cao-tien-do/{bao.Id}/nhan-xet",
                new NhanXetBaoCaoRequest(7.5m, "Tiến độ chậm, cần bổ sung."), Json, Ct));
            Assert.Equal(7.5m, nhanXet.DiemTienDo);
        }

        var cuaToi = await Doc<List<HoSoTienDoItem>>(await sv.GetAsync($"/api/v1/tien-do/cua-toi?dotId={dot.Id}", Ct));
        var moc2 = Assert.Single(cuaToi).CacMoc.Single(x => x.MaMoc == MaMocTienDo.M2);
        Assert.Equal(KetQuaTienDo.CanhBao, moc2.TrangThai);
        Assert.True(moc2.BaoCaoMoiNhat!.CamKetAI);
        Assert.Equal(gvId, cuaToi[0].GVHDId);
    }

    // ================================================================= Job nhắc việc: không gửi trùng

    [Fact]
    public async Task Job_nhac_viec_khong_gui_trung_va_chuyen_T04()
    {
        var pdt = await Login("pdt.hn");
        // Đợt bắt đầu 25 ngày trước → mốc M1 còn 2 ngày: nhắc trước hạn.
        var dot = await TaoDotAsync(pdt, Today(-25));
        var (hoSo, _, _) = await DuaDenDangThucHienAsync(pdt, dot.Id, "65HN001", chayNhacViec: false);
        var truoc = factory.Email.DaGui.Count;

        var lan1 = await ChayNhacViecAsync();
        var guiMoi = factory.Email.DaGui.Skip(truoc).ToArray();

        Assert.Contains(guiMoi, x => x.To == "65hn001@st.utc.edu.vn" && x.Subject.StartsWith("Sắp đến hạn", StringComparison.Ordinal));
        Assert.True(lan1.SoEmail > 0);

        // Chạy lại trong cùng ngày: không gửi thêm lần nào.
        var lan2 = await ChayNhacViecAsync();
        Assert.Equal(0, lan2.SoEmail);
        Assert.Equal(0, lan2.SoThongBao);
        Assert.Equal(guiMoi.Length, factory.Email.DaGui.Skip(truoc).Count());

        // Đúng một bản ghi nhắc cho hồ sơ + loại + hạn.
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        Assert.Equal(1, await db.LichNhacViec.CountAsync(x => x.SVTrongDotId == hoSo && x.LoaiNhac == LoaiNhac.MocTienDo, Ct));
    }

    // ================================================================= C01 quá hạn + bỏ 2 mốc → gợi ý RUT (không tự động)

    [Fact]
    public async Task Bo_hai_moc_chi_goi_y_RUT_cho_Bo_mon_khong_tu_dong_chuyen()
    {
        var pdt = await Login("pdt.hn");
        var tbm = await Login("tbm.cau.hn");
        // Đợt bắt đầu 70 ngày trước: M1 (cuối tuần 4) và M2 (cuối tuần 9) đều đã quá hạn, không nộp báo cáo.
        var dot = await TaoDotAsync(pdt, Today(-70));
        var (hoSo, _, _) = await DuaDenDangThucHienAsync(pdt, dot.Id, "65HN004");

        var cham = await Doc<List<HoSoTienDoItem>>(await tbm.GetAsync($"/api/v1/tien-do/bo-mon?dotId={dot.Id}", Ct));
        var h = cham.Single(x => x.SVTrongDotId == hoSo);
        Assert.True(h.CanNhacRut);
        Assert.All(h.CacMoc.Where(m => m.MaMoc != MaMocTienDo.M3), m => Assert.Equal(KetQuaTienDo.KhongNop, m.TrangThai));

        // Không tự động chuyển trạng thái (chỉ gợi ý).
        await using var scope = factory.CreateScope();
        Assert.Equal(TrangThaiSVTrongDot.DANG_THUC_HIEN,
            await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot.Where(x => x.Id == hoSo).Select(x => x.TrangThai).SingleAsync(Ct));

        // Job cảnh báo C01 gửi cho SV và GVHD (mã trong tiêu đề).
        await ChayNhacViecAsync();
        Assert.Contains(factory.Email.DaGui, x => x.To == "65hn004@st.utc.edu.vn" && x.Subject.StartsWith("[C01]", StringComparison.Ordinal));
    }

    // ================================================================= C05 gia hạn, C04 xin dừng → RUT

    [Fact]
    public async Task C05_gia_han_phai_sau_han_cu_va_C04_xin_dung_duyet_roi_chuyen_RUT()
    {
        var pdt = await Login("pdt.hn");
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var dot = await TaoDotAsync(pdt, Today(-25));
        var (hoSo, _, _) = await DuaDenDangThucHienAsync(pdt, dot.Id, "65HN001");
        var sv = await Login("sv.65hn001");
        var mocs = await Doc<List<MocTienDoItem>>(await pdt.GetAsync($"/api/v1/tien-do/moc?dotId={dot.Id}", Ct));
        var hanM1 = mocs.Single(x => x.MaMoc == MaMocTienDo.M1).NgayHan;

        var don = await Doc<DonTienDoItem>(await sv.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo}/don-tien-do",
            new DonTienDoTaoRequest(LoaiDon.GiaHan, "Cần thêm thời gian thu thập số liệu."), Json, Ct));

        // Hạn mới không sau hạn cũ → bị chặn (C05).
        await AssertLoiAsync(await gv.PostAsJsonAsync($"/api/v1/tien-do/don/{don.DonTuId}/duyet",
            new DuyetDonTienDoRequest(true, hanM1, null), Json, Ct), "UPM_C05_HAN_MOI_KHONG_HOP_LE");

        var duyetGiaHan = await gv.PostAsJsonAsync($"/api/v1/tien-do/don/{don.DonTuId}/duyet",
            new DuyetDonTienDoRequest(true, hanM1.AddDays(14), null), Json, Ct);
        Assert.True(duyetGiaHan.StatusCode == HttpStatusCode.NoContent, await duyetGiaHan.Content.ReadAsStringAsync(Ct));
        var sauGiaHan = await Doc<List<HoSoTienDoItem>>(await sv.GetAsync($"/api/v1/tien-do/cua-toi?dotId={dot.Id}", Ct));
        var m1 = sauGiaHan.Single().CacMoc.Single(x => x.MaMoc == MaMocTienDo.M1);
        Assert.Equal(hanM1.AddDays(14), m1.HanHieuLuc);
        Assert.True(m1.DaGiaHan);

        // C04: SV xin dừng; Trưởng BM duyệt → hồ sơ chuyển RUT (T06) với lý do.
        var (hoSo2, _, _) = await DuaDenDangThucHienAsync(pdt, (await TaoDotAsync(pdt, Today(-1))).Id, "65HN004");
        var sv4 = await Login("sv.65hn004");
        var xinDung = await Doc<DonTienDoItem>(await sv4.PostAsJsonAsync($"/api/v1/svtrongdot/{hoSo2}/don-tien-do",
            new DonTienDoTaoRequest(LoaiDon.XinDung, "Chuyển ngành, xin dừng ĐATN."), Json, Ct));
        var duyetDung = await tbm.PostAsJsonAsync($"/api/v1/tien-do/don/{xinDung.DonTuId}/duyet",
            new DuyetDonTienDoRequest(true, null, "Đồng ý cho dừng theo đơn."), Json, Ct);
        Assert.True(duyetDung.StatusCode == HttpStatusCode.NoContent, await duyetDung.Content.ReadAsStringAsync(Ct));

        await using var scope = factory.CreateScope();
        Assert.Equal(TrangThaiSVTrongDot.RUT,
            await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot.Where(x => x.Id == hoSo2).Select(x => x.TrangThai).SingleAsync(Ct));
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
        var req = new DotDATNRequest(HN, $"TD{Guid.NewGuid():N}"[..14], "Đợt UC-3a", "2026-2027", 1, batDau, 12, batDau.AddDays(84));
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

    /// <summary>Đề tài nhóm 2 SV (chế tạo, MaxSV = 2) đã được Bộ môn duyệt.</summary>
    private static async Task<DeTaiItem> TaoDeTaiNhomAsync(HttpClient gv, HttpClient tbm, long dotId, long boMonId)
    {
        var de = await Doc<DeTaiItem>(await gv.PostAsJsonAsync("/api/v1/de-tai", new
        {
            dotId, boMonId, tenDeTai = $"Đề tài nhóm {Guid.NewGuid():N}"[..24], loaiDeTai = "CheTao", maxSV = 2,
        }, Json, Ct));
        return await Doc<DeTaiItem>(await tbm.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/duyet", new { dongY = true }, Json, Ct));
    }

    private static async Task<HttpResponseMessage> PhanCongAsync(HttpClient tbm, long hoSoId, long deTaiId, long gvhd) =>
        await tbm.PostAsJsonAsync("/api/v1/de-tai/phan-cong", new
        {
            svTrongDotId = hoSoId, gvhdId = gvhd, gvddId = (long?)null, dongHDIds = Array.Empty<long>(), deTaiId, vuotQuotaXacNhan = false,
        }, Json, Ct);

    /// <summary>
    /// Đưa một SV tới DANG_THUC_HIEN: đề tài đã duyệt → GVHD (GV001) → chạy xét → QĐ giao (T03) → job nhắc việc (T04).
    /// </summary>
    private async Task<(long HoSoId, long GvId, long DeTaiId)> DuaDenDangThucHienAsync(HttpClient pdt, long dotId, string maSV, bool chayNhacViec = true)
    {
        var hoSo = (await ThemAsync(pdt, dotId, maSV)).Single();
        var gv = await Login("gv001");
        var tbm = await Login("tbm.cau.hn");
        var boMon = await BoMonIdAsync("CAU");
        var de = await Doc<DeTaiItem>(await gv.PostAsJsonAsync("/api/v1/de-tai", new
        {
            dotId, boMonId = boMon, tenDeTai = $"Đề tài TD {Guid.NewGuid():N}"[..24], loaiDeTai = "LyThuyet",
        }, Json, Ct));
        await tbm.PostAsJsonAsync($"/api/v1/de-tai/{de.Id}/duyet", new { dongY = true }, Json, Ct);
        var gvId = await GiangVienIdAsync("GV001");
        Assert.Equal(HttpStatusCode.OK, (await PhanCongAsync(tbm, hoSo, de.Id, gvId)).StatusCode);

        await using (var scope = factory.CreateScope())
        {
            var kq = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dotId, null, ct: Ct);
            Assert.Empty(kq.Loi);
        }
        var qd = await pdt.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/quyet-dinh-giao",
            new QuyetDinhGiaoRequest($"QD-TD-{Guid.NewGuid():N}"[..20], Today(0), null, [hoSo], "Hiệu trưởng"), Json, Ct);
        Assert.Equal(HttpStatusCode.OK, qd.StatusCode);
        if (chayNhacViec) await ChayNhacViecAsync();
        return (hoSo, gvId, de.Id);
    }

    private async Task<KetQuaNhacViec> ChayNhacViecAsync()
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<INhacViecService>().ChayAsync(factory.Clock.Today, Ct);
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
