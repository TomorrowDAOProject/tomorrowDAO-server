using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.NetworkDao.Dto;
using Xunit;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest
{
    [Fact]
    public async Task GetNetworkDaoProposalsAsync_Test()
    {
        var result = await _networkDaoProposalProvider.GetNetworkDaoProposalsAsync(new GetNetworkDaoProposalsInput
        {
            ChainId = ChainIdAELF,
            ProposalIds = new List<string>() { ProposalId1 },
            ProposalType = NetworkDaoProposalType.All,
            SkipCount = 0,
            MaxResultCount = 10,
            StartBlockHeight = 0,
            EndBlockHeight = 0
        });
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items.ShouldNotBeNull();
        result.Items.First().Title.ShouldBe("ProposalId1 Title");
        result.Items.First().ProposalId.ShouldBe(ProposalId1);
    }
}