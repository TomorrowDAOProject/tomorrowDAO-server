using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Vote.Index;

public class IndexerVoteSchemeResult : IndexerCommonResult<List<IndexerVoteSchemeInfo>>
{
}

public class IndexerVoteSchemeInfo
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string VoteSchemeId { get; set; }
    public VoteMechanism VoteMechanism { get; set; }
    public bool WithoutLockToken { get; set; }
    public VoteStrategy VoteStrategy { get; set; }
}

