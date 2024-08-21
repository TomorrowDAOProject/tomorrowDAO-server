using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Token;

public partial class TransferTokenServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITransferTokenService _transferTokenService;

    public TransferTokenServiceTest(ITestOutputHelper output) : base(output)
    {
        _transferTokenService = ServiceProvider.GetRequiredService<ITransferTokenService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTransferTokenOption());
        services.AddSingleton(MockAbpDistributedLock());
        services.AddSingleton(MockIDistributedCache());
    }
    
    [Fact]
    public async Task TransferTokenAsyncTest()
    {
        var userId = Guid.NewGuid();
        Login(userId);
        var transferTokenResponse  = await _transferTokenService.TransferTokenAsync(new TransferTokenInput
        {
            ChainId = ChainIdAELF,
            Symbol = ELF
        });
        transferTokenResponse.ShouldNotBeNull();
        transferTokenResponse.Status.ShouldBe(TransferTokenStatus.TransferInProgress);
    }

    [Fact]
    public async Task TransferTokenAsyncTest_AlreadyClaimed()
    {
        var userId = Guid.NewGuid();
        Login(userId, TransferTokenStatus.AlreadyClaimed.ToString());
        var transferTokenResponse  = await _transferTokenService.TransferTokenAsync(new TransferTokenInput
        {
            ChainId = ChainIdAELF,
            Symbol = ELF
        });
        transferTokenResponse.ShouldNotBeNull();
        transferTokenResponse.Status.ShouldBe(TransferTokenStatus.AlreadyClaimed);
    }

    [Fact]
    public async Task TransferTokenAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _transferTokenService.TransferTokenAsync(new TransferTokenInput());
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _transferTokenService.TransferTokenAsync(new TransferTokenInput
            {
                ChainId = ChainIdAELF
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }
    
    [Fact]
    public async Task TransferTokenAsyncTest_NotLoggedIn()
    {
        Login(Guid.Empty);
        
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _transferTokenService.TransferTokenAsync(new TransferTokenInput
            {
                ChainId = ChainIdAELF,
                Symbol = ELF
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldContain("User Address Not Found.");
    }
}