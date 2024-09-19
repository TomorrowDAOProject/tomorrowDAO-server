using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Users;

public partial class UserGrainTest : TomorrowDAOServerGrainsTestsBase
{
    private static readonly Guid UserId = GuidHelper.UniqGuid();

    public UserGrainTest(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockUserAppService());
        services.AddSingleton(MockUserProvider());
    }
    
    [Fact]
    public async Task CreateUserTest()
    {
        var grain = Cluster.GrainFactory.GetGrain<IUserGrain>(UserId);

        // var grainResultDto = await grain.CreateUser(new UserGrainDto
        // {
        //     AppId = "AppId",
        //     UserId = UserId,
        //     UserName = "UserName",
        //     CaHash = Address1,
        //     AddressInfos = new List<AddressInfo> {
        //         new()
        //         {
        //             ChainId = ChainIdAELF,
        //             Address = Address1
        //         }, 
        //         new()
        //         {
        //             ChainId = ChainIdtDVW,
        //             Address = Address2
        //         }
        //     },
        //     CreateTime = DateTime.Now.Millisecond,
        //     ModificationTime = DateTime.Now.Millisecond
        // });
        // grainResultDto.ShouldNotBeNull();
        // grainResultDto.Success.ShouldBeTrue();
        // grainResultDto.Data.ShouldNotBeNull();
        // grainResultDto.Data.UserId.ShouldBe(UserId);
        //
        // var resultDto = await grain.GetUser();
        // resultDto.ShouldNotBeNull();
        // resultDto.Success.ShouldBeTrue();
        // resultDto.Data.ShouldNotBeNull();
        // resultDto.Data.UserId.ShouldBe(UserId);
    }
}