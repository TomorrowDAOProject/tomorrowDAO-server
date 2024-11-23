using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Indexer;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Providers;

public interface IPortkeyProvider
{
    Task<Tuple<string, string>> GetShortLinkAsync(string chainId, string token);
    Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime, long endTime, int skipCount, int maxResultCount);
    Task<List<ReferralCodeInfo>> GetReferralCodeCaHashAsync(List<string> referralCodes);
    Task<List<CaHolderTransactionDetail>> GetCaHolderTransactionAsync(string chainId, string caAddress);
    Task<HolderInfoIndexerDto> GetHolderInfosAsync(string caHash);
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

    public async Task<Tuple<string, string>> GetShortLinkAsync(string chainId, string token)
    {
        var resp = await _httpProvider.InvokeAsync<ShortLinkResponse>("", ReferralApi.ShortLink,
            param: new Dictionary<string, string> { ["projectCode"] = CommonConstant.ProjectCode },
            header: new Dictionary<string, string> { ["Authorization"] = token },
            withInfoLog: false, withDebugLog: false);
        return new Tuple<string, string>(resp?.ShortLinkCode ?? string.Empty, resp?.InviteCode ?? string.Empty);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
        Message = "GetSyncReferralListAsync error", ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"methodName", "startTime", "endTime", "skipCount", "maxResultCount"})]
    public virtual async Task<List<IndexerReferral>> GetSyncReferralListAsync(string methodName, long startTime, long endTime, int skipCount, int maxResultCount)
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

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
        Message = "GetReferralCodeCaHashAsync error", ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"referralCodes"})]
    public virtual async Task<List<ReferralCodeInfo>> GetReferralCodeCaHashAsync(List<string> referralCodes)
    {
        var domain = _rankingOptions.CurrentValue.ReferralDomain;
        var referralCodesString = string.Join("&referralCodes=", referralCodes);
        var url = $"{domain}{ReferralApi.ReferralCode.Path}?projectCode=13027&referralCodes={referralCodesString}&skipCount=0&maxResultCount={referralCodes.Count}";
        var resp = await _httpProvider.InvokeAsync<ReferralCodeResponse>(ReferralApi.ReferralCode.Method, url);
        return resp.Data;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
        Message = "GetCaHolderTransactionAsync error", ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"chainId", "caAddress"})]
    public async Task<List<CaHolderTransactionDetail>> GetCaHolderTransactionAsync(string chainId, string caAddress)
    {
        var url = _graphQlOptions.CurrentValue.PortkeyConfiguration;
            using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
            var request = new GraphQLRequest
            {
                Query = @"
                    query($chainId: String, $symbol: String, $caAddressInfos: [CAAddressInfo!], $methodNames: [String!], $transactionId: String, $startBlockHeight: Long!, $endBlockHeight: Long!, $startTime: Long, $endTime: Long, $skipCount: Int!, $maxResultCount: Int!) {
                        caHolderTransaction(dto: {
                            chainId: $chainId,
                            symbol: $symbol,
                            caAddressInfos: $caAddressInfos,
                            methodNames: $methodNames,
                            transactionId: $transactionId,
                            startBlockHeight: $startBlockHeight,
                            endBlockHeight: $endBlockHeight,
                            startTime: $startTime,
                            endTime: $endTime,
                            skipCount: $skipCount,
                            maxResultCount: $maxResultCount
                        }) {
                            data {
                                timestamp
                            }
                            totalRecordCount
                        }
                    }",
                Variables = new
                {
                    chainId = "",
                    symbol = "",
                    caAddressInfos = new List<dynamic>
                    {
                        new
                        {
                            chainId = CommonConstant.MainChainId,
                            caAddress = caAddress
                        },
                        new
                        {
                            chainId = chainId,
                            caAddress = caAddress
                        }
                    },
                    methodNames = CommonConstant.CreateAccountMethodName,
                    transactionId = "",
                    startBlockHeight = 0L,
                    endBlockHeight = 0L,
                    startTime = 0L,
                    endTime = 0L,
                    skipCount = 0,
                    maxResultCount = 1
                }
            };


            var graphQlResponse = await graphQlClient.SendQueryAsync<IndexerCaHolderTransaction>(request);
            return graphQlResponse.Data.CaHolderTransaction.Data;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, 
        Message = "GetHolderInfosAsync error", ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"caHash"})]
    public async Task<HolderInfoIndexerDto> GetHolderInfosAsync(string caHash)
    {
        var url = _graphQlOptions.CurrentValue.PortkeyConfiguration;
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData}}
                }",
            Variables = new
            {
                caHash, skipCount = 0, maxResultCount = 10
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<HolderInfoIndexerDto>(request);
        return graphQlResponse.Data;
    }
}