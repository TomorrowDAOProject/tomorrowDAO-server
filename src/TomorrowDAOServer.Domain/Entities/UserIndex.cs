using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class UserIndex : AbstractEntity<Guid>, IIndexBuild
{
    [Keyword]  public override Guid Id { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public Guid UserId { get; set; }
    public string UserName { get; set; }
    [Keyword]  public string CaHash { get; set; }
    public List<UserAddressInfo> AddressInfos { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
    [Keyword]
    public string Address { get; set; }  //CAAddress or EOA
    public string Extra { get; set; }
    public string UserInfo { get; set; }
}

public class UserAddressInfo
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}