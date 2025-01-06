using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Http;
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Auth.Portkey.Providers;

public interface IPortkeyProvider
{
    Task<GuardianResultDto> GetGuardianIdentifierAsync(string caHash, string guardianIdentifier);
    Task<GuardianDto> GetLoginGuardianAsync(GuardianResultDto guardianResultDto);
}

public class PortkeyProvider : IPortkeyProvider, ISingletonDependency
{
    private readonly ILogger<PortkeyProvider> _logger;
    private IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<TelegramAuthOptions> _telegramAuthOptions;
    private readonly IHttpClientService _httpClientService;

    private const string Colon = ":";
    private const string GuardianIdentifiersApi = "/api/app/account/guardianIdentifiers";

    public PortkeyProvider(ILogger<PortkeyProvider> logger, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<TelegramAuthOptions> telegramAuthOptions, IHttpClientService httpClientService)
    {
        _logger = logger;
        _chainOptions = chainOptions;
        _telegramAuthOptions = telegramAuthOptions;
        _httpClientService = httpClientService;
    }


    public async Task<GuardianResultDto> GetGuardianIdentifierAsync(string caHash, string guardianIdentifier)
    {
        var chainIds = _chainOptions.CurrentValue.ChainInfos
            .Select(key => _chainOptions.CurrentValue.ChainInfos[key.Key]).ToList();
        var url = _telegramAuthOptions.CurrentValue.PortkeyUrl!.TrimEnd('/') + GuardianIdentifiersApi;
        
        foreach (var chainInfo in chainIds)
        {
            try
            {
                var param = new Dictionary<string, string>();
                if (!caHash.IsNullOrWhiteSpace())
                {
                    param["caHash"] = caHash;
                    param["chainId"] = chainInfo.ChainId;
                } else
                {
                    param["guardianIdentifier"] = guardianIdentifier;
                    param["chainId"] = chainInfo.ChainId;
                }
                
                var guardianResultDto = await _httpClientService.GetAsync<GuardianResultDto>(url, param);
                if (guardianResultDto.Error != null)
                {
                    _logger.LogWarning("Query CAAddress by GuardianIdentifier warning. {0}",
                        JsonConvert.SerializeObject(guardianResultDto.Error));
                    continue;
                }

                return guardianResultDto;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Query CAAddress by GuardianIdentifier error. guardianIdentifier={0}, caHash={1}", guardianIdentifier, caHash);
            }
        }

        return null;
    }

    public async Task<GuardianDto> GetLoginGuardianAsync(GuardianResultDto guardianResultDto)
    {
        if (guardianResultDto == null)
        {
            return null;
        }

        var guardianDtos = guardianResultDto.GuardianList?.Guardians?? new  List<GuardianDto>();
        return guardianDtos.FirstOrDefault(guardianDto => guardianDto.IsLoginGuardian);
    }
}