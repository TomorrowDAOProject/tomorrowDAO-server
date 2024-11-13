using System.Collections.Generic;

namespace TomorrowDAOServer.Common;

public class GuardianIdentifiersResponse
{
    public GuardianIdentifierList GuardianList { get; set; }
}

public class GuardianIdentifierList
{
    public List<GuardianIdentifier> Guardians { get; set; }
}

public class GuardianIdentifier
{
    public string IdentifierHash { get; set; }
    public string Identifier { get; set; }
}