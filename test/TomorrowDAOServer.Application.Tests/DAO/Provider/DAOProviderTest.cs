using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.DAO.Provider;

public class DAOProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ILogger<DAOProvider> _logger;
    private readonly IDAOProvider _daoProvider;

    public DAOProviderTest(ITestOutputHelper output) : base(output)
    {
        _daoProvider = ServiceProvider.GetRequiredService<IDAOProvider>();
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
        var daoIndices = await _daoProvider.GetDaoListByDaoIds(ChainIdAELF, new List<string>());
        daoIndices.ShouldBeEmpty();

        daoIndices = await _daoProvider.GetDaoListByDaoIds(ChainIdAELF, new List<string>() { DaoId });
        daoIndices.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSyncDAOListAsyncTest()
    {
        var daoList = await _daoProvider.GetSyncDAOListAsync(new GetChainBlockHeightInput
        {
            SkipCount = 0,
            MaxResultCount = 100,
            ChainId = ChainIdAELF,
            StartBlockHeight = 0,
            EndBlockHeight = 1000
        });
        daoList.ShouldNotBeNull();
        daoList.Count.ShouldBe(1);
        daoList[0].Id.ShouldBe(DaoId);
    }

    [Fact]
    public async Task GetAsyncTest()
    {
        var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = ChainIdAELF,
            DAOId = DaoId
        });
        daoIndex.ShouldBeNull();

        daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = ChainIdAELF,
            Alias = DAOName
        });
        daoIndex.ShouldBeNull();
    }

    [Fact]
    public async Task GetDAOListAsyncTest()
    {
        var (count, daoList) = await _daoProvider.GetDAOListAsync(new QueryPageInput
        {
            ChainId = ChainIdAELF,
            SkipCount = 0,
            MaxResultCount = 10
        }, new HashSet<string>() { DAOName });
        count.ShouldBe(0);
        daoList.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDAOListCountAsyncTest()
    {
        var count = await _daoProvider.GetDAOListCountAsync(new QueryPageInput
        {
            ChainId = ChainIdAELF,
            SkipCount = 0,
            MaxResultCount = 100
        }, new HashSet<string>() { DAOName });

        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetDAOListByNameAsyncTest()
    {
        var (count, daoIndices) = await _daoProvider.GetDAOListByNameAsync(ChainIdAELF, new List<string>());
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();

        (count, daoIndices) = await _daoProvider.GetDAOListByNameAsync(ChainIdAELF, new List<string>() { DAOName });
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMyOwneredDAOListAsyncTest()
    {
        var (count, daoIndices) = await _daoProvider.GetMyOwneredDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdAELF,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Managed
        }, Address1);
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetManagedDAOAsyncTest()
    {
        var (count, daoIndices) = await _daoProvider.GetManagedDAOAsync(new QueryMyDAOListInput(), new List<string>(), false);
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();

        (count, daoIndices) = await _daoProvider.GetManagedDAOAsync(new QueryMyDAOListInput(), new List<string>(), true);
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();
        
        (count, daoIndices) = await _daoProvider.GetManagedDAOAsync(new QueryMyDAOListInput(), new List<string>() {DaoId, DAOId}, false);
        count.ShouldBe(0);
        daoIndices.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMyParticipatedDaoListAsync()
    {
        var pageResultDto = await _daoProvider.GetMyParticipatedDaoListAsync(new GetParticipatedInput());
        pageResultDto.ShouldNotBeNull();
        pageResultDto.TotalCount.ShouldBe(1);
        pageResultDto.Data[0].Id.ShouldBe(DaoId);
    }
    
    [Fact]
    public async Task GetMyParticipatedDaoListAsync_Exception()
    {
        var pageResultDto = await _daoProvider.GetMyParticipatedDaoListAsync(new GetParticipatedInput
        {
            ChainId = ChainIdAELF,
            Address = "ThrowException",
            SkipCount = 0,
            MaxResultCount = 10
        });
        pageResultDto.ShouldNotBeNull();
        pageResultDto.TotalCount.ShouldBe(0);
        pageResultDto.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMemberAsyncTest()
    {
        var memberDto = await _daoProvider.GetMemberAsync(new GetMemberInput());
        memberDto.ShouldNotBeNull();
        memberDto.Address.ShouldBe(Address1);
        memberDto.DAOId.ShouldBe(DaoId);
        
        memberDto = await _daoProvider.GetMemberAsync(new GetMemberInput
        {
            ChainId = ChainIdAELF,
            DAOId = "ThrowException",
            Alias = null,
            Address = null
        });
        memberDto.ShouldNotBeNull();
        memberDto.Address.ShouldBeNull();
        memberDto.DAOId.ShouldBeNull(DaoId);
    }
}