using Newtonsoft.Json;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Grains.Grain.Users;

[GenerateSerializer]
public class UserGrainDto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string UserName { get; set; }
    [Id(3)] public string CaHash { get; set; }
    [Id(4)] public List<AddressInfo> AddressInfos { get; set; }
    [Id(5)] public long CreateTime { get; set; }
    [Id(6)] public long ModificationTime { get; set; }
    [Id(7)] public string GuardianIdentifier { get; set; }
    [Id(8)] public string Address { get; set; }  //CAAddress or EOA
    [Id(9)] public string Extra { get; set; }
    [Id(10)] public string UserInfo { get; set; }

    public UserExtraDto GetUserExtraDto()
    {
        return Extra.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<UserExtraDto>(Extra);
    }

    public void SetUserExtraDto(UserExtraDto userExtraDto)
    {
        if (userExtraDto == null)
        {
            return;
        }
        Extra = JsonConvert.SerializeObject(userExtraDto);
    }

    public TelegramAuthDataDto GetUserInfo()
    {
        return UserInfo.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<TelegramAuthDataDto>(UserInfo);
    }

    public void SetUserInfo(TelegramAuthDataDto telegramAuthDataDto)
    {
        if (telegramAuthDataDto == null)
        {
            return;
        }
        UserInfo = JsonConvert.SerializeObject(telegramAuthDataDto);
    }
}

public class UserExtraDto
{
    public int ConsecutiveLoginDays { get; set; } = 0;
    public DateTime LastModifiedTime { get; set; }
    public bool DailyLoginPointsStatus { get; set; } = false;
    public bool HasVisitedVotePage { get; set; } = false;
}