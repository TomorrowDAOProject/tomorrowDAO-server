using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Orleans;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.User.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string AppId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string CaHash { get; set; }
    public List<AddressInfo> AddressInfos { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public string Address { get; set; }  //CAAddress or EOA
    public string Extra { get; set; }
    public string UserInfo { get; set; }
    
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

[GenerateSerializer]
public class AddressInfo
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string Address { get; set; }
}