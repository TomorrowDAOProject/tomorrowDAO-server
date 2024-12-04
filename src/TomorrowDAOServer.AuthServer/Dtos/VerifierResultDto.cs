using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Auth.Dtos;

public class VerifierResultDto
{
    public bool IsVerified { get; set; }
    public string CaHash { get; set; }
    public string Address { get; set; }
    public string GuardianIdentifier { get; set; }
    public string CreateChainId { get; set; }
    public List<AddressInfo> AddressInfos { get; set; }
    public IActionResult ForbidResult { get; set; }
}