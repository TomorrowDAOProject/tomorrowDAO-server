using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.Election;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ElectionService : TomorrowDAOServerAppService, IElectionService
{
    private readonly ILogger<ElectionService> _logger;
    private readonly IElectionProvider _electionProvider;

    public ElectionService(ILogger<ElectionService> logger, IElectionProvider electionProvider)
    {
        _logger = logger;
        _electionProvider = electionProvider;
    }


    public async Task<List<string>> GetHighCouncilMembersAsync(HighCouncilMembersInput input)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            var list = await _electionProvider.GetHighCouncilMembersAsync(input.ChainId, input.DaoId);
            
            sw.Stop();
            _logger.LogInformation("GetHighCouncilMembers service duration:{0}", sw.ElapsedMilliseconds);
            
            return list;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get high council members error, chainId={0},daoId={1}", input.ChainId, input.DaoId);
            throw new UserFriendlyException("Failed to query the High Council member list.");
        }
    }
}