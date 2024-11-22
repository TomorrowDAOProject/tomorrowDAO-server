using System;
using System.Collections.Generic;
using Orleans;

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
}

[GenerateSerializer]
public class AddressInfo
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string Address { get; set; }
}