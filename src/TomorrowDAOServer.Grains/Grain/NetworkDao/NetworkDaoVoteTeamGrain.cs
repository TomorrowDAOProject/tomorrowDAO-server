using Microsoft.Extensions.Logging;
using Orleans;
using TomorrowDAOServer.Grains.Grain.Dao;
using TomorrowDAOServer.Grains.State.NetworkDao;
using TomorrowDAOServer.NetworkDao.GrainDtos;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Grains.Grain.NetworkDao;

public interface INetworkDaoVoteTeamGrain : IGrainWithStringKey
{
    Task<GrainResultDto<string>> SaveVoteTeamDescriptionAsync(NetworkDaoVoteTeamDto voteTeamDto);
    Task<GrainResultDto<string>> UpdateVoteTeamStatusAsync(string publicKey, bool isActive);
    Task<GrainResultDto<List<NetworkDaoVoteTeamDto>>> GetVoteTeamListAsync();

}

public class NetworkDaoVoteTeamGrain : Grain<NetworkDaoVoteTeamState>, INetworkDaoVoteTeamGrain
{
    private readonly ILogger<DaoAliasGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoVoteTeamGrain(ILogger<DaoAliasGrain> logger, IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }


    public async Task<GrainResultDto<string>> SaveVoteTeamDescriptionAsync(NetworkDaoVoteTeamDto voteTeamDto)
    {
        var daoVoteTeams = State.VoteTeams;
        var duplicateNames = daoVoteTeams.Where(t => t.PublicKey != voteTeamDto.PublicKey).ToList();
        if (!duplicateNames.IsNullOrEmpty())
        {
            return new GrainResultDto<string>
            {
                Success = false,
                Message = "has duplicate names"
            };
        }

        try
        {
            voteTeamDto.UpdateTime ??= DateTime.Now;
            var daoVoteTeam = _objectMapper.Map<NetworkDaoVoteTeamDto, NetworkDaoVoteTeam>(voteTeamDto);
            daoVoteTeams.Add(daoVoteTeam);

            State.VoteTeams = daoVoteTeams;
            await WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Save NetworkDaoVoteTeam fail.");
            return new GrainResultDto<string>
            {
                Success = false,
                Message = $"Save NetworkDaoVoteTeam fail. {e.Message}"
            };
        }

        return new GrainResultDto<string>
        {
            Success = true
        };
    }

    public async Task<GrainResultDto<string>> UpdateVoteTeamStatusAsync(string publicKey, bool isActive)
    {
        try
        {
            var daoVoteTeams = State.VoteTeams;
            var subList = daoVoteTeams.Where(t => t.PublicKey == publicKey).ToList();
            foreach (var daoVoteTeam in daoVoteTeams.Where(daoVoteTeam => daoVoteTeam.PublicKey == publicKey))
            {
                daoVoteTeam.IsActive = isActive;
                daoVoteTeam.UpdateTime = DateTime.Now;
            }
        
            State.VoteTeams = daoVoteTeams;
            await WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Update NetworkDaoVoteTeam Status fail.");
            return new GrainResultDto<string>
            {
                Success = false,
                Message = $"Update NetworkDaoVoteTeam Status fail. {e.Message}"
            };
        }
        return new GrainResultDto<string>
        {
            Success = true
        };
    }

    public async Task<GrainResultDto<List<NetworkDaoVoteTeamDto>>> GetVoteTeamListAsync()
    {
        return new GrainResultDto<List<NetworkDaoVoteTeamDto>>
        {
            Success = true,
            Data = _objectMapper.Map<List<NetworkDaoVoteTeam>, List<NetworkDaoVoteTeamDto>>(State.VoteTeams)
        };
    }
}