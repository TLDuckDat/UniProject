using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upm.Api.IntegrationTests.Infrastructure;
using Upm.Application.Common;
using Upm.Application.Workflow;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.Persistence.Seed;

namespace Upm.Api.IntegrationTests.XetGiao;

/// <summary>
/// UC-1 · Đợt & xét điều kiện giao trên SQL Server thật: dữ liệu seed (DevDataSeeder.Scenarios) cho đúng kết quả A01–A08,
/// A10–A14; job chạy lại không nhân đôi snapshot; K64 + K67 cùng đợt áp RuleSet riêng (A15); A18–A21; QĐ giao (T03).
/// </summary>
[Collection(SharedApiFixture.Name)]
public sealed class XetGiaoDotTests(UpmApiFactory factory)
{
    private const long HN = 1;
    private const long HCM = 2;

    /// <summary>Ngày xét cố định: trong khoảng hiệu lực kỷ luật mẫu (01/09/2026–30/06/2027) của seeder.</summary>
    private static readonly DateOnly NgayXet = new(2026, 11, 9);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ======================================================================= Kịch bản seed

    [Fact]
    public async Task Seed_HN_cho_dung_A01_A08_A10_A13_A14_va_job_chay_lai_khong_nhan_doi_snapshot()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var dot = await TaoDotAsync(client, HN, new DateOnly(2026, 9, 7));
        string[] maSV = ["65HN001", "65HN002", "65HN003", "65HN004", "65HN005", "65HN006", "65HN007", "65HN008",
            "65HN009", "65HN010", "65HN011", "65HN012", "66HN018"];
        await ThemAsync(client, dot.Id, maSV);

        // Lần 1 qua Hangfire (đúng đường đi của nút "Chạy xét" + thanh tiến độ).
        var job = await ChayQuaApiAsync(client, dot.Id);
        Assert.Equal("Succeeded", job.TrangThai);
        Assert.Equal(maSV.Length, job.TongSo);
        Assert.Equal(maSV.Length, job.DaXuLy);
        Assert.Empty(job.Loi);
        var ganNhat = await client.GetFromJsonAsync<XetGiaoJobItem>($"/api/v1/dot-datn/{dot.Id}/xet-giao/jobs/gan-nhat", Json, Ct);
        Assert.Equal(job.JobId, ganNhat!.JobId);

        var hoSo = await HoSoTheoMaAsync(client, dot.Id);
        async Task<XetGiaoSnapshotItem> Snap(string ma) => Assert.Single(await SnapshotAsync(client, hoSo[ma].Id));

        // Đủ điều kiện, không nợ: ở lại CHO_XET chờ đề tài + QĐ (T03 chưa chạy ở Phần 7).
        var sv001 = await Snap("65HN001");
        Assert.Equal("DU_DK", sv001.KetQua);
        Assert.Equal(0, sv001.TCChuaTichLuy);
        Assert.Empty(sv001.Co);
        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, hoSo["65HN001"].TrangThai);

        // A01: nợ 6 ≤ 10 → giao + cờ NO_TIN_CHI.
        var a01 = await Snap("65HN002");
        Assert.Equal(("DU_DK", 6, "A01"), (a01.KetQua, a01.TCChuaTichLuy, a01.MaNgoaiLe));
        Assert.Contains("NO_TIN_CHI", a01.Co);
        Assert.True(hoSo["65HN002"].CoNoTinChi);
        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, hoSo["65HN002"].TrangThai);

        // A02 + A13: nợ 14 > 10, khối Kỹ thuật → KHONG_DU_DK (T01), liệt kê từng học phần nợ.
        var a02 = await Snap("65HN003");
        Assert.Equal(("KHONG_DU_DK", 14, "A13"), (a02.KetQua, a02.TCChuaTichLuy, a02.MaNgoaiLe));
        Assert.False(a02.G3TinChi);
        Assert.Equal(["KTH104", "KTH106", "KTH107", "KTH109"],
            a02.ChiTiets.Where(c => !c.BiLoaiTru).Select(c => c.MaHP).Order().ToArray());
        Assert.Equal(14, a02.ChiTiets.Where(c => !c.BiLoaiTru).Sum(c => c.SoTC));
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK, hoSo["65HN003"].TrangThai);

        // A03: đúng 10 TC → vẫn được giao (≤).
        var a03 = await Snap("65HN004");
        Assert.Equal(("DU_DK", 10, "A03"), (a03.KetQua, a03.TCChuaTichLuy, a03.MaNgoaiLe));

        // A04: môn đang học chưa có điểm = chưa tích lũy (3 + 2) + nợ 2 = 7.
        var a04 = await Snap("65HN005");
        Assert.Equal(("DU_DK", 7), (a04.KetQua, a04.TCChuaTichLuy));
        Assert.Equal(5, a04.ChiTiets.Where(c => c.LoaiNo == LoaiNo.DangHoc).Sum(c => c.SoTC));

        // A05: nợ GDQP, GDTC không cộng → chỉ 3 TC.
        var a05 = await Snap("65HN006");
        Assert.Equal(("DU_DK", 3), (a05.KetQua, a05.TCChuaTichLuy));
        Assert.DoesNotContain(a05.ChiTiets, c => !c.BiLoaiTru && c.LoaiHP is LoaiHocPhan.GDQP or LoaiHocPhan.GDTC);

        // A06: đúng tiến độ, chưa học TTTN → TTTN không cộng, cờ CHUA_QUA_TTTN.
        var a06 = await Snap("65HN007");
        Assert.Equal(("DU_DK", 0, "A06"), (a06.KetQua, a06.TCChuaTichLuy, a06.MaNgoaiLe));
        Assert.Equal(["CHUA_QUA_TTTN"], a06.Co);
        Assert.True(hoSo["65HN007"].ChuaQuaTTTN);
        Assert.False(hoSo["65HN007"].CoNoTinChi);

        // A07: trễ tiến độ, chưa có điểm TTTN → TC TTTN bị cộng: 5 + 6 = 11 > 10.
        var a07 = await Snap("65HN008");
        Assert.Equal(("KHONG_DU_DK", 11), (a07.KetQua, a07.TCChuaTichLuy));
        var tttn = Assert.Single(a07.ChiTiets, c => c.LoaiNo == LoaiNo.TTTNCong);
        Assert.False(tttn.BiLoaiTru);
        Assert.Equal(6, tttn.SoTC);

        // A08: điểm I và X = chưa tích lũy.
        var a08 = await Snap("65HN009");
        Assert.Equal(("DU_DK", 7), (a08.KetQua, a08.TCChuaTichLuy));
        Assert.Contains(a08.ChiTiets, c => c.LoaiNo == LoaiNo.DiemI && c.MaHP == "KTH106");
        Assert.Contains(a08.ChiTiets, c => c.LoaiNo == LoaiNo.DiemX && c.MaHP == "KTH107");

        // A09 (không bắt buộc ở Phần 7, kiểm tra kèm): R = đã tích lũy.
        Assert.Equal(("DU_DK", 0), ((await Snap("65HN010")).KetQua, (await Snap("65HN010")).TCChuaTichLuy));

        // A14: khối Kinh tế có HP thay thế, nợ 12 > 10 → KHONG_DU_DK_CHUYEN_THAY_THE (T02).
        var a14 = await Snap("65HN011");
        Assert.Equal(("KHONG_DU_DK_CHUYEN_THAY_THE", "A14"), (a14.KetQua, a14.MaNgoaiLe));
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE, hoSo["65HN011"].TrangThai);

        // A10: đang bị đình chỉ học tập → KHONG_DU_DK dù TC đủ.
        var a10 = await Snap("65HN012");
        Assert.Equal(("KHONG_DU_DK", "A10", false), (a10.KetQua, a10.MaNgoaiLe, a10.G1KyLuat));
        Assert.True(a10.G3TinChi);

        // A16/O2: K66 tạm áp UTC-2021.
        Assert.Equal("UTC-2021", (await Snap("66HN018")).MaRuleSet);

        // Chạy lại (cả runner trực tiếp lẫn job Hangfire): không thêm snapshot, không thêm lịch sử trạng thái.
        var (soSnapshot, soLichSu) = await DemAsync(dot.Id);
        await using (var scope = factory.CreateScope())
        {
            var lanHai = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dot.Id, null, ct: Ct);
            Assert.Equal(0, lanHai.TongSo);
        }
        var job2 = await ChayQuaApiAsync(client, dot.Id);
        Assert.Equal(("Succeeded", 0), (job2.TrangThai, job2.TongSo));
        Assert.Equal((soSnapshot, soLichSu), await DemAsync(dot.Id));
        Assert.Equal(maSV.Length, soSnapshot);

        // Bộ lọc tab SV: theo trạng thái + theo kết quả xét + thống kê.
        var khongDu = await client.GetFromJsonAsync<DanhSachHoSoKetQua>(
            $"/api/v1/dot-datn/{dot.Id}/sinh-vien?trangThai=KHONG_DU_DK", Json, Ct);
        Assert.Equal(["65HN003", "65HN008", "65HN012"], khongDu!.Items.Select(x => x.MaSV).ToArray());
        Assert.Equal(3, khongDu.ThongKe.Single(x => x.TrangThai == TrangThaiSVTrongDot.KHONG_DU_DK).SoLuong);
        var coNo = await client.GetFromJsonAsync<DanhSachHoSoKetQua>(
            $"/api/v1/dot-datn/{dot.Id}/sinh-vien?coNoTinChi=true&ketQuaXet=DU_DK", Json, Ct);
        Assert.Equal(["65HN002", "65HN004", "65HN005", "65HN006", "65HN009"], coNo!.Items.Select(x => x.MaSV).ToArray());
    }

    [Fact]
    public async Task Seed_HCM_A10_A11_A12_va_A15_K64_K67_cung_dot_ap_dung_RuleSet_theo_khoa()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hcm", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var dot = await TaoDotAsync(client, HCM, new DateOnly(2026, 9, 7));

        // A11: nghỉ học tạm thời → không đưa vào đợt (thêm trực tiếp bị chặn; thêm theo lọc thì bỏ qua + báo lý do).
        var ids = await IdsAsync(["65HCM014"]);
        var res = await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/sinh-vien",
            new[] { new HoSoTrongDotRequest(ids[0].SinhVienId, ids[0].CTDTId) }, Json, Ct);
        await AssertLoiAsync(res, "UPM_A11_NGHI_TAM_THOI");
        var theoLoc = await (await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/sinh-vien/theo-loc",
            new ThemHoSoTheoLocRequest(65, null, null), Json, Ct)).Content.ReadFromJsonAsync<ThemHoSoKetQua>(Json, Ct);
        var boQua = Assert.Single(theoLoc!.BoQua, x => x.MaSV == "65HCM014");
        Assert.StartsWith("A11", boQua.LyDo, StringComparison.Ordinal);
        var ungVien = await client.GetFromJsonAsync<List<SinhVienUngVienItem>>(
            $"/api/v1/dot-datn/{dot.Id}/sinh-vien-ung-vien?q=65HCM014", Json, Ct);
        Assert.False(Assert.Single(ungVien!).CoTheThem);

        await ThemAsync(client, dot.Id, ["55HCM015", "64HCM017", "67HCM019"]);
        await using (var scope = factory.CreateScope())
        {
            var ketQua = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dot.Id, null, ct: Ct);
            Assert.Empty(ketQua.Loi);
        }
        var hoSo = await HoSoTheoMaAsync(client, dot.Id);
        Assert.DoesNotContain("65HCM014", hoSo.Keys);
        async Task<XetGiaoSnapshotItem> Snap(string ma) => Assert.Single(await SnapshotAsync(client, hoSo[ma].Id));

        // A10: truy cứu trách nhiệm hình sự.
        Assert.Equal(("KHONG_DU_DK", "A10"), ((await Snap("65HCM013")).KetQua, (await Snap("65HCM013")).MaNgoaiLe));

        // A12: K55 vượt thời gian học tối đa (2 × chuẩn).
        var a12 = await Snap("55HCM015");
        Assert.Equal(("KHONG_DU_DK", "A12", false), (a12.KetQua, a12.MaNgoaiLe, a12.G2TrangThaiHocTap));
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK, hoSo["55HCM015"].TrangThai);

        // A15: K64 và K67 CÙNG đợt → RuleSet theo khóa, RuleVersion lưu theo từng snapshot.
        var k64 = await Snap("64HCM017");
        var k67 = await Snap("67HCM019");
        Assert.Equal(("UTC-2021", "UTC-2021"), (k64.MaRuleSet, k64.RuleVersion));
        Assert.Equal(("UTC-2026", "UTC-2026"), (k67.MaRuleSet, k67.RuleVersion));
        Assert.Equal(hoSo["64HCM017"].RuleVersion, k64.RuleVersion);
    }

    // ======================================================================= A19, QĐ giao (T03)

    [Fact]
    public async Task A19_xet_lai_giu_snapshot_cu_va_QD_giao_khoa_du_lieu()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var dot = await TaoDotAsync(client, HN, new DateOnly(2026, 9, 7));
        string[] no = ["KTH104", "KTH106", "KTH107", "KTH109"]; // 14 TC
        var sinhVienId = await TaoSinhVienTuMauAsync("65HN001", no);
        var hoSoId = (await ThemIdsAsync(client, dot.Id, sinhVienId)).Single();
        await ChayTrucTiepAsync(dot.Id);
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK, await TrangThaiAsync(hoSoId));

        // Điểm thay đổi (cải thiện / phúc khảo) → xét lại → đủ ĐK → T01R về CHO_XET; snapshot cũ còn nguyên.
        await DoiDiemAsync(sinhVienId, no, TrangThaiDiem.Dat);
        var xetLai = await client.PostAsync($"/api/v1/svtrongdot/{hoSoId}/xet-lai", null, Ct);
        Assert.Equal(HttpStatusCode.OK, xetLai.StatusCode);
        var moi = (await xetLai.Content.ReadFromJsonAsync<XetGiaoSnapshotItem>(Json, Ct))!;
        Assert.Equal((LoaiXet.XetLai, "DU_DK", 0), (moi.LoaiXet, moi.KetQua, moi.TCChuaTichLuy));
        Assert.Equal("pdt.hn", moi.NguoiChay);
        var tatCa = await SnapshotAsync(client, hoSoId);
        Assert.Equal([(LoaiXet.Giao, "KHONG_DU_DK", 14), (LoaiXet.XetLai, "DU_DK", 0)],
            tatCa.Select(x => (x.LoaiXet, x.KetQua, x.TCChuaTichLuy)).ToArray());
        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, await TrangThaiAsync(hoSoId));

        // QĐ giao: chặn khi chưa có đề tài (Phần 8 mới gắn đề tài).
        var qd = new QuyetDinhGiaoRequest($"QD-{Guid.NewGuid():N}"[..20], new DateOnly(2026, 9, 1), null, [hoSoId], "Hiệu trưởng");
        await AssertLoiAsync(await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/quyet-dinh-giao", qd, Json, Ct),
            "UPM_QUYET_DINH_CHUA_CO_DE_TAI");
        Assert.Equal(0, await DemQuyetDinhAsync(qd.SoQuyetDinh));

        await GanDeTaiAsync(dot.Id, hoSoId);
        var ghiNhan = await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/quyet-dinh-giao", qd, Json, Ct);
        Assert.Equal(HttpStatusCode.OK, ghiNhan.StatusCode);
        var quyetDinh = (await ghiNhan.Content.ReadFromJsonAsync<QuyetDinhGiaoItem>(Json, Ct))!;
        Assert.True(quyetDinh.DaKhoaDuLieu);
        Assert.Equal(TrangThaiSVTrongDot.DA_GIAO, await TrangThaiAsync(hoSoId));
        var lichSu = (await client.GetFromJsonAsync<List<WorkflowHistoryItem>>($"/api/v1/svtrongdot/{hoSoId}/lich-su-trang-thai", Json, Ct))!;
        Assert.Equal([TrangThaiSVTrongDot.CHO_XET, TrangThaiSVTrongDot.KHONG_DU_DK, TrangThaiSVTrongDot.CHO_XET, TrangThaiSVTrongDot.DA_GIAO],
            lichSu.OrderBy(x => x.Id).Select(x => x.DenTrangThai).ToArray());
        Assert.Equal(quyetDinh.Id, lichSu.OrderBy(x => x.Id).Last().QuyetDinhId);
        Assert.Contains(await client.GetFromJsonAsync<List<QuyetDinhGiaoItem>>($"/api/v1/dot-datn/{dot.Id}/quyet-dinh-giao", Json, Ct) ?? [],
            x => x.Id == quyetDinh.Id && x.SVTrongDotIds.SequenceEqual([hoSoId]));

        // Đã có QĐ: không xét lại (không tự lật), không ghi trùng số QĐ.
        await AssertLoiAsync(await client.PostAsync($"/api/v1/svtrongdot/{hoSoId}/xet-lai", null, Ct), "UPM_A19_DA_CO_QUYET_DINH_GIAO");
        Assert.Equal(2, (await SnapshotAsync(client, hoSoId)).Count);
        await AssertLoiAsync(await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/quyet-dinh-giao", qd, Json, Ct),
            "UPM_QUYET_DINH_TRUNG_SO");
    }

    [Fact]
    public async Task QD_giao_chan_ho_so_khong_du_dieu_kien()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var dot = await TaoDotAsync(client, HN, new DateOnly(2026, 9, 7));
        var sinhVienId = await TaoSinhVienTuMauAsync("65HN001", ["KTH104", "KTH106", "KTH107", "KTH109"]);
        var hoSoId = (await ThemIdsAsync(client, dot.Id, sinhVienId)).Single();
        await ChayTrucTiepAsync(dot.Id);
        await GanDeTaiAsync(dot.Id, hoSoId);

        var qd = new QuyetDinhGiaoRequest($"QD-{Guid.NewGuid():N}"[..20], new DateOnly(2026, 9, 1), null, [hoSoId]);
        await AssertLoiAsync(await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/quyet-dinh-giao", qd, Json, Ct),
            "UPM_QUYET_DINH_TRANG_THAI_KHONG_HOP_LE");
        Assert.Equal(0, await DemQuyetDinhAsync(qd.SoQuyetDinh));
    }

    // ======================================================================= A20 khiếu nại

    [Fact]
    public async Task A20_SV_khieu_nai_Phong_DT_tu_choi_phai_co_ly_do_chap_nhan_thi_xet_lai()
    {
        var (pdt, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        var (sv, _, _) = await factory.DangNhapAsync("sv.65hn001", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var dot = await TaoDotAsync(pdt, HN, new DateOnly(2026, 9, 7));
        await ThemAsync(pdt, dot.Id, ["65HN001"]);
        await ChayTrucTiepAsync(dot.Id);
        var hoSoId = (await HoSoTheoMaAsync(pdt, dot.Id))["65HN001"].Id;

        var cuaToi = await KetQuaCuaToiAsync(sv, dot.MaDot);
        Assert.True(cuaToi.CoTheKhieuNai);
        Assert.Equal("DU_DK", cuaToi.KetQuaMoiNhat!.KetQua);

        // SV không gọi được API của Phòng ĐT; không khiếu nại hộ hồ sơ người khác.
        Assert.Equal(HttpStatusCode.Forbidden, (await sv.PostAsync($"/api/v1/svtrongdot/{hoSoId}/xet-lai", null, Ct)).StatusCode);

        var gui = await sv.PostAsJsonAsync($"/api/v1/ho-so-cua-toi/{hoSoId}/khieu-nai-xet-giao",
            new DonKhieuNaiRequest("Em đã có điểm phúc khảo KTH104, đề nghị xét lại."), Json, Ct);
        Assert.Equal(HttpStatusCode.Created, gui.StatusCode);
        var donId = (await gui.Content.ReadFromJsonAsync<DonKhieuNaiKetQua>(Json, Ct))!.DonTuId;
        await AssertLoiAsync(await sv.PostAsJsonAsync($"/api/v1/ho-so-cua-toi/{hoSoId}/khieu-nai-xet-giao",
            new DonKhieuNaiRequest("Gửi lần hai"), Json, Ct), "UPM_KHIEU_NAI_DANG_CHO_XU_LY");

        var dsChoXuLy = await pdt.GetFromJsonAsync<List<KhieuNaiItem>>($"/api/v1/dot-datn/{dot.Id}/khieu-nai?trangThai=ChoXuLy", Json, Ct);
        Assert.Equal(donId, Assert.Single(dsChoXuLy!).Id);

        await AssertLoiAsync(await pdt.PostAsJsonAsync($"/api/v1/don-tu/{donId}/xu-ly-khieu-nai-xet-giao",
            new XuLyKhieuNaiRequest(false, " "), Json, Ct), "UPM_KHIEU_NAI_LY_DO_TU_CHOI_BAT_BUOC");
        Assert.Equal(HttpStatusCode.NoContent, (await pdt.PostAsJsonAsync($"/api/v1/don-tu/{donId}/xu-ly-khieu-nai-xet-giao",
            new XuLyKhieuNaiRequest(false, "Điểm phúc khảo chưa được QLĐT cập nhật."), Json, Ct)).StatusCode);
        cuaToi = await KetQuaCuaToiAsync(sv, dot.MaDot);
        var tuChoi = Assert.Single(cuaToi.KhieuNai);
        Assert.Equal((TrangThaiDonTu.TuChoi, "Điểm phúc khảo chưa được QLĐT cập nhật."), (tuChoi.TrangThai, tuChoi.KetQuaXuLy));
        Assert.Single(await SnapshotAsync(pdt, hoSoId));

        // Khiếu nại lần 2 → chấp nhận → xét lại (snapshot XetLai mới, giữ snapshot cũ).
        var gui2 = await sv.PostAsJsonAsync($"/api/v1/ho-so-cua-toi/{hoSoId}/khieu-nai-xet-giao",
            new DonKhieuNaiRequest("Đề nghị xét lại sau khi có điểm."), Json, Ct);
        var don2 = (await gui2.Content.ReadFromJsonAsync<DonKhieuNaiKetQua>(Json, Ct))!.DonTuId;
        Assert.Equal(HttpStatusCode.NoContent, (await pdt.PostAsJsonAsync($"/api/v1/don-tu/{don2}/xu-ly-khieu-nai-xet-giao",
            new XuLyKhieuNaiRequest(true, null), Json, Ct)).StatusCode);
        Assert.Equal([LoaiXet.Giao, LoaiXet.XetLai], (await SnapshotAsync(pdt, hoSoId)).Select(x => x.LoaiXet).ToArray());
        cuaToi = await KetQuaCuaToiAsync(sv, dot.MaDot);
        Assert.Equal(LoaiXet.XetLai, cuaToi.KetQuaMoiNhat!.LoaiXet);
        Assert.Contains(cuaToi.KhieuNai, x => x.Id == don2 && x.TrangThai == TrangThaiDonTu.DaDuyet);
        await AssertLoiAsync(await pdt.PostAsJsonAsync($"/api/v1/don-tu/{don2}/xu-ly-khieu-nai-xet-giao",
            new XuLyKhieuNaiRequest(true, null), Json, Ct), "UPM_KHIEU_NAI_DA_XU_LY");
    }

    // ======================================================================= A18, A21, chặn trùng, phạm vi

    [Fact]
    public async Task A18_hoc_hai_CTDT_co_hai_ho_so_va_chan_trung_ho_so()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        var dot = await TaoDotAsync(client, HN, new DateOnly(2026, 9, 7));
        var sv = (await IdsAsync(["65HN001"]))[0];
        long ctdtThuHai;
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            ctdtThuHai = await db.ChuongTrinhDaoTao.Where(x => x.MaCTDT == "7840104-HN").Select(x => x.Id).SingleAsync(Ct);
        }

        var res = await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/sinh-vien",
            new[] { new HoSoTrongDotRequest(sv.SinhVienId, sv.CTDTId), new HoSoTrongDotRequest(sv.SinhVienId, ctdtThuHai) }, Json, Ct);
        Assert.Equal(2, (await res.Content.ReadFromJsonAsync<ThemHoSoKetQua>(Json, Ct))!.SoDaThem);
        var ds = await client.GetFromJsonAsync<DanhSachHoSoKetQua>($"/api/v1/dot-datn/{dot.Id}/sinh-vien", Json, Ct);
        Assert.Equal(["7580205-HN", "7840104-HN"], ds!.Items.Where(x => x.MaSV == "65HN001").Select(x => x.MaCTDT).ToArray());

        await AssertLoiAsync(await client.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/sinh-vien",
            new[] { new HoSoTrongDotRequest(sv.SinhVienId, sv.CTDTId) }, Json, Ct), "UPM_DOT_HO_SO_DA_TON_TAI");
        Assert.Equal(2, ds.Total);
    }

    [Fact]
    public async Task A21_ho_so_lam_lai_tu_lien_ket_ho_so_truoc_va_tang_LanLamLai()
    {
        var (client, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        using var ngayXet = CoDinhNgayXet(); // sau khi đăng nhập: JWT phải cấp theo giờ thật
        var sinhVienId = await TaoSinhVienTuMauAsync("65HN001", ["KTH104", "KTH106", "KTH107", "KTH109"]);
        var dotTruoc = await TaoDotAsync(client, HN, new DateOnly(2026, 2, 2), "2025-2026", 2);
        var hoSoTruoc = (await ThemIdsAsync(client, dotTruoc.Id, sinhVienId)).Single();
        await ChayTrucTiepAsync(dotTruoc.Id);
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK, await TrangThaiAsync(hoSoTruoc));

        var dotSau = await TaoDotAsync(client, HN, new DateOnly(2026, 9, 7));
        var maSV = await MaSVAsync(sinhVienId);
        var ungVien = await client.GetFromJsonAsync<List<SinhVienUngVienItem>>(
            $"/api/v1/dot-datn/{dotSau.Id}/sinh-vien-ung-vien?q={maSV}", Json, Ct);
        Assert.Equal(hoSoTruoc, Assert.Single(ungVien!).HoSoTruocId);

        var hoSoMoi = (await ThemIdsAsync(client, dotSau.Id, sinhVienId)).Single();
        var item = (await client.GetFromJsonAsync<DanhSachHoSoKetQua>($"/api/v1/dot-datn/{dotSau.Id}/sinh-vien?q={maSV}", Json, Ct))!
            .Items.Single();
        Assert.Equal((hoSoMoi, hoSoTruoc, 1, TrangThaiSVTrongDot.CHO_XET), (item.Id, item.HoSoTruocId, item.LanLamLai, item.TrangThai));
    }

    [Fact]
    public async Task Dot_kiem_tra_thong_tin_va_pham_vi_co_so()
    {
        var (hn, _, _) = await factory.DangNhapAsync("pdt.hn", Ct);
        var (hcm, _, _) = await factory.DangNhapAsync("pdt.hcm", Ct);
        var dot = await TaoDotAsync(hn, HN, new DateOnly(2026, 9, 7));

        // Số tuần ngoài 12–13 bị chặn; PĐT HCM không thấy / không tạo đợt ở HN.
        var sai = new DotDATNRequest(HN, $"T7-{Guid.NewGuid():N}"[..16], "Đợt sai", "2026-2027", 1, new DateOnly(2026, 9, 7), 14,
            new DateOnly(2026, 12, 1));
        await AssertLoiAsync(await hn.PostAsJsonAsync("/api/v1/dot-datn", sai, Json, Ct), "UPM_DOT_THONG_TIN_KHONG_HOP_LE");
        await AssertLoiAsync(await hcm.PostAsJsonAsync("/api/v1/dot-datn", sai with { SoTuan = 12 }, Json, Ct), "UPM_DOT_SCOPE_DENIED");
        await AssertLoiAsync(await hcm.GetAsync($"/api/v1/dot-datn/{dot.Id}", Ct), "UPM_DOT_NOT_FOUND");
        await AssertLoiAsync(await hcm.PostAsync($"/api/v1/dot-datn/{dot.Id}/xet-giao/chay", null, Ct), "UPM_DOT_NOT_FOUND");
        var dsHcm = await hcm.GetFromJsonAsync<PagedResult<DotDATNItem>>("/api/v1/dot-datn?pageSize=200", Json, Ct);
        Assert.DoesNotContain(dsHcm!.Items, x => x.Id == dot.Id);

        // Đóng đợt → không thêm SV, không chạy xét.
        Assert.Equal(TrangThaiDot.Dong, (await (await hn.PostAsync($"/api/v1/dot-datn/{dot.Id}/dong", null, Ct))
            .Content.ReadFromJsonAsync<DotDATNItem>(Json, Ct))!.TrangThai);
        await AssertLoiAsync(await hn.PostAsJsonAsync($"/api/v1/dot-datn/{dot.Id}/sinh-vien/theo-loc",
            new ThemHoSoTheoLocRequest(65, null, null), Json, Ct), "UPM_DOT_DA_DONG");
    }

    // ======================================================================= Helpers

    /// <summary>Cố định "hôm nay" = <see cref="NgayXet"/> (kỷ luật mẫu đang hiệu lực) cho lần xét trong test, khôi phục khi xong.</summary>
    private KhoiPhuc CoDinhNgayXet()
    {
        var cu = factory.Clock.Lech;
        // Dịch nguyên ngày (giữ giờ trong ngày); hôm nay đã là NgayXet thì không dịch.
        factory.Clock.Lech = cu + TimeSpan.FromDays(NgayXet.DayNumber - factory.Clock.Today.DayNumber);
        return new KhoiPhuc(() => factory.Clock.Lech = cu);
    }

    private sealed class KhoiPhuc(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    private static async Task<DotDATNItem> TaoDotAsync(HttpClient client, long coSoId, DateOnly batDau, string namHoc = "2026-2027", byte hocKy = 1)
    {
        var request = new DotDATNRequest(coSoId, $"T7-{Guid.NewGuid():N}"[..16], "Đợt kiểm thử UC-1", namHoc, hocKy, batDau, 12,
            batDau.AddDays(12 * 7));
        var res = await client.PostAsJsonAsync("/api/v1/dot-datn", request, Json, Ct);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<DotDATNItem>(Json, Ct))!;
    }

    private async Task<List<(long SinhVienId, long CTDTId)>> IdsAsync(IEnumerable<string> maSV)
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var list = maSV.ToList();
        var rows = await db.SinhVien.Where(x => list.Contains(x.MaSV)).Select(x => new { x.MaSV, x.Id, x.CTDTId }).ToListAsync(Ct);
        return list.Select(m => rows.Single(r => r.MaSV == m)).Select(r => (r.Id, r.CTDTId)).ToList();
    }

    private async Task ThemAsync(HttpClient client, long dotId, IEnumerable<string> maSV)
    {
        var items = (await IdsAsync(maSV)).Select(x => new HoSoTrongDotRequest(x.SinhVienId, x.CTDTId)).ToArray();
        var res = await client.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/sinh-vien", items, Json, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(items.Length, (await res.Content.ReadFromJsonAsync<ThemHoSoKetQua>(Json, Ct))!.SoDaThem);
    }

    private async Task<List<long>> ThemIdsAsync(HttpClient client, long dotId, long sinhVienId)
    {
        await using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
            var ctdtId = await db.SinhVien.Where(x => x.Id == sinhVienId).Select(x => x.CTDTId).SingleAsync(Ct);
            var res = await client.PostAsJsonAsync($"/api/v1/dot-datn/{dotId}/sinh-vien",
                new[] { new HoSoTrongDotRequest(sinhVienId, ctdtId) }, Json, Ct);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
        await using var scope2 = factory.CreateScope();
        return await scope2.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot
            .Where(x => x.DotId == dotId && x.SinhVienId == sinhVienId).Select(x => x.Id).ToListAsync(Ct);
    }

    private async Task ChayTrucTiepAsync(long dotId)
    {
        await using var scope = factory.CreateScope();
        var kq = await scope.ServiceProvider.GetRequiredService<IXetGiaoBatchRunner>().ChayAsync(dotId, null, ct: Ct);
        Assert.Empty(kq.Loi);
    }

    /// <summary>POST chạy xét → poll tiến độ đến khi job kết thúc (tối đa 60 giây).</summary>
    private static async Task<XetGiaoJobItem> ChayQuaApiAsync(HttpClient client, long dotId)
    {
        var res = await client.PostAsync($"/api/v1/dot-datn/{dotId}/xet-giao/chay", null, Ct);
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
        var job = (await res.Content.ReadFromJsonAsync<XetGiaoJobItem>(Json, Ct))!;
        var han = DateTime.UtcNow.AddSeconds(60);
        while (job.TrangThai is not ("Succeeded" or "Failed" or "Deleted"))
        {
            Assert.True(DateTime.UtcNow < han, $"Job xét giao {job.JobId} chưa xong sau 60 giây (trạng thái {job.TrangThai}).");
            await Task.Delay(200, Ct);
            job = (await client.GetFromJsonAsync<XetGiaoJobItem>($"/api/v1/dot-datn/{dotId}/xet-giao/jobs/{job.JobId}", Json, Ct))!;
        }
        return job;
    }

    private static async Task<Dictionary<string, HoSoXetGiaoItem>> HoSoTheoMaAsync(HttpClient client, long dotId)
    {
        var ds = await client.GetFromJsonAsync<DanhSachHoSoKetQua>($"/api/v1/dot-datn/{dotId}/sinh-vien?pageSize=200", Json, Ct);
        return ds!.Items.ToDictionary(x => x.MaSV);
    }

    private static async Task<List<XetGiaoSnapshotItem>> SnapshotAsync(HttpClient client, long hoSoId) =>
        (await client.GetFromJsonAsync<List<XetGiaoSnapshotItem>>($"/api/v1/svtrongdot/{hoSoId}/xet-giao", Json, Ct))!;

    private static async Task<HoSoCuaToiXetGiaoItem> KetQuaCuaToiAsync(HttpClient sv, string maDot) =>
        (await sv.GetFromJsonAsync<List<HoSoCuaToiXetGiaoItem>>("/api/v1/ho-so-cua-toi/xet-giao", Json, Ct))!.Single(x => x.MaDot == maDot);

    private async Task<(int Snapshot, int LichSu)> DemAsync(long dotId)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        return (await db.XetDieuKien.CountAsync(x => x.SVTrongDot.DotId == dotId, Ct),
            await db.LichSuTrangThai.CountAsync(x => x.SVTrongDot.DotId == dotId, Ct));
    }

    private async Task<TrangThaiSVTrongDot> TrangThaiAsync(long hoSoId)
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SVTrongDot
            .Where(x => x.Id == hoSoId).Select(x => x.TrangThai).SingleAsync(Ct);
    }

    private async Task<int> DemQuyetDinhAsync(string so)
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().QuyetDinh.CountAsync(x => x.SoQuyetDinh == so, Ct);
    }

    private async Task<string> MaSVAsync(long sinhVienId)
    {
        await using var scope = factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpmDbContext>().SinhVien
            .Where(x => x.Id == sinhVienId).Select(x => x.MaSV).SingleAsync(Ct);
    }

    /// <summary>SV mới chép từ SV mẫu (CTĐT, tiến độ, bảng điểm) – các học phần <paramref name="truot"/> đổi thành rớt.</summary>
    private async Task<long> TaoSinhVienTuMauAsync(string maMau, IReadOnlyCollection<string> truot)
    {
        await factory.SeedAsync(Ct);
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var mau = await db.SinhVien.AsNoTracking().SingleAsync(x => x.MaSV == maMau, Ct);
        var sv = new SinhVien
        {
            MaSV = $"T7{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            HoTen = "Sinh viên kiểm thử UC-1",
            NgaySinh = mau.NgaySinh,
            KhoaHoc = mau.KhoaHoc,
            CTDTId = mau.CTDTId,
            CoSoId = mau.CoSoId,
            TrangThaiHocTap = TrangThaiHocTap.DangHoc,
            NgayNhapHoc = mau.NgayNhapHoc,
        };
        db.SinhVien.Add(sv);
        await db.SaveChangesAsync(Ct);
        var tienDo = await db.ThongTinTienDoSV.AsNoTracking().SingleAsync(x => x.SinhVienId == mau.Id, Ct);
        db.ThongTinTienDoSV.Add(new ThongTinTienDoSV
        {
            SinhVienId = sv.Id,
            KyCuoiKeHoach = tienDo.KyCuoiKeHoach,
            HanToiDa = tienDo.HanToiDa,
            DungTienDo = tienDo.DungTienDo,
            DongBoLuc = tienDo.DongBoLuc,
        });
        var hpTruot = await db.HocPhan.Where(x => truot.Contains(x.MaHP)).Select(x => x.Id).ToListAsync(Ct);
        foreach (var d in await db.BangDiem.AsNoTracking().Where(x => x.SinhVienId == mau.Id).ToListAsync(Ct))
        {
            var rot = hpTruot.Contains(d.HocPhanId);
            db.BangDiem.Add(new BangDiem
            {
                SinhVienId = sv.Id,
                HocPhanId = d.HocPhanId,
                HocKy = d.HocKy,
                LanHoc = d.LanHoc,
                Diem10 = rot ? 3.0m : d.Diem10,
                DiemChu = rot ? "F" : d.DiemChu,
                Diem4 = rot ? 0m : d.Diem4,
                TrangThaiDiem = rot ? TrangThaiDiem.Rot : d.TrangThaiDiem,
                NguonDongBo = "TEST",
                DongBoLuc = d.DongBoLuc,
            });
        }
        await db.SaveChangesAsync(Ct);
        return sv.Id;
    }

    /// <summary>Mô phỏng QLĐT đồng bộ điểm mới (cải thiện / phúc khảo) cho các học phần.</summary>
    private async Task DoiDiemAsync(long sinhVienId, string[] maHP, TrangThaiDiem trangThai)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var rows = await db.BangDiem.Where(x => x.SinhVienId == sinhVienId && maHP.Contains(x.HocPhan.MaHP)).ToListAsync(Ct);
        Assert.Equal(maHP.Length, rows.Count);
        foreach (var row in rows)
        {
            row.TrangThaiDiem = trangThai;
            row.Diem10 = 7.0m;
            row.DiemChu = "B";
            row.Diem4 = 3.0m;
        }
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>Gắn đề tài đã duyệt cho hồ sơ (UC-2 / Phần 8 chưa có API).</summary>
    private async Task GanDeTaiAsync(long dotId, long hoSoId)
    {
        await using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpmDbContext>();
        var hoSo = await db.SVTrongDot.Include(x => x.CTDT).SingleAsync(x => x.Id == hoSoId, Ct);
        var boMonId = await db.BoMon.Where(x => x.KhoaId == hoSo.CTDT.KhoaId).Select(x => x.Id).FirstAsync(Ct);
        var deTai = new DeTai
        {
            DotId = dotId,
            BoMonId = boMonId,
            TenDeTai = "Đề tài kiểm thử quyết định giao",
            LoaiDeTai = LoaiDeTai.LyThuyet,
            MaxSV = 1,
            NguonDeXuat = NguonDeXuat.GV,
            TrangThaiDuyet = TrangThaiDuyetDeTai.DaDuyet,
        };
        db.DeTai.Add(deTai);
        await db.SaveChangesAsync(Ct);
        hoSo.DeTaiId = deTai.Id;
        // T03 (UC-2) cũng yêu cầu GVHD còn hiệu lực: gắn GV001 làm GVHD cho hồ sơ kiểm thử.
        var gvId = await db.GiangVien.Where(x => x.MaGV == "GV001").Select(x => x.Id).SingleAsync(Ct);
        db.PhanCong.Add(new PhanCong { SVTrongDotId = hoSo.Id, GiangVienId = gvId, VaiTro = VaiTroPhanCong.GVHD, NgayBatDau = factory.Clock.Today });
        await db.SaveChangesAsync(Ct);
    }

    private static async Task AssertLoiAsync(HttpResponseMessage res, string code)
    {
        var body = await res.Content.ReadAsStringAsync(Ct);
        var mongDoi = Upm.Domain.Common.DomainException.LaLoiTuChoiQuyen(code) ? HttpStatusCode.Forbidden : HttpStatusCode.UnprocessableEntity;
        Assert.True(res.StatusCode == mongDoi, $"Mong đợi {(int)mongDoi} {code}, nhận {(int)res.StatusCode}: {body}");
        Assert.Equal(code, JsonDocument.Parse(body).RootElement.GetProperty("code").GetString());
    }
}
