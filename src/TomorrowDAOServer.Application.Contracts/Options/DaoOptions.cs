using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Options;

public class DaoOptions
{
    public ISet<string> FilteredDaoNames { get; set; } = new HashSet<string>();
    public Dictionary<VerifiedType, List<string>> VerifiedTopDaoNames { get; set; } = new();
    
    public List<string> GetTopDaoNames()
    {
        return VerifiedTopDaoNames
            .OrderBy(kv => kv.Key)
            .SelectMany(kv => kv.Value) 
            .Distinct() 
            .ToList();
    }

    public Dictionary<string, VerifiedType> GetVerifiedTypeDic()
    {
        return VerifiedTopDaoNames
            .SelectMany(kv => kv.Value.Select(value => new { Key = value, Value = kv.Key }))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}