using CodingSeb.ExpressionEvaluator;
using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common;

public class ExpressionHelperTest : TomorrowDaoServerApplicationContractsTestsBase
{
    private readonly Dictionary<string, object> _dictionary;

    public ExpressionHelperTest(ITestOutputHelper output) : base(output)
    {
        _dictionary = new Dictionary<string, object>
        {
            { "item", "aa" },
            { "list", new List<string> { "aa", "bb", "cc" } },
            { "version", "1.9.3" },
            { "from", "1.0.0" },
            { "to", "2.0.0" },
            { "versions", "3.0.0" }
        };
    }

    [Fact]
    public void EvaluateTest()
    {
        var b = ExpressionHelper.Evaluate("InList(item, list)", _dictionary);
        b.ShouldBeTrue();

        Assert.Throws<ExpressionEvaluatorSyntaxErrorException>(() =>
        {
            b = ExpressionHelper.Evaluate("InList(item, list)", null);
            b.ShouldBeFalse();
        });

        b = ExpressionHelper.Evaluate("VersionInRange(version, from, to)", _dictionary);
        b.ShouldBeTrue();

        b = ExpressionHelper.Evaluate("VersionInRange(versions, from, to)", _dictionary);
        b.ShouldBeFalse();
    }
}