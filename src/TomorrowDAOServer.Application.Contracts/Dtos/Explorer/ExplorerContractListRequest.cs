using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerContractListRequest : ExplorerPagerRequest
{
}

public class ExplorerContractListResponse
{
    public int Total { get; set; }
    public List<ExplorerContractDto> List { get; set; }
}

public class ExplorerContractDto
{
    public int Id { get; set; }
    public string ContractName { get; set; }
    public string Address { get; set; }
    public string Author { get; set; }
    public string Category { get; set; }
    public bool IsSystemContract { get; set; }
    public string Serial { get; set; }
    public string Version { get; set; }
    public DateTime UpdateTime { get; set; }
}