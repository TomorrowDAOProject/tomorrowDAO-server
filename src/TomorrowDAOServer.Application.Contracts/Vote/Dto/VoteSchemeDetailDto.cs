using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Vote.Dto;

public class VoteSchemeDetailDto
{
    public List<VoteSchemeInfoDto> VoteSchemeList  { get; set; }
}

public class VoteSchemeInfoDto
{
    public string VoteSchemeId { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public VoteMechanism VoteMechanism { get; set; }
}