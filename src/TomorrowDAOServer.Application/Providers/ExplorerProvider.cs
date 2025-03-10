using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.AelfScan;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Providers;

public interface IExplorerProvider
{
    Task<List<AelfScanBalanceDto>> GetBalancesAsync(GetBalanceFromAelfScanRequest request);
    Task<GetTokenInfoFromAelfScanResponse> GetTokenInfoAsync(GetTokenInfoFromAelfScanRequest request);

    //Call before migration, do not delete
    Task<List<ExplorerVoteTeamDescDto>> GetAllTeamDescAsync(string chainId, bool isActive);
    Task<ExplorerProposalResponse> GetProposalPagerAsync(string chainId, ExplorerProposalListRequest request);
    Task<ExplorerPagerResult<ExplorerTransactionDetailResult>> GetTransactionDetailAsync(string chainId, ExplorerTransactionDetailRequest request);

    Task<ExplorerContractListResponse> GetContractListAsync(string chainId, ExplorerContractListRequest request);
}

public static class ExplorerApi
{
    public static readonly ApiInfo Balances = new(HttpMethod.Get, "/api/viewer/balances");
    public static readonly ApiInfo TokenInfo = new(HttpMethod.Get, "/api/viewer/tokenInfo");
    public static readonly ApiInfo TransferList = new(HttpMethod.Get, "/api/viewer/transferList");
    public static readonly ApiInfo TransactionDetail = new(HttpMethod.Get, "/api/app/blockchain/transactionDetail");
    

    //Call before migration, do not delete
    public static readonly ApiInfo GetAllTeamDesc = new(HttpMethod.Get, "/api/vote/getAllTeamDesc");
    public static readonly ApiInfo ProposalList = new(HttpMethod.Get, "/api/proposal/list");
    public static readonly ApiInfo ContractList = new(HttpMethod.Get, "/api/viewer/list");

    //AelfScan API
    public static readonly ApiInfo TokenInfoV2 = new(HttpMethod.Get, "/api/app/token/info");
    public static readonly ApiInfo BalancesV2 = new(HttpMethod.Get, "/api/app/address/tokens");
    public static readonly ApiInfo BalancesNFTV2 = new(HttpMethod.Get, "/api/app/address/nft-assets");
}

public class ExplorerProvider : IExplorerProvider, ISingletonDependency
{
    private readonly ILogger<ExplorerProvider> _logger;
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<ExplorerOptions> _explorerOptions;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IObjectMapper _objectMapper;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    private const string ProposalStatusAll = "all";


    public ExplorerProvider(IHttpProvider httpProvider, IOptionsMonitor<ExplorerOptions> explorerOptions,
        IGraphQLProvider graphQlProvider, IObjectMapper objectMapper, ILogger<ExplorerProvider> logger)
    {
        _httpProvider = httpProvider;
        _explorerOptions = explorerOptions;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    private string BaseUrl(string chainId)
    {
        var urlExists = _explorerOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var baseUrl);
        AssertHelper.IsTrue(urlExists && baseUrl.NotNullOrEmpty(), "Explorer url not found of chainId {}", chainId);
        return baseUrl!.TrimEnd('/');
    }
    
    private string BaseUrlV2()
    {
        var baseUrlV2 = _explorerOptions.CurrentValue.BaseUrlV2;
        AssertHelper.IsTrue(baseUrlV2.NotNullOrEmpty(), "ExplorerV2urlNotFound");
        return baseUrlV2!.TrimEnd('/');
    }

    //Call before migration, do not delete
    public async Task<List<ExplorerVoteTeamDescDto>> GetAllTeamDescAsync(string chainId, bool isActive)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<List<ExplorerVoteTeamDescDto>>>(
            BaseUrl(chainId), ExplorerApi.GetAllTeamDesc,
            param: new Dictionary<string, string>() { { "isActive", isActive ? "true" : "false" } },
            settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    ///     GetProposalPagerAsync
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExplorerProposalResponse> GetProposalPagerAsync(string chainId,
        ExplorerProposalListRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerProposalResponse>>(BaseUrl(chainId),
            ExplorerApi.ProposalList, param: ToDictionary(request), withInfoLog: false, withDebugLog: false,
            settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    public async Task<ExplorerContractListResponse> GetContractListAsync(string chainId,
        ExplorerContractListRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerContractListResponse>>(BaseUrl(chainId),
            ExplorerApi.ContractList, param: ToDictionary(request), withInfoLog: false, withDebugLog: false,
            settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    ///     Get Balances by address
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<List<AelfScanBalanceDto>> GetBalancesAsync(GetBalanceFromAelfScanRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<AelfScanBaseResponse<GetBalanceFromAelfScanResponse>>(
            _explorerOptions.CurrentValue.AelfScan!.TrimEnd('/'),
            ExplorerApi.BalancesV2, param: ToDictionary(request), settings: DefaultJsonSettings);
        if (!resp.Success)
        {
            Log.Error("get balances from aelfscan fail, code={0},message={1}", resp.Code, resp.Message);
            throw new UserFriendlyException($"get balances from aelfscan fail. {resp.Message}");
        }

        var respNft = await _httpProvider.InvokeAsync<AelfScanBaseResponse<GetBalanceFromAelfScanResponse>>(
            _explorerOptions.CurrentValue.AelfScan!.TrimEnd('/'),
            ExplorerApi.BalancesNFTV2, param: ToDictionary(request), settings: DefaultJsonSettings);
        if (!respNft.Success)
        {
            Log.Error("get balances NFT from aelfscan fail, code={0},message={1}", resp.Code, resp.Message);
            throw new UserFriendlyException($"get balances NFT from aelfscan fail. {resp.Message}");
        }

        var res = new List<AelfScanBalanceDto>();
        if (resp.Data != null && !resp.Data.List.IsNullOrEmpty())
        {
            res.AddRange(resp.Data.List);
        }

        if (respNft.Data != null && !respNft.Data.List.IsNullOrEmpty())
        {
            res.AddRange(respNft.Data.List);
        }

        return res;
    }

    /// <summary>
    ///     Get token info
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
    //     Message = "get token from explorer error", ReturnDefault = ReturnDefault.New,
    //     LogTargets = new []{"chainId", "request"})]
    public virtual async Task<GetTokenInfoFromAelfScanResponse> GetTokenInfoAsync(
        GetTokenInfoFromAelfScanRequest request)
    {
        if (request == null || request.Symbol.IsNullOrWhiteSpace() || request.ChainId.IsNullOrWhiteSpace())
        {
            return new GetTokenInfoFromAelfScanResponse();
        }

        var resp = await _httpProvider.InvokeAsync<AelfScanBaseResponse<GetTokenInfoFromAelfScanResponse>>(
            _explorerOptions.CurrentValue.AelfScan!.TrimEnd('/'),
            ExplorerApi.TokenInfoV2, param: ToDictionary(request), settings: DefaultJsonSettings);
        if (!resp.Success)
        {
            Log.Error("get token from aelfscan fail, code={0},message={1}", resp.Code, resp.Message);
            throw new UserFriendlyException($"get token from aelfscan fail. {resp.Message}");
        }

        return resp.Data ?? new GetTokenInfoFromAelfScanResponse();
    }

    public async Task<ExplorerPagerResult<ExplorerTransactionDetailResult>> GetTransactionDetailAsync(string chainId, ExplorerTransactionDetailRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerPagerResult<ExplorerTransactionDetailResult>>>(
            BaseUrlV2(), ExplorerApi.TransactionDetail, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    private Dictionary<string, string> ToDictionary(object param)
    {
        if (param == null) return null;
        if (param is Dictionary<string, string>) return param as Dictionary<string, string>;
        var json = param is string ? param as string : JsonConvert.SerializeObject(param, DefaultJsonSettings);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
    }
}