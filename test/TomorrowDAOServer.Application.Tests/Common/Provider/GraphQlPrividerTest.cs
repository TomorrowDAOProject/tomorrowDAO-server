using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Shouldly;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Token;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Common.Provider;

public partial class GraphQlPrividerTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IClusterClient _clusterClient;
    
    public GraphQlPrividerTest(ITestOutputHelper output) : base(output)
    {
        _graphQlProvider = Application.ServiceProvider.GetRequiredService<IGraphQLProvider>();
        _clusterClient = Application.ServiceProvider.GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task GetTokenInfoAsyncTest()
    {
        await _graphQlProvider.SetTokenInfoAsync(new TokenInfoDto
        {
            Id = "id",
            ContractAddress = "ContractAddress",
            Symbol = ELF,
            ChainId = ChainIdAELF,
            IssueChainId = null,
            TxId = null,
            Name = null,
            TotalSupply = null,
            Supply = null,
            Decimals = null,
            Holders = null,
            Transfers = null,
            LastUpdateTime = 0
        });

        var tokenInfo = await _graphQlProvider.GetTokenInfoAsync(ChainIdAELF, ELF);
        tokenInfo.ShouldNotBeNull();
        tokenInfo.Symbol.ShouldBe(ELF);
    }
    
    [Fact]
    public async Task GetTokenInfoAsyncTest_Exception()
    {
        await Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await _graphQlProvider.SetTokenInfoAsync(null);
        });
    }

    [Fact]
    public async Task SetBPAsyncTest()
    {
        await _graphQlProvider.SetBPAsync(ChainIdAELF, new List<string>() { Address1, Address2 }, 5);

        var bpList = await _graphQlProvider.GetBPAsync(ChainIdAELF);
        bpList.ShouldNotBeNull();
        bpList.Count.ShouldBe(2);

        var bpInfoDto = await _graphQlProvider.GetBPWithRoundAsync(ChainIdAELF);
        bpInfoDto.ShouldNotBeNull();
        bpInfoDto.Round.ShouldBe(5);
        bpInfoDto.AddressList.ShouldNotBeNull();
        bpInfoDto.AddressList.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SetProposalNumAsyncTest()
    {
        await _graphQlProvider.SetProposalNumAsync(ChainIdAELF, 10, 20, 30);

        var proposalNumAsync = await _graphQlProvider.GetProposalNumAsync(ChainIdAELF);
        proposalNumAsync.ShouldBe(60);
    }

    [Fact]
    public async Task SetLastEndHeightAsyncTest()
    {
        await _graphQlProvider.SetLastEndHeightAsync(ChainIdAELF, WorkerBusinessType.ProposalSync, 100);

        var heightAsync = await _graphQlProvider.GetLastEndHeightAsync(ChainIdAELF, WorkerBusinessType.ProposalSync);
        heightAsync.ShouldBe(100);
    }

    [Fact]
    public async Task GetHoldersAsyncTest()
    {
        var dictionary = await _graphQlProvider.GetHoldersAsync(new List<string> { ELF }, ChainIdAELF, 0, 100);

        dictionary.ShouldNotBeNull();
        dictionary.Count.ShouldBe(0);
       // dictionary.Keys.ShouldContain(ELF);
    }

    [Fact]
    public async Task GetDAOAmountAsyncTest()
    {
        var amounts = await _graphQlProvider.GetDAOAmountAsync(ChainIdAELF);
        amounts.ShouldNotBeNull();
        amounts.Count.ShouldBe(1);
        amounts.First().Amount.ShouldBe(1000);
        
        amounts = await _graphQlProvider.GetDAOAmountAsync("ThrowException");
        amounts.ShouldNotBeNull();
        amounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetHighCouncilMembersAsyncTest()
    {
        await _graphQlProvider.SetHighCouncilMembersAsync(ChainIdAELF, DAOId, new List<string>() { Address1, Address2 });

        var councilMembers = await _graphQlProvider.GetHighCouncilMembersAsync(ChainIdAELF, DAOId);
        councilMembers.ShouldNotBeNull();
        councilMembers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SetDaoAliasInfoAsyncTest()
    {
        var result = await _graphQlProvider.SetDaoAliasInfoAsync(ChainIdAELF, "daoname", new DaoAliasDto
        {
            DaoId = DAOId,
            DaoName = "DaoName",
            Alias = "daoname",
            CharReplacements = null,
            FilteredChars = null,
            Serial = 0,
            CreateTime = DateTime.UtcNow
        });
        result.ShouldBe(0);
    }

}