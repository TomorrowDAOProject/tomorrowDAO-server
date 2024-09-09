using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Referral;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ReferralService : ApplicationService, IReferralService
{
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IReferralLinkProvider _referralLinkProvider;
    private readonly IUserProvider _userProvider;
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<ShortLinkOptions> _shortLinkOptions;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IReferralLinkProvider referralLinkProvider,
        IUserProvider userProvider, IHttpProvider httpProvider, IOptionsMonitor<ShortLinkOptions> shortLinkOptions)
    {
        _referralInviteProvider = referralInviteProvider;
        _referralLinkProvider = referralLinkProvider;
        _userProvider = userProvider;
        _httpProvider = httpProvider;
        _shortLinkOptions = shortLinkOptions;
    }

    public async Task<string> GetLinkAsync(string token, string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        var referralLink = await _referralLinkProvider.GetByInviterAsync(chainId, address);
        if (referralLink != null)
        {
            return referralLink.ReferralLink;
        }

        AssertHelper.IsTrue(_shortLinkOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var domain));
        var projectCode = _shortLinkOptions.CurrentValue.ProjectCode;
        var resp = await _httpProvider.InvokeAsync<ShortLinkResponse>(domain, ReferralApi.ShortLink,
            param: new Dictionary<string, string> { ["projectCode"] = projectCode },
            header: new Dictionary<string, string> { ["Authorization"] = token },
            withInfoLog: false, withDebugLog: false);
        if (resp == null || resp.Link.IsNullOrEmpty())
        {
            return string.Empty;
        }
        
        await _referralLinkProvider.GenerateLinkAsync(chainId, address, resp.Link);
        return resp.Link;
    }
}

public static class ReferralApi
{
    public static readonly ApiInfo ShortLink = new(HttpMethod.Get, "/api/app/growth/shortLink");
}