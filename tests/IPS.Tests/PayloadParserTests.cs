using IPS.Processing;
using Xunit;

namespace IPS.Tests;

public class PayloadParserTests
{
    [Fact]
    public void Parse_Reads_Pairs()
    {
        var fields = PayloadParser.Parse("numbers:100,threads:2");
        Assert.Equal("100", fields["numbers"]);
        Assert.Equal("2", fields["threads"]);
    }

    [Fact]
    public void ReadInt_Handles_Underscores()
    {
        var fields = PayloadParser.Parse("numbers:10_000,threads:2");
        Assert.Equal(10_000, PayloadParser.ReadInt(fields, "numbers"));
    }

    [Fact]
    public void Parse_Throws_On_Bad_Segment()
    {
        Assert.Throws<FormatException>(() => PayloadParser.Parse("foo"));
    }
}
