using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public class RankingAppAliasTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    
    private static readonly char[] Characters = new char[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
        'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
    };

    public RankingAppAliasTest(ITestOutputHelper output, ITestOutputHelper testOutputHelper) : base(output)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task TestNewPattern()
    {
        var currentValue = 354;
        var batchSize = 3;
        var res = new List<string>();
        for (var i = 0; i < batchSize; i++)
        {
            ++currentValue;
            
            var value = currentValue;
            var sb = new StringBuilder();
            while (value > 0)
            {
                var index = (int)(value % Characters.Length);
                sb.Append(Characters[index]);
                value /= Characters.Length;
            }
            res.Add(new string(sb.ToString().Reverse().ToArray()));
        }
        res.Count.ShouldBe(3);
    }
}