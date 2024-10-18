using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public class RankingProposalDescriptionTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RankingProposalDescriptionTest(ITestOutputHelper output, ITestOutputHelper testOutputHelper) : base(output)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task TestNewPattern()
    {
        //var pattern = @"^##GameRanking[\s]*:[\s]*((?:\{[^{}]*\}[,]?)+)(?:#B[\s]*:[\s]*(?:\{([^{}]*)?\})?)?$";
        //var regex = new Regex(pattern, RegexOptions.Compiled);
        var regex = new Regex(CommonConstant.NewDescriptionPattern, RegexOptions.Compiled);
        
        var description = @"##GameRanking:{aliasA},{aliasB},{aliasC}#B:{aliasBanner}";
        var match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("{aliasA},{aliasB},{aliasC}");
        match.Groups[2].Value.ShouldBe("aliasBanner");
        
        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}#B:{}";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("{aliasA},{aliasB},{aliasC}");
        match.Groups[2].Value.ShouldBe("");
        
        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}#B:";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("{aliasA},{aliasB},{aliasC}");
        match.Groups[2].Value.ShouldBe("");
        
        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("{aliasA},{aliasB},{aliasC}");
        match.Groups[2].Value.ShouldBe("");
        
        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}B";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();

        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}#B";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();

        description = @"##GameRanking:{aliasA},{aliasB},{aliasC}#B  : ";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("{aliasA},{aliasB},{aliasC}");
        match.Groups[2].Value.ShouldBe("");
        
        description = @"##GameRanking:{aliasA},{aliasB},{aliasC";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:#B:{aliasBanner}";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:{}#B:{aliasBanner}";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:{#B:{aliasBanner}";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:}#B:{aliasBanner}";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:{a}#B:{aliasBanner}";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task TestOldPattern()
    {
        //var pattern = @"^##GameRanking\s*:\s*([a-zA-Z0-9&'’\-]+(?:\s*,\s*[a-zA-Z0-9&'’\-]+)*)+$";
        //var regex = new Regex(pattern, RegexOptions.Compiled);
        var regex = new Regex(CommonConstant.OldDescriptionPattern, RegexOptions.Compiled);
        
        var description = @"##GameRanking:aliasA,aliasB,aliasC";
        var match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("aliasA,aliasB,aliasC");
        
        description = @"##GameRanking:aliasA";
        match = regex.Match(description);
        match.Success.ShouldBeTrue();
        match.Groups[1].Value.ShouldBe("aliasA");
        
        description = @"##GameRanking:";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
        
        description = @"##GameRanking:  ";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();

        description = @"##GameRanking: {aliasA}";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();

        description = @"##GameRanking:aliasA#";
        match = regex.Match(description);
        match.Success.ShouldBeFalse();
    }
    
    [Fact]
    public async Task TestAliasPattern()
    {
        //var pattern = @"\{([^{}]+)\}";
        //var regex = new Regex(pattern, RegexOptions.Compiled);
        var regex = new Regex(CommonConstant.NewDescriptionAliasPattern, RegexOptions.Compiled);
        
        var description = "{aliasA},{aliasB},{aliasC}";
        var matchs = regex.Matches(description);
        matchs.Count.ShouldBe(3);
        matchs[0].Groups[1].Value.ShouldBe("aliasA");
        matchs[1].Groups[1].Value.ShouldBe("aliasB");
        matchs[2].Groups[1].Value.ShouldBe("aliasC");

    }
}