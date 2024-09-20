using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp.Users;

namespace TomorrowDAOServer.User;

public class UserService : TomorrowDAOServerAppService, IUserService
{
    private readonly IUserProvider _userProvider;
    private readonly IOptionsMonitor<UserOptions> _userOptions;
    private readonly IUserVisitProvider _userVisitProvider;
    private readonly IUserVisitSummaryProvider _userVisitSummaryProvider;

    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider)
    {
        _userProvider = userProvider;
        _userOptions = userOptions;
        _userVisitProvider = userVisitProvider;
        _userVisitSummaryProvider = userVisitSummaryProvider;
    }

    public async Task<UserSourceReportResultDto> UserSourceReportAsync(string chainId, string source)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var userSourceList = _userOptions.CurrentValue.UserSourceList;
        if (!userSourceList.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            return new UserSourceReportResultDto
            {
                Success = false, Reason = "Invalid source."
            };
        }
        var matchedSource = userSourceList.FirstOrDefault(s => 
            string.Equals(s, source, StringComparison.OrdinalIgnoreCase));
        var now = TimeHelper.GetTimeStampInMilliseconds();
        var visitType = UserVisitType.Votigram;
        await _userVisitProvider.AddOrUpdateAsync(new UserVisitIndex
        {
            Id = GuidHelper.GenerateId(address, chainId, visitType.ToString(), matchedSource, now.ToString()),
            ChainId = chainId,
            Address = address,
            UserVisitType = visitType,
            Source = matchedSource!,
            VisitTime = now
        });
        var summaryId = GuidHelper.GenerateId(address, chainId, visitType.ToString(), matchedSource);
        var visitSummaryIndex = await _userVisitSummaryProvider.GetByIdAsync(summaryId);
        if (visitSummaryIndex == null)
        {
            visitSummaryIndex = new UserVisitSummaryIndex
            {
                Id = summaryId,
                ChainId = chainId,
                Address = address,
                UserVisitType = visitType,
                Source = matchedSource!,
                CreateTime = now,
                ModificationTime = now
            };
        }
        else
        {
            visitSummaryIndex.ModificationTime = now;
        }
        await _userVisitSummaryProvider.AddOrUpdateAsync(visitSummaryIndex);
        
        return new UserSourceReportResultDto
        {
            Success = true
        };
    }
}