using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Grains.Grain.Users;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserProvider
{
    Task<UserGrainDto> GetUserAsync(Guid userId);
    Task<string> GetUserAddressAsync(Guid userId, string chainId);
    Task<Tuple<string, string>> GetUserAddressAndCaHashAsync(Guid userId, string chainId);
    Task<string> GetAndValidateUserAddressAsync(Guid userId, string chainId);
    Task<Tuple<string, string>> GetAndValidateUserAddressAndCaHashAsync(Guid userId, string chainId);
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

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = ReturnDefault.Default,
        Message = "get user info error", LogTargets = new []{"userId"})]
    public virtual async Task<UserGrainDto> GetUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
        var user = await userGrain.GetUser();
        if (user.Success)
        {
            return user.Data;
        }

        return null;
    }

    public async Task<string> GetUserAddressAsync(Guid userId, string chainId)
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
    
    public async Task<Tuple<string, string>> GetUserAddressAndCaHashAsync(Guid userId, string chainId)
    {
        if (chainId.IsNullOrWhiteSpace())
        {
            return new Tuple<string, string>(string.Empty, string.Empty);
        }

        var userGrainDto = await GetUserAsync(userId);
        if (userGrainDto == null)
        {
            return new Tuple<string, string>(string.Empty, string.Empty);
        }

        var addressInfo = userGrainDto.AddressInfos.Find(a => a.ChainId == chainId);
        var address =  addressInfo == null ? string.Empty : addressInfo.Address;
        var addressCaHash = userGrainDto.CaHash ?? string.Empty;
        return new Tuple<string, string>(address, addressCaHash);
    }
    
    public async Task<string> GetAndValidateUserAddressAsync(Guid userId, string chainId)
    {
        var userAddress = await GetUserAddressAsync(userId, chainId);
        if (!userAddress.IsNullOrWhiteSpace())
        {
            return userAddress;
        }

        Log.Error("query user address fail, userId={0}, chainId={1}", userId, chainId);
        throw new UserFriendlyException("No user address found");
    }

    public async Task<Tuple<string, string>> GetAndValidateUserAddressAndCaHashAsync(Guid userId, string chainId)
    {
        var (address, addressCaHash) = await GetUserAddressAndCaHashAsync(userId, chainId);
        if (!address.IsNullOrWhiteSpace() && !addressCaHash.IsNullOrWhiteSpace())
        {
            return new Tuple<string, string>(address, addressCaHash);
        }
        Log.Error("query user address fail, userId={0}, chainId={1}", userId, chainId);
        throw new UserFriendlyException("No user address and caHash found");
    }
}