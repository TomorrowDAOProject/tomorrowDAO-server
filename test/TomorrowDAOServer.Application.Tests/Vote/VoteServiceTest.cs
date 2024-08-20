using System.Collections.Generic;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace TomorrowDAOServer.Vote;

public class VoteServiceTest
{ 
    private readonly VoteService _voteService = new(Substitute.For<IVoteProvider>(), Substitute.For<IDAOProvider>(), 
        Substitute.For<IObjectMapper>(), Substitute.For<IOptionsMonitor<RankingOptions>>());

    [Fact]
    public async void GetVoteSchemeAsync_Test()
    {
        var result = await _voteService.GetVoteSchemeAsync(new GetVoteSchemeInput
        {
            ChainId = "AELF",
        });
        result.ShouldNotBeNull();
    }
}