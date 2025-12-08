using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Work;

public class WorkerReRunProposalOptions
{
    public List<string> ReRunProposalIds { get; set; } = new();
    public string ChainId { get; set; }
}