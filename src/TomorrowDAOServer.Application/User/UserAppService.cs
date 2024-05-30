using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserAppService : TomorrowDAOServerAppService, IUserAppService
{
    private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;

    public UserAppService(INESTRepository<UserIndex, Guid> userIndexRepository)
    {
        _userIndexRepository = userIndexRepository;
    }

    public async Task CreateUserAsync(UserDto user)
    {
        try
        {
            await _userIndexRepository.AddOrUpdateAsync(ObjectMapper.Map<UserDto, UserIndex>(user));
            Logger.LogInformation("Create user success, userId:{userId}, appId:{appId}", user.UserId, user.AppId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Create user error, userId:{userId}, appId:{appId}", user.UserId, user.AppId);
        }
    }

    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(t=>t.UserId).Value(userId)));

        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, users) = await _userIndexRepository.GetListAsync(Filter);
        if (totalCount != 1)
        {
            throw new UserFriendlyException("User count: {count}");
        }

        return ObjectMapper.Map<UserIndex, UserDto>(users.First());
    }
}