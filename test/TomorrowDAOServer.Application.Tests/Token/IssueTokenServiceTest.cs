using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Token;

public class IssueTokenServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IIssueTokenService _issueTokenService;

    public IssueTokenServiceTest(ITestOutputHelper output) : base(output)
    {
        _issueTokenService = ServiceProvider.GetRequiredService<IIssueTokenService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task IssueTokensAsyncTest()
    {
        var response = await _issueTokenService.IssueTokensAsync(new IssueTokensInput
        {
            ChainId = ChainIdAELF,
            Symbol = ELF
        });
        response.ShouldNotBeNull();
        response.Symbol.ShouldBe(ELF);
        response.ProxyAccountHash.ShouldBe(TransactionHash.ToHex());
        
        response = await _issueTokenService.IssueTokensAsync(new IssueTokensInput
        {
            ChainId = ChainIdAELF,
            Symbol = ELF,
            Amount = 100,
            ToAddress = Address1,
            Memo = "IssueTesting"
        });
        response.ShouldNotBeNull();
        response.Symbol.ShouldBe(ELF);
        response.ProxyAccountHash.ShouldBe(TransactionHash.ToHex());
        response.ProxyArgs.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task IssueTokensAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _issueTokenService.IssueTokensAsync(new IssueTokensInput());
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }
}