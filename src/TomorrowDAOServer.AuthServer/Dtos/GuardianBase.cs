
namespace TomorrowDAOServer.Auth.Dtos;

public class GuardianBase
{
    public string CreateChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
}

public class GuardianBaseListDto
{
    public List<GuardianInfoBase> Guardians { get; set; }
}

public class ManagerInfoDBase
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}

public class GuardianInfoBase
{
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string GuardianIdentifier { get; set; }
    public string VerifierId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public bool IsLoginGuardian { get; set; }
    public string Type { get; set; }
    public string TransactionId { get; set; }
    public bool VerifiedByZk { get; set; }
    public bool ManuallySupportForZk { get; set; }
    
    public string PoseidonIdentifierHash { get; set; }
}