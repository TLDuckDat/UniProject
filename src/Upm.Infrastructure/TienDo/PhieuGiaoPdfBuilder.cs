using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Upm.Infrastructure.TienDo;

/// <summary>Dữ liệu in phiếu giao nhiệm vụ (mẫu đồ án tốt nghiệp).</summary>
public sealed record PhieuGiaoPdfDuLieu(
    string MaSV,
    string HoTen,
    string TenCTDT,
    string TenDot,
    string NamHoc,
    string GVHD,
    string NoiDungNhiemVu,
    string? YeuCauSanPham,
    DateOnly NgayGiao);

/// <summary>Xuất phiếu giao nhiệm vụ PDF bằng QuestPDF (gói Community – xem PROGRESS, quyết định giấy phép).</summary>
public sealed class PhieuGiaoPdfBuilder
{
    static PhieuGiaoPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Dung(PhieuGiaoPdfDuLieu d) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(11));
            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text("TRƯỜNG ĐẠI HỌC GIAO THÔNG VẬN TẢI").FontSize(10);
                col.Item().AlignCenter().Text("PHIẾU GIAO NHIỆM VỤ ĐỒ ÁN TỐT NGHIỆP").Bold().FontSize(15);
                col.Item().AlignCenter().Text($"{d.TenDot} – năm học {d.NamHoc}").FontSize(10);
            });
            page.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text(t => { t.Span("Sinh viên: ").Bold(); t.Span($"{d.HoTen} – MSSV {d.MaSV}"); });
                col.Item().Text(t => { t.Span("Chương trình đào tạo: ").Bold(); t.Span(d.TenCTDT); });
                col.Item().Text(t => { t.Span("Giảng viên hướng dẫn: ").Bold(); t.Span(d.GVHD); });
                col.Item().Text(t => { t.Span("Ngày giao: ").Bold(); t.Span(d.NgayGiao.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)); });
                col.Item().PaddingTop(6).Text("1. Nội dung nhiệm vụ").Bold();
                col.Item().Text(d.NoiDungNhiemVu);
                col.Item().PaddingTop(6).Text("2. Yêu cầu sản phẩm").Bold();
                col.Item().Text(string.IsNullOrWhiteSpace(d.YeuCauSanPham) ? "—" : d.YeuCauSanPham);
                col.Item().PaddingTop(24).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(c => { c.Item().Text("Giảng viên hướng dẫn").Bold(); c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9); });
                    row.RelativeItem().AlignCenter().Column(c => { c.Item().Text("Sinh viên").Bold(); c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9); });
                });
            });
            page.Footer().AlignRight().Text(t =>
            {
                t.Span("Trang ").FontSize(9);
                t.CurrentPageNumber().FontSize(9);
            });
        });
    }).GeneratePdf();
}
