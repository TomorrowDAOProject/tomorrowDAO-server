using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Providers;

public interface IExplorerProvider
{
    Task<ExplorerProposalResponse> GetProposalPagerAsync(string chainId, ExplorerProposalListRequest request);
    Task<ExplorerProposalInfoResponse> GetProposalInfoAsync(string chainId, ExplorerProposalInfoRequest request);
    Task<List<ExplorerBalanceOutput>> GetBalancesAsync(string chainId, ExplorerBalanceRequest request);
    Task<ExplorerTokenInfoResponse> GetTokenInfoAsync(string chainId, ExplorerTokenInfoRequest request);
    Task<ExplorerPagerResult<ExplorerTransactionResponse>> GetTransactionPagerAsync(string chainId,
        ExplorerTransactionRequest request);

    Task<ExplorerPagerResult<ExplorerTransferResult>> GetTransferListAsync(string chainId,
        ExplorerTransferRequest request);
}

public static class ExplorerApi
{
    public static readonly ApiInfo ProposalList = new(HttpMethod.Get, "/api/proposal/list");
    public static readonly ApiInfo ProposalInfo = new(HttpMethod.Get, "/api/proposal/proposalInfo");
    public static readonly ApiInfo Organizations = new(HttpMethod.Get, "/api/proposal/organizations");
    public static readonly ApiInfo Balances = new(HttpMethod.Get, "/api/viewer/balances");
    public static readonly ApiInfo TokenInfo = new(HttpMethod.Get, "/api/viewer/tokenInfo");
    public static readonly ApiInfo Transactions = new(HttpMethod.Get, "/api/all/transaction");
    public static readonly ApiInfo TransferList = new(HttpMethod.Get, "/api/viewer/transferList");
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

    public string BaseUrl(string chainId)
    {
        var urlExists = _explorerOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var baseUrl);
        AssertHelper.IsTrue(urlExists && baseUrl.NotNullOrEmpty(), "Explorer url not found of chainId {}", chainId);
        return baseUrl!.TrimEnd('/');
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

    public async Task<ExplorerProposalInfoResponse> GetProposalInfoAsync(string chainId, ExplorerProposalInfoRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerProposalInfoResponse>>(BaseUrl(chainId),
            ExplorerApi.ProposalInfo, param: ToDictionary(request), withInfoLog: false, withDebugLog: false,
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
    public async Task<List<ExplorerBalanceOutput>> GetBalancesAsync(string chainId, ExplorerBalanceRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<List<ExplorerBalanceOutput>>>(BaseUrl(chainId),
            ExplorerApi.Balances, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    ///     Get token info
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
        Message = "get token from explorer error", ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"chainId", "request"})]
    public virtual async Task<ExplorerTokenInfoResponse> GetTokenInfoAsync(string chainId, ExplorerTokenInfoRequest request)
    {
        if (request == null || request.Symbol.IsNullOrWhiteSpace())
        {
            return new ExplorerTokenInfoResponse();
        }

        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerTokenInfoResponse>>(
            BaseUrl(chainId),
            ExplorerApi.TokenInfo, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        if (!resp.Success)
        {
            Log.Error("get token from explorer fail, code={0},message={1}", resp.Code, resp.Msg);
        }

        return resp.Data ?? new ExplorerTokenInfoResponse();
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    public async Task<ExplorerPagerResult<ExplorerTransactionResponse>> GetTransactionPagerAsync(string chainId,
        ExplorerTransactionRequest request)
    {
        var resp = await _httpProvider
            .InvokeAsync<ExplorerBaseResponse<ExplorerPagerResult<ExplorerTransactionResponse>>>(BaseUrl(chainId),
                ExplorerApi.Transactions, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExplorerPagerResult<ExplorerTransferResult>> GetTransferListAsync(string chainId,
        ExplorerTransferRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerPagerResult<ExplorerTransferResult>>>(
            BaseUrl(chainId), ExplorerApi.TransferList, param: ToDictionary(request), settings: DefaultJsonSettings);
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