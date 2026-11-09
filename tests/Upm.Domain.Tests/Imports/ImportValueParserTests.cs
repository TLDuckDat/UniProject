using Upm.Application.DongBo;

namespace Upm.Domain.Tests.DataImport;

public sealed class ImportValueParserTests
{
    [Theory]
    [InlineData("8,5", 8.5)]
    [InlineData("8.5", 8.5)]
    public void Decimal_doc_duoc_dau_phay_va_dau_cham(string text, decimal expected)
    {
        Assert.True(ImportValueParser.TryDecimal(text, out var value));
        Assert.Equal(expected, value);
    }
}
