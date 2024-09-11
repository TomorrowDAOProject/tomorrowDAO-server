using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
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
}

public static class ReferralApi
{
    public static readonly ApiInfo ShortLink = new(HttpMethod.Get, "/api/app/growth/shortLink");
}

public class PortkeyProvider : IPortkeyProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<ShortLinkOptions> _shortLinkOptions;
    private readonly IOptionsMonitor<GraphQLOptions> _graphQLOptions;

    public PortkeyProvider(IHttpProvider httpProvider, IOptionsMonitor<ShortLinkOptions> shortLinkOptions, 
        IOptionsMonitor<GraphQLOptions> graphQlOptions)
    {
        _httpProvider = httpProvider;
        _shortLinkOptions = shortLinkOptions;
        _graphQLOptions = graphQlOptions;
    }

    public async Task<Tuple<string, string>> GetShortLingAsync(string chainId, string token)
    {
        AssertHelper.IsTrue(_shortLinkOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var domain));
        var projectCode = _shortLinkOptions.CurrentValue.ProjectCode;
        var resp = await _httpProvider.InvokeAsync<ShortLinkResponse>(domain, ReferralApi.ShortLink,
            param: new Dictionary<string, string> { ["projectCode"] = projectCode },
            header: new Dictionary<string, string> { ["Authorization"] = token },
            withInfoLog: false, withDebugLog: false);
        return new Tuple<string, string>(resp?.ShortLinkCode ?? string.Empty, resp?.InviteCode ?? string.Empty);
    }

    public async Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime, long endTime, int skipCount, int maxResultCount)
    {
        var url = _graphQLOptions.CurrentValue.PortkeyConfiguration;
        var projectCode = _shortLinkOptions.CurrentValue.ProjectCode;
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
                projectCode = projectCode, 
                startTime = startTime, 
                endTime = endTime,
                skipCount = skipCount, 
                maxResultCount = maxResultCount
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<IndexerReferralInfo>(request);
        return graphQlResponse.Data.ReferralInfoPage.Data;
    }
}