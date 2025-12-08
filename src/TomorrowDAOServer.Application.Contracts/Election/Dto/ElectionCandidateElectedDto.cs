using System;

namespace TomorrowDAOServer.Election.Dto;

public class ElectionCandidateElectedDto
{
    public string Id { get; set; }
    public string DaoId { get; set; }
    public long PreTermNumber { get; set; }
    public long NewNumber { get; set; }
    public DateTime CandidateElectedTime { get; set; }
    
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
}

public class GetCandidateElectedRecordsInput : ElectionGraphqlQueryParams
{
}