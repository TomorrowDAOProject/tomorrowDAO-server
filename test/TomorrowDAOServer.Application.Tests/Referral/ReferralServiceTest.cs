using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Referral.Provider;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Referral;

public class ReferralServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IReferralService _referralService;
    private readonly IReferralCycleProvider _referralCycleProvider;
    
    public ReferralServiceTest(ITestOutputHelper output) : base(output)
    {
        _referralService = Application.ServiceProvider.GetRequiredService<IReferralService>();
        _referralCycleProvider = Application.ServiceProvider.GetRequiredService<IReferralCycleProvider>();
    }

    [Fact]
    public async Task InviteDetailAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);

        await GenerateReferralCycleAsync();

        var inviteDetailDto = await _referralService.InviteDetailAsync(ChainIdAELF);
        inviteDetailDto.ShouldNotBeNull();
        inviteDetailDto.DuringCycle.ShouldBeTrue();
    }
}