using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class DaoAliasOptions
{
    public IDictionary<string, string> CharReplacements { get; set; } = new Dictionary<string, string>();
    public ISet<string> FilteredChars { get; set; } = new HashSet<string>();
}