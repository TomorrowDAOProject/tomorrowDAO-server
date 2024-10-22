using System.Collections.Generic;
using Orleans;

namespace TomorrowDAOServer.Common;

[GenerateSerializer]
public class BpInfoDto
{
    [Id(0)] public List<string> AddressList { get; set; }
    [Id(1)] public long Round { get; set; }
}