using System.Threading.Tasks;
using TomorrowDAOServer.Token.Dto;

namespace TomorrowDAOServer.Token;

public interface ITransferTokenService
{
    Task<TransferTokenResponse> TransferTokenAsync(TransferTokenInput input);
    Task<TokenClaimRecord> GetTransferTokenStatusAsync(TransferTokenStatusInput input);
}