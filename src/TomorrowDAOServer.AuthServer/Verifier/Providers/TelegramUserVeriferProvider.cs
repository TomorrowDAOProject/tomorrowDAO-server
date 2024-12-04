using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using TomorrowDAOServer.Auth.Common;
using TomorrowDAOServer.Auth.Constants;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Http;
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.Auth.Telegram.Providers;
using TomorrowDAOServer.Auth.Verifier.Constants;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace TomorrowDAOServer.Auth.Verifier.Providers;

public class TelegramUserVeriferProvider : IVerifierProvider, ISingletonDependency
{
    private readonly ITelegramVerifyProvider _telegramVerifyProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpClientService _httpClientService;
    private readonly IOptionsMonitor<TelegramAuthOptions> _telegramAuthOptions;
    private IOptionsMonitor<ChainOptions> _chainOptions;

    private const string Colon = ":";
    private const string GuardianIdentifiersApi = "/api/app/account/guardianIdentifiers";


    public TelegramUserVeriferProvider(ITelegramVerifyProvider telegramVerifyProvider, IObjectMapper objectMapper,
        IHttpClientService httpClientService, IOptionsMonitor<TelegramAuthOptions> telegramAuthOptions,
        IOptionsMonitor<ChainOptions> chainOptions)
    {
        _telegramVerifyProvider = telegramVerifyProvider;
        _objectMapper = objectMapper;
        _httpClientService = httpClientService;
        _telegramAuthOptions = telegramAuthOptions;
        _chainOptions = chainOptions;
    }


    public string GetLoginType()
    {
        return LoginType.LoginType_Telegram;
    }

    public async Task<VerifierResultDto> VerifyUserInfoAsync(ExtensionGrantContext context)
    {
        //var streamReader = new StreamReader(context.Request.Body);
        //var requestJson = await streamReader.ReadToEndAsync();
        //var data = JsonConvert.DeserializeObject<IDictionary<string, string>>(requestJson);
        var openIddictParameters = context.Request.GetParameters();
        var data = openIddictParameters.ToDictionary(t => t.Key, t => t.Value.ToString());
        return await VerifyTgBotDataAndGenerateAuthDataAsync(data);
    }

    public async Task<VerifierResultDto> VerifyTgBotDataAndGenerateAuthDataAsync(
        IDictionary<string, string> data)
    {
        var result = await VerifyTelegramDataAsync(data);
        if (!result)
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"Telegram user information verification failed. {JsonConvert.SerializeObject(data)}")
            };
        }

        var telegramAuthDataDto = ConvertToTelegramAuthDataDto(data);

        var hash = await _telegramVerifyProvider.GenerateHashAsync(telegramAuthDataDto);
        telegramAuthDataDto.Hash = hash;


        var chainIds = _chainOptions.CurrentValue.ChainInfos.Select(key => _chainOptions.CurrentValue.ChainInfos[key.Key]).ToList();
        var url = _telegramAuthOptions.CurrentValue.PortkeyUrl!.TrimEnd('/') + GuardianIdentifiersApi;

        var addressInfos = new List<AddressInfo>();
        var caHash = string.Empty;
        var address = string.Empty;
        var chainId = string.Empty;
        foreach (var chainInfo in chainIds)
        {
            var guardianResultDto = await _httpClientService.GetAsync<GuardianResultDto>(url, new Dictionary<string, string>
            {
                { "guardianIdentifier", telegramAuthDataDto.Id },
                { "chainId", chainInfo.ChainId}
            });

            addressInfos.Add(new AddressInfo()
            {
                Address = guardianResultDto.CaAddress,
                ChainId = chainInfo.ChainId
            });
            
            chainId = guardianResultDto.CreateChainId;
        }

        return new VerifierResultDto
        {
            IsVerified = true,
            CaHash = caHash,
            Address = address,
            GuardianIdentifier = telegramAuthDataDto.Id,
            CreateChainId = chainId,
            AddressInfos = addressInfos,
            ForbidResult = null
        };
    }

    private async Task<bool> VerifyTelegramDataAsync(IDictionary<string, string> data)
    {
        return await _telegramVerifyProvider.ValidateTelegramDataAsync(data,
            GenerateTelegramDataHash.TgBotDataHash);
    }

    private TelegramAuthDataDto ConvertToTelegramAuthDataDto(IDictionary<string, string> data)
    {
        var userJsonString = data[CommonConstants.RequestParameterNameUser];
        var userData = JsonConvert.DeserializeObject<IDictionary<string, string>>(userJsonString);
        var telegramAuthDataDto = _objectMapper.Map<IDictionary<string, string>, TelegramAuthDataDto>(userData);
        telegramAuthDataDto.AuthDate = data.ContainsKey(CommonConstants.RequestParameterNameAuthDate)
            ? data[CommonConstants.RequestParameterNameAuthDate]
            : null;
        return telegramAuthDataDto;
    }
}