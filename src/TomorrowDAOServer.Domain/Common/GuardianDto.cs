using System.Collections.Generic;

namespace TomorrowDAOServer.Common;

public class GuardianIdentifiersResponse
{
    public GuardianIdentifierList GuardianList { get; set; }
}

public class GuardianIdentifierList
{
    public List<Guardian> Guardians { get; set; }
}

public class Guardian
{
    public string IdentifierHash { get; set; }
    public string GuardianIdentifier { get; set; }
}