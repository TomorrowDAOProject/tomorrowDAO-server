using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Indexer;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Providers;

public interface IPortkeyProvider
{
    Task<Tuple<string, string>> GetShortLingAsync(string chainId, string token);
    Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime, long endTime, int skipCount, int maxResultCount);
    Task<List<ReferralCodeInfo>> GetReferralCodeCaHashAsync(List<string> referralCodes);
}

public static class ReferralApi
{
    public static readonly ApiInfo ShortLink = new(HttpMethod.Get, "/api/app/growth/shortLink");
    public static readonly ApiInfo ReferralCode = new(HttpMethod.Get, "/api/app/growth/growthInfos");
}

public class PortkeyProvider : IPortkeyProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<GraphQLOptions> _graphQlOptions;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly ILogger<IPortkeyProvider> _logger;

    public PortkeyProvider(IHttpProvider httpProvider, IOptionsMonitor<GraphQLOptions> graphQlOptions,
        IOptionsMonitor<RankingOptions> rankingOptions, ILogger<IPortkeyProvider> logger)
    {
        _httpProvider = httpProvider;
        _graphQlOptions = graphQlOptions;
        _rankingOptions = rankingOptions;
        _logger = logger;
    }

    public async Task<Tuple<string, string>> GetShortLingAsync(string chainId, string token)
    {
        var resp = await _httpProvider.InvokeAsync<ShortLinkResponse>("", ReferralApi.ShortLink,
            param: new Dictionary<string, string> { ["projectCode"] = CommonConstant.ProjectCode },
            header: new Dictionary<string, string> { ["Authorization"] = token },
            withInfoLog: false, withDebugLog: false);
        return new Tuple<string, string>(resp?.ShortLinkCode ?? string.Empty, resp?.InviteCode ?? string.Empty);
    }

    public async Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime, long endTime, int skipCount, int maxResultCount)
    {
        var url = _graphQlOptions.CurrentValue.PortkeyConfiguration;
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
        query($caHashes: [String], $methodName: String, $referralCodes: [String], $projectCode: String, $startTime: Long!, $endTime: Long!, $skipCount: Int!, $maxResultCount: Int!) {
            referralInfoPage(dto: {
                caHashes: $caHashes,
                referralCodes: $referralCodes,
                projectCode: $projectCode,
                methodName: $methodName,
                startTime: $startTime,
                endTime: $endTime,
                skipCount: $skipCount,
                maxResultCount: $maxResultCount
            }) {
                totalRecordCount
                data {
                    caHash
                    referralCode
                    projectCode
                    methodName
                    timestamp
                }
            }
        }",
            Variables = new
            {
                caHashes = new List<string>(), 
                methodName = CommonConstant.CreateAccountMethodName, 
                referralCodes = new List<string>(), 
                projectCode = CommonConstant.ProjectCode, 
                startTime = startTime, 
                endTime = endTime,
                skipCount = skipCount, 
                maxResultCount = maxResultCount
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<IndexerReferralInfo>(request);
        return graphQlResponse.Data.ReferralInfoPage.Data;
    }

    public async Task<List<ReferralCodeInfo>> GetReferralCodeCaHashAsync(List<string> referralCodes)
    {
        try
        {
            var domain = _rankingOptions.CurrentValue.ReferralDomain;
            var referralCodesString = string.Join("&referralCodes=", referralCodes);
            var url = $"{domain}{ReferralApi.ReferralCode.Path}?projectCode=13027&referralCodes={referralCodesString}&skipCount=0&maxResultCount={referralCodes.Count}";
            var resp = await _httpProvider.InvokeAsync<ReferralCodeResponse>(ReferralApi.ReferralCode.Method, url);
            return resp.Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetReferralCodeCaHashAsyncException count {0}", referralCodes.Count);
        }

        return new List<ReferralCodeInfo>();
    }
}