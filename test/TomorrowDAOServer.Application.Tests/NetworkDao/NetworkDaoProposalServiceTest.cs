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

    private void MockExplorerRequest()
    {
        HttpRequestMock.MockHttpByPath(ExplorerApi.ProposalList.Method, ExplorerApi.ProposalList.Path,
            new ExplorerBaseResponse<ExplorerProposalResponse>
            {
                Code = 0,
                Msg = null,
                Data = new ExplorerProposalResponse
                {
                    Total = 1,
                    List = new List<ExplorerProposalResult>()
                    {
                        new ExplorerProposalResult
                        {
                            Abstentions = null,
                            Approvals = null,
                            CanVote = false,
                            ContractAddress = null,
                            ContractMethod = null,
                            CreateAt = default,
                            CreateTxId = null,
                            CreatedBy = null,
                            ExpiredTime = default,
                            Id = 0,
                            IsContractDeployed = false,
                            LeftInfo = null,
                            OrganizationAddress = null,
                            OrgAddress = null,
                            OrganizationInfo = null,
                            ProposalType = null,
                            TxId = null,
                            UpdatedAt = default,
                            ProposalId = ProposalId1,
                            Proposer = null,
                            Rejections = 0,
                            ReleasedTime = default,
                            ReleasedTxId = null,
                            Status = null,
                            VotedStatus = null,
                            Title = null,
                            Description = null
                        },
                        new ExplorerProposalResult
                        {
                            Abstentions = null,
                            Approvals = null,
                            CanVote = false,
                            ContractAddress = null,
                            ContractMethod = null,
                            CreateAt = default,
                            CreateTxId = null,
                            CreatedBy = null,
                            ExpiredTime = default,
                            Id = 0,
                            IsContractDeployed = false,
                            LeftInfo = null,
                            OrganizationAddress = null,
                            OrgAddress = null,
                            OrganizationInfo = null,
                            ProposalType = null,
                            TxId = null,
                            UpdatedAt = default,
                            ProposalId = ProposalId2,
                            Proposer = null,
                            Rejections = 0,
                            ReleasedTime = default,
                            ReleasedTxId = null,
                            Status = null,
                            VotedStatus = null,
                            Title = null,
                            Description = null
                        }
                    },
                    BpCount = 17
                }
            });
    }
}