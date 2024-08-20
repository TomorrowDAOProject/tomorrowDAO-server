using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
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
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;

    public VoteService(IVoteProvider voteProvider, IDAOProvider daoProvider,
        IObjectMapper objectMapper, IOptionsMonitor<RankingOptions> rankingOptions)
    {
        _voteProvider = voteProvider;
        _objectMapper = objectMapper;
        _rankingOptions = rankingOptions;
        _daoProvider = daoProvider;
    }

    public async Task<VoteSchemeDetailDto> GetVoteSchemeAsync(GetVoteSchemeInput input)
    {
        var rankingDaoIds = _rankingOptions.CurrentValue.DaoIds;
        var result = await _voteProvider.GetVoteSchemeAsync(input);
        List<IndexerVoteSchemeInfo> filterResult;
        if (string.IsNullOrEmpty(input.DAOId))
        {
            filterResult = result.Where(x => x.VoteStrategy == VoteStrategy.PROPOSAL_DISTINCT && !x.WithoutLockToken).ToList();
        }
        else
        {
            if (rankingDaoIds.Contains(input.DAOId))
            {
                filterResult = result.Where(x => x.VoteStrategy == VoteStrategy.DAY_DISTINCT && x.WithoutLockToken).ToList();
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
                filterResult = result.Where(x => x.VoteStrategy == VoteStrategy.PROPOSAL_DISTINCT 
                                                 && !x.WithoutLockToken && x.VoteMechanism == voteMechanism).ToList();
            }
        }

        return new VoteSchemeDetailDto
        {
            VoteSchemeList = _objectMapper.Map<List<IndexerVoteSchemeInfo>, List<VoteSchemeInfoDto>>(filterResult)
        };
    }
}