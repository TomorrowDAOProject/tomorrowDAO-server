using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Common.Enum;

namespace TomorrowDAOServer.Entities;

public class ProposalIndex : ProposalBase, IIndexBuild
{
    public ProposalSourceEnum ProposalSource { get; set; } = ProposalSourceEnum.TMRWDAO;
}