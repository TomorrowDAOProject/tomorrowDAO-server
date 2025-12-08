using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.DAO;

public class PermissionInfo
{
    [Keyword] public string Where { get; set; }
    [Keyword] public string What { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public PermissionType PermissionType { get; set; }
    [Keyword] public string Who { get; set; }
}