using System.Threading.Tasks;
using TomorrowDAOServer.Token.Dto;

namespace TomorrowDAOServer.Token;

public interface IIssueTokenService
{
    Task<IssueTokenResponse> IssueTokensAsync(IssueTokensInput input);
}