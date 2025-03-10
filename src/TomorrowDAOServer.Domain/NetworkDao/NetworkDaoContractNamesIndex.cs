using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoContractNamesIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ContractName { get; set; }
    [Keyword] public string ContractAddress { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string TxId { get; set; }
    public NetworkDaoContractNameActionEnum Action { get; set; }
    public DateTime CreateAt { get; set; }
    public string Category { get; set; }
    public bool IsSystemContract { get; set; }
    public string Serial { get; set; }
    public string Version { get; set; }
    public DateTime UpdateTime { get; set; }
}