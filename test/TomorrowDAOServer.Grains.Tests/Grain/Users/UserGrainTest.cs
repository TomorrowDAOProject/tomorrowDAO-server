using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Telegram.Dto;
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

        var grainResultDto = await grain.CreateUser(new UserGrainDto
        {
            AppId = "AppId",
            UserId = UserId,
            UserName = "UserName",
            CaHash = Address1,
            AddressInfos = new List<AddressInfo>
            {
                new()
                {
                    ChainId = ChainIdAELF,
                    Address = Address1
                },
                new()
                {
                    ChainId = ChainIdtDVW,
                    Address = Address2
                }
            },
            CreateTime = DateTime.Now.Millisecond,
            ModificationTime = DateTime.Now.Millisecond
        });
        grainResultDto.ShouldNotBeNull();
        grainResultDto.Success.ShouldBeTrue();
        grainResultDto.Data.ShouldNotBeNull();
        grainResultDto.Data.UserId.ShouldBe(UserId);

        var resultDto = await grain.GetUser();
        resultDto.ShouldNotBeNull();
        resultDto.Success.ShouldBeTrue();
        resultDto.Data.ShouldNotBeNull();
        resultDto.Data.UserId.ShouldBe(UserId);
    }

    [Fact]
    public async Task CreateUserTest_UserInfo()
    {
        var grain = Cluster.GrainFactory.GetGrain<IUserGrain>(UserId);

        var userInfoStr = CreateUserInfoStr();
        var grainResultDto = await grain.CreateUser(CreateUserGrainDtoWithUserInfo(userInfoStr));
        grainResultDto.ShouldNotBeNull();
        grainResultDto.Success.ShouldBeTrue();
        grainResultDto.Data.ShouldNotBeNull();
        grainResultDto.Data.UserInfo.ShouldBe(userInfoStr);

        var resultDto = await grain.GetUser();
        resultDto.ShouldNotBeNull();
        resultDto.Success.ShouldBeTrue();
        resultDto.Data.ShouldNotBeNull();
        resultDto.Data.UserInfo.ShouldBe(userInfoStr);
    }

    [Fact]
    public async Task UpdateUserTest()
    {
        var grain = Cluster.GrainFactory.GetGrain<IUserGrain>(UserId);

        var userInfoStr = CreateUserInfoStr();
        var updateUser = await grain.UpdateUser(CreateUserGrainDtoWithUserInfo(userInfoStr));
        updateUser.ShouldNotBeNull();
        updateUser.Success.ShouldBeFalse();
        updateUser.Message.ShouldBe("User not exists.");
        
        var grainResultDto = await grain.CreateUser(CreateUserGrainDtoWithUserInfo(userInfoStr));
        updateUser = await grain.UpdateUser(CreateUserGrainDtoWithUserInfo(userInfoStr));
        updateUser.ShouldNotBeNull();
        updateUser.Success.ShouldBeTrue();
        updateUser.Data.ShouldNotBeNull();
        updateUser.Data.UserInfo.ShouldBe(userInfoStr);
        
        
    }

    private string CreateUserInfoStr()
    {
        var userInfoStr = JsonConvert.SerializeObject(new TelegramAuthDataDto
        {
            Id = "GuardianIdentifier",
            UserName = "TGUserName",
            AuthDate = DateTime.UtcNow.ToLongTimeString(),
            FirstName = "TGFirstName",
            LastName = "TGLastName",
            Hash = "TGHash",
            PhotoUrl = "PhotoUrl"
        });

        return userInfoStr;
    }

    private UserGrainDto CreateUserGrainDtoWithUserInfo(string userInfoStr)
    {
        return new UserGrainDto
        {
            AppId = "AppId",
            UserId = UserId,
            UserName = "UserName",
            CaHash = Address1,
            AddressInfos = new List<AddressInfo>
            {
                new()
                {
                    ChainId = ChainIdAELF,
                    Address = Address1
                },
                new()
                {
                    ChainId = ChainIdtDVW,
                    Address = Address1
                }
            },
            CreateTime = DateTime.Now.Millisecond,
            ModificationTime = DateTime.Now.Millisecond,
            GuardianIdentifier = "GuardianIdentifier",
            Address = Address1,
            Extra = JsonConvert.SerializeObject(new UserExtraDto
            {
                ConsecutiveLoginDays = 1,
                LastModifiedTime = DateTime.Now,
                DailyPointsClaimedStatus = new bool[]
                {
                },
                HasVisitedVotePage = true
            }),
            UserInfo = userInfoStr
        };
    }
}