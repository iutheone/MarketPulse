using MarketPulse.Application.Queries;

namespace MarketPulse.UnitTests;

public class AnomalyQueryTests
{
    [Fact]
    public void Normalize_UppercasesSymbolAndClampsPaging()
    {
        var query = AnomalyQuery.Normalize("nvda", "high", "score", "ASC", 0, 500);

        Assert.Equal("NVDA", query.Symbol);
        Assert.Equal("High", query.Severity);
        Assert.Equal("score", query.Sort);
        Assert.Equal("asc", query.Direction);
        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.PageSize);
    }

    [Fact]
    public void Normalize_DropsUnknownSeverityAndSort()
    {
        var query = AnomalyQuery.Normalize("AAPL", "yolo", "magic", "sideways", 2, 10);

        Assert.Null(query.Severity);
        Assert.Equal("detectedat", query.Sort);
        Assert.Equal("desc", query.Direction);
        Assert.Equal(2, query.Page);
        Assert.Equal(10, query.PageSize);
    }

    [Fact]
    public void HistoryQuery_NormalizesSymbol()
    {
        var query = HistoryQuery.Normalize(" msft ", null, null, -1, 0);
        Assert.Equal("MSFT", query.Symbol);
        Assert.Equal(1, query.Page);
        Assert.Equal(50, query.PageSize);
    }
}
