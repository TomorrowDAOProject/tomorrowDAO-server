using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using TomorrowDAOServer.Grains.Grain.Users;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserProvider
{
    Task<UserGrainDto> GetUserAsync(Guid userId);
    Task<string> GetUserAddress(Guid userId, string chainId);
    Task<string> GetAndValidateUserAddress(Guid userId, string chainId);
}

public class UserProvider : IUserProvider, ISingletonDependency
{
    private readonly ILogger<UserProvider> _logger;
    private readonly IClusterClient _clusterClient;

    public UserProvider(ILogger<UserProvider> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<UserGrainDto> GetUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        try
        {
            var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
            var user = await userGrain.GetUser();
            if (user.Success)
            {
                return user.Data;
            }

            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get user info error, userId={0}", userId.ToString());
            return null;
        }
    }

    public async Task<string> GetUserAddress(Guid userId, string chainId)
    {
        if (chainId.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        var userGrainDto = await GetUserAsync(userId);
        if (userGrainDto == null)
        {
            return string.Empty;
        }

        var addressInfo = userGrainDto.AddressInfos.Find(a => a.ChainId == chainId);
        return addressInfo == null ? string.Empty : addressInfo.Address;
    }

    public async Task<string> GetAndValidateUserAddress(Guid userId, string chainId)
    {
        var userAddress = await GetUserAddress(userId, chainId);
        if (!userAddress.IsNullOrWhiteSpace())
        {
            return userAddress;
        }

        _logger.LogError("query user address fail, userId={0}, chainId={1}", userId, chainId);
        throw new UserFriendlyException("No user address found");
    }
}