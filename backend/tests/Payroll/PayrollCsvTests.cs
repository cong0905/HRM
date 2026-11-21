using HRM.Next.Infrastructure.Payroll;
using Xunit;
namespace HRM.Next.Tests.Integration;
public sealed class PayrollCsvTests
{
    [Theory]
    [InlineData("=1+1","'=1+1")]
    [InlineData("+1+1","'+1+1")]
    [InlineData("-1+1","'-1+1")]
    [InlineData("@SUM(1)","'@SUM(1)")]
    [InlineData("  =1+1","'  =1+1")]
    [InlineData("\t=1+1","'\t=1+1")]
    [InlineData("\r=1+1","'\r=1+1")]
    [InlineData("\n=1+1","'\n=1+1")]
    [InlineData("\ufeff=1+1","'\ufeff=1+1")]
    [InlineData("Nguyễn, \"Công\"","Nguyễn, \"\"Công\"\"")]
    [InlineData("Đỗ Văn Công","Đỗ Văn Công")]
    [InlineData("","")]
    public void Payslip_CsvUntrustedText_IsQuotedAndFormulaSafe(string input,string escaped) =>
        Assert.Equal("\""+escaped+"\"",PayrollReadService.CsvText(input));
}
