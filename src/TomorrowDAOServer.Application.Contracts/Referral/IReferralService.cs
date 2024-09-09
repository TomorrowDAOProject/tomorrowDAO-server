using System.Threading.Tasks;

namespace TomorrowDAOServer.Referral;

public interface IReferralService
{
    Task<string> GetLinkAsync(string token, string chainId);
}