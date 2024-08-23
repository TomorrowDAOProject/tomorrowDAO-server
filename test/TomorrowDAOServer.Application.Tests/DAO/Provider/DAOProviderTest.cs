using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.DAO.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO.Provider;

public class DAOProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;
    private readonly ILogger<DAOProvider> _logger;
    private readonly IDAOProvider _daoProvider;

    public DAOProviderTest(ITestOutputHelper output) : base(output)
    {
        _graphQlHelper = Substitute.For<IGraphQlHelper>();
        _daoIndexRepository = Substitute.For<INESTRepository<DAOIndex, string>>();
        _daoProvider = ServiceProvider.GetRequiredService<IDAOProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockINESTRepositoryDAOIndex());
    }

    [Fact]
    public async void GetMemberListAsync_Test()
    {
        var memberList = await _daoProvider.GetMemberListAsync(new GetMemberListInput());
        memberList.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDaoListByDaoIds_Test()
    {
        var chainId = ChainIdAELF;
        var daoIds = new List<string>() { "", "" };
        var list = await _daoProvider.GetDaoListByDaoIds(chainId, daoIds);
        list.ShouldNotBeNull();
        list.Count.ShouldBe(1);
    }

    private INESTRepository<DAOIndex, string> MockINESTRepositoryDAOIndex()
    {
        var mock = new Mock<INESTRepository<DAOIndex, string>>();

        mock.Setup(o => o.GetListAsync(It.IsAny<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>(), null, null,
                It.IsAny<SortOrder>(), 1000, 0, null))
            .ReturnsAsync(
                new Tuple<long, List<DAOIndex>>(10, new List<DAOIndex>() { new DAOIndex() }));

        return mock.Object;
    }
}