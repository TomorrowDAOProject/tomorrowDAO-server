using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using TomorrowDAOServer.Auth.Common;
using TomorrowDAOServer.Auth.Constants;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Http;
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.Auth.Portkey.Providers;
using TomorrowDAOServer.Auth.Telegram.Providers;
using TomorrowDAOServer.Auth.Verifier.Constants;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace TomorrowDAOServer.Auth.Verifier.Providers;

public class TelegramUserVeriferProvider : IVerifierProvider
{
    private readonly ILogger<TelegramUserVeriferProvider> _logger;
    private readonly ITelegramVerifyProvider _telegramVerifyProvider;
    private readonly IHttpClientService _httpClientService;
    private readonly IOptionsMonitor<TelegramAuthOptions> _telegramAuthOptions;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IPortkeyProvider _portkeyProvider;


    public TelegramUserVeriferProvider(ITelegramVerifyProvider telegramVerifyProvider,
        IHttpClientService httpClientService, IOptionsMonitor<TelegramAuthOptions> telegramAuthOptions,
        IOptionsMonitor<ChainOptions> chainOptions, ILogger<TelegramUserVeriferProvider> logger, IPortkeyProvider portkeyProvider)
    {
        _telegramVerifyProvider = telegramVerifyProvider;
        _httpClientService = httpClientService;
        _telegramAuthOptions = telegramAuthOptions;
        _chainOptions = chainOptions;
        _logger = logger;
        _portkeyProvider = portkeyProvider;
    }


    public string GetLoginType()
    {
        return LoginType.LoginType_Telegram;
    }

    public async Task<VerifierResultDto> VerifyUserInfoAsync(ExtensionGrantContext context)
    {
        var openIddictParameters = context.Request.GetParameters();
        var data = openIddictParameters.ToDictionary(t => t.Key, t => t.Value.ToString());
        return await VerifyTgBotDataAndGenerateAuthDataAsync(data);
    }

    public async Task<VerifierResultDto> VerifyTgBotDataAndGenerateAuthDataAsync(
        Dictionary<string, string> data)
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
        telegramAuthDataDto.Hash = await _telegramVerifyProvider.GenerateHashAsync(telegramAuthDataDto);
        
        var addressInfos = new List<AddressInfo>();
        var caHash = string.Empty;
        var address = string.Empty;
        var chainId = string.Empty;
        var guardianResultDto = await _portkeyProvider.GetGuardianIdentifierAsync(string.Empty,telegramAuthDataDto.Id);
        if (guardianResultDto != null)
        {
            caHash = guardianResultDto.CaHash;
            address = guardianResultDto.CaAddress;
            chainId = guardianResultDto.CreateChainId;
            addressInfos.Add(new AddressInfo
            {
                ChainId = chainId,
                Address = address
            });
        }

        return new VerifierResultDto
        {
            IsVerified = true,
            CaHash = caHash,
            Address = address,
            GuardianIdentifier = telegramAuthDataDto.Id,
            CreateChainId = chainId,
            AddressInfos = addressInfos,
            ForbidResult = null,
            UserInfo = telegramAuthDataDto
        };
    }

    private async Task<bool> VerifyTelegramDataAsync(IDictionary<string, string> data)
    {
        return await _telegramVerifyProvider.ValidateTelegramDataAsync(data,
            GenerateTelegramDataHash.TgBotDataHash);
    }

    private TelegramAuthDataDto ConvertToTelegramAuthDataDto(Dictionary<string, string> data)
    {
        var userJsonString = data[CommonConstants.RequestParameterNameUser];
        var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(userJsonString);
        var telegramAuthDataDto = new TelegramAuthDataDto
        {
            Id = userData.GetValueOrDefault(CommonConstants.RequestParameterNameId, null),
            UserName = userData.GetValueOrDefault(CommonConstants.RequestParameterNameUserName, null),
            FirstName = userData.GetValueOrDefault(CommonConstants.RequestParameterNameFirstName, null),
            LastName = userData.GetValueOrDefault(CommonConstants.RequestParameterNameLastName, null),
            PhotoUrl = userData.GetValueOrDefault(CommonConstants.RequestParameterPhotoUrl, null)
        };
        telegramAuthDataDto.AuthDate = data.GetValueOrDefault(CommonConstants.RequestParameterNameAuthDate, null);
        telegramAuthDataDto.Hash = data.GetValueOrDefault(CommonConstants.RequestParameterNameHash, null);
        return telegramAuthDataDto;
    }
}