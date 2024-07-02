using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO.Provider;

public class DAOProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;
    private readonly ILogger<DAOProvider> _logger;
    private readonly IDAOProvider _provider;

    public DAOProviderTest(ITestOutputHelper output) : base(output)
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

    [Fact]
    public async Task GetDaoListByDaoIds_Test()
    {
        _daoIndexRepository
            .GetListAsync(Arg.Any<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>(), null, null,
                Arg.Any<SortOrder>(), 1000, 0, null)
            .Returns(Task.FromResult(new Tuple<long, List<DAOIndex>>(10, new List<DAOIndex>(){ new DAOIndex()})));
        
        var chainId = ChainIdAELF;
        var daoIds = new List<string>() { "", ""};
        var list = await _provider.GetDaoListByDaoIds(chainId, daoIds);
        list.ShouldNotBeNull();
        list.Count.ShouldBe(1);
        
    }
}