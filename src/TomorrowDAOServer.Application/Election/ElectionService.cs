using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
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
    private readonly IDAOProvider _daoProvider;

    public ElectionService(ILogger<ElectionService> logger, IElectionProvider electionProvider,
        IDAOProvider daoProvider)
    {
        _logger = logger;
        _electionProvider = electionProvider;
        _daoProvider = daoProvider;
    }


    public async Task<List<string>> GetHighCouncilMembersAsync(HighCouncilMembersInput input)
    {
        Stopwatch sw = Stopwatch.StartNew();
        if (input == null || (input.DaoId.IsNullOrWhiteSpace() && input.Alias.IsNullOrWhiteSpace()))
        {
            ExceptionHelper.ThrowArgumentException();
        }

        try
        {
            if (input.DaoId.IsNullOrWhiteSpace())
            {
                var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
                {
                    ChainId = input.ChainId,
                    DAOId = input.DaoId,
                    Alias = input.Alias
                });
                if (daoIndex == null || daoIndex.Id.IsNullOrWhiteSpace())
                {
                    throw new UserFriendlyException("No DAO information found.");
                }

                input.DaoId = daoIndex.Id;
            }
            
            var list = await _electionProvider.GetHighCouncilMembersAsync(input.ChainId, input.DaoId);
            
            sw.Stop();
            _logger.LogInformation("GetHighCouncilMembers service duration:{0}", sw.ElapsedMilliseconds);
            
            return list;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get high council members error, chainId={0},daoId={1}", input.ChainId, input.DaoId);
            throw new UserFriendlyException($"System exception occurred during querying High Council member list. {e.Message}");
        }
    }
}