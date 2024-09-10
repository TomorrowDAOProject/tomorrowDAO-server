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
    Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime);
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

    public PortkeyProvider(IHttpProvider httpProvider, IOptionsMonitor<ShortLinkOptions> shortLinkOptions)
    {
        _httpProvider = httpProvider;
        _shortLinkOptions = shortLinkOptions;
    }

    public async Task<Tuple<string, string>> GetShortLingAsync(string chainId, string token)
    {
        AssertHelper.IsTrue(_shortLinkOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var domain));
        var projectCode = _shortLinkOptions.CurrentValue.ProjectCode;
        var resp = await _httpProvider.InvokeAsync<ShortLinkResponse>(domain, ReferralApi.ShortLink,
            param: new Dictionary<string, string> { ["projectCode"] = projectCode },
            header: new Dictionary<string, string> { ["Authorization"] = token },
            withInfoLog: false, withDebugLog: false);
        return new Tuple<string, string>(resp?.ReferralLink ?? string.Empty, resp?.ReferralCode ?? string.Empty);
    }

    // todo wait port key change
    public async Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime)
    {
        var url = _graphQLOptions.CurrentValue.PortkeyConfiguration;
        var projectCode = _shortLinkOptions.CurrentValue.ProjectCode;
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
			    query($referralInfo:[String],$methodNames:[String],$referralCodes:Int!,$projectCode:String,startTime: Long!,endTime:Long!) {
                    referralInfo(dto: {referralInfo:$referralInfo,methodNames:$methodNames,referralCodes:$referralCodes,projectCode:$projectCode,startTime:$startTime,endTime:$endTime})
                    {
                         caHash,referralCode,projectCode,methodName,timestamp
                    }
                }",
            Variables = new
            {
                
            }
        };
    
        var graphQlResponse = await graphQlClient.SendQueryAsync<IndexerReferralInfo>(request);
        return graphQlResponse.Data.DataList;
    }
}