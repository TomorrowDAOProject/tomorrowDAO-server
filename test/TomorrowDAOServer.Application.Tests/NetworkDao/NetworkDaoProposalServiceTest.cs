using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.Providers;
using Xunit;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest
{
    [Fact]
    public async Task GetProposalList_Test()
    {
        MockExplorerRequest();
        
        var result = await _networkDaoProposalService.GetProposalListAsync(new ProposalListRequest
        {
            ChainId = ChainIdAELF,
            Address = null,
            Search = null,
            IsContract = 0,
            PageSize = 6,
            PageNum = 1,
            Status = "all",
            ProposalType = "Referendum"
        });
        result.ShouldNotBeNull();

        var proposal1 = result.List.FirstOrDefault(item => item.ProposalId == ProposalId1);
        proposal1.ShouldNotBeNull();
        proposal1.ProposalId.ShouldBe(ProposalId1);
        proposal1.Title.ShouldBe("ProposalId1 Title");
        proposal1.Description.ShouldBe("ProposalId1 Description");
    }
}