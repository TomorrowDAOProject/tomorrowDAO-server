using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Vote;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class VoteService : TomorrowDAOServerAppService, IVoteService
{
    private readonly IObjectMapper _objectMapper;
    private readonly IVoteProvider _voteProvider;
    private readonly IDAOProvider _daoProvider;

    public VoteService(IVoteProvider voteProvider, IDAOProvider daoProvider,
        IObjectMapper objectMapper)
    {
        _voteProvider = voteProvider;
        _objectMapper = objectMapper;
        _daoProvider = daoProvider;
    }

    public async Task<VoteSchemeDetailDto> GetVoteSchemeAsync(GetVoteSchemeInput input)
    {
        var result = await _voteProvider.GetVoteSchemeAsync(input);
        List<VoteSchemeInfoDto> voteSchemeList;
        if (string.IsNullOrEmpty(input.DAOId))
        {
            voteSchemeList = _objectMapper.Map<List<IndexerVoteSchemeInfo>, List<VoteSchemeInfoDto>>(result);
        }
        else
        {
            var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
            {
                DAOId = input.DAOId, ChainId = input.ChainId
            });
            var voteMechanism = string.IsNullOrEmpty(daoIndex.GovernanceToken)
                ? VoteMechanism.UNIQUE_VOTE
                : VoteMechanism.TOKEN_BALLOT;
            voteSchemeList = _objectMapper.Map<List<IndexerVoteSchemeInfo>, List<VoteSchemeInfoDto>>(
                result.Where(x => x.VoteMechanism == voteMechanism).ToList());
        }

        return new VoteSchemeDetailDto
        {
            VoteSchemeList = voteSchemeList
        };
    }
}