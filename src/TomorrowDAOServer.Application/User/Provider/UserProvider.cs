using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Grains.Grain.Users;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace TomorrowDAOServer.User.Provider;

public interface IUserProvider
{
    Task<UserGrainDto> GetUserAsync(Guid userId);
    Task<UserGrainDto> GetAuthenticatedUserAsync(ICurrentUser currentUser);
    Task<string> GetUserAddressAsync(string chainId, UserGrainDto userGrainDto);
    Task<string> GetUserAddressAsync(Guid userId, string chainId);
    Task<Tuple<string, string>> GetUserAddressAndCaHashAsync(Guid userId, string chainId);
    Task<string> GetAndValidateUserAddressAsync(Guid userId, string chainId);
    Task<Tuple<string, string>> GetAndValidateUserAddressAndCaHashAsync(Guid userId, string chainId);
    Task<bool> UpdateUserAsync(UserGrainDto input);
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
        Message = "get user info error", LogTargets = new[] { "userId" })]
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

    public async Task<UserGrainDto> GetAuthenticatedUserAsync(ICurrentUser currentUser)
    {
        var userId = currentUser.IsAuthenticated ? currentUser.GetId() : Guid.Empty;
        if (userId == Guid.Empty)
        {
            throw new UserFriendlyException("User is not authenticated.");
        }

        var userGrainDto = await GetUserAsync(userId);
        if (userGrainDto == null)
        {
            throw new UserFriendlyException("User does not exist.");
        }

        return userGrainDto;
    }

    public async Task<string> GetUserAddressAsync(string chainId, UserGrainDto userGrainDto)
    {
        if (userGrainDto == null)
        {
            return string.Empty;
        }

        var addressInfo = !userGrainDto.CaHash.IsNullOrWhiteSpace()
            ? userGrainDto.AddressInfos?.First()
            : userGrainDto.AddressInfos.Find(a => a.ChainId == chainId);
        return addressInfo == null ? string.Empty : addressInfo.Address;
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
        var address = addressInfo == null ? string.Empty : addressInfo.Address;
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

    public async Task<bool> UpdateUserAsync(UserGrainDto input)
    {
        if (input == null || input.UserId == Guid.Empty)
        {
            return false;
        }

        var userGrain = _clusterClient.GetGrain<IUserGrain>(input.UserId);
        var user = await userGrain.UpdateUser(input);
        return user.Success;
    }
}