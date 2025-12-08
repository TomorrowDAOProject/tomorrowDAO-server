using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Token.Index;
using GraphQL;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Token.Provider;

public interface IUserTokenProvider
{
    Task<List<IndexerUserToken>> GetUserTokens(string chainId, string address);
}

public class UserTokenProvider : IUserTokenProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;

    public UserTokenProvider(IGraphQlHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<List<IndexerUserToken>> GetUserTokens(string chainId, string address)
    {
        var response =  await _graphQlHelper.QueryAsync<IndexerUserTokens>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$address:String!) {
                    userTokens:getUserTokenInfos(input: {chainId:$chainId,address:$address}){
                        chainId,symbol,tokenName,imageUrl,decimals,balance
                    }
                }",
            Variables = new
            {
                chainId, address
            }
        });
        return response?.UserTokens ?? new List<IndexerUserToken>();
    }
}