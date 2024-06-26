using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using Xunit;

namespace TomorrowDAOServer.DAO.Provider;

public class DAOProviderTest 
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;
    private readonly ILogger<DAOProvider> _logger;
    private readonly DAOProvider _provider;

    public DAOProviderTest()
    {
        _graphQlHelper = Substitute.For<IGraphQlHelper>();
        _daoIndexRepository = Substitute.For<INESTRepository<DAOIndex, string>>();
        _logger = Substitute.For<ILogger<DAOProvider>>();
        _provider = new DAOProvider(_graphQlHelper, _logger, _daoIndexRepository);
    }

    [Fact]
    public async void GetMemberListAsync_Test()
    {
        _graphQlHelper.QueryAsync<IndexerCommonResult<PageResultDto<MemberDto>>>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerCommonResult<PageResultDto<MemberDto>>());
        var memberList = await _provider.GetMemberListAsync(new GetMemberListInput());
        memberList.ShouldNotBeNull();
    }
}