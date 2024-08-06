using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.State.Token;

namespace TomorrowDAOServer.Grains.Grain.Token;

public interface ITokenGrain : IGrainWithStringKey
{
    Task<TokenInfoDto> GetTokenInfoAsync();
    Task SetTokenInfoAsync(TokenInfoDto tokenInfo);
}

public class TokenGrain : Grain<ExplorerTokenState>, ITokenGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }
    

    public Task<TokenInfoDto> GetTokenInfoAsync()
    {
        return Task.FromResult(new TokenInfoDto
        {
            Id = State.Id,
            ChainId = State.ChainId,
            IssueChainId = State.IssueChainId,
            TxId = State.TxId,
            ContractAddress = State.ContractAddress,
            Decimals = State.Decimals,
            Supply = State.Supply,
            Holders = State.Holders,
            Symbol = State.Symbol,
            Name = State.Name,
            TotalSupply = State.TotalSupply,
            Transfers = State.Transfers,
            LastUpdateTime = State.LastUpdateTime
        });
    }

    public async Task SetTokenInfoAsync(TokenInfoDto tokenInfo)
    {
        State.Id = tokenInfo.Id;
        State.ChainId = tokenInfo.ChainId;
        State.IssueChainId = tokenInfo.IssueChainId;
        State.TxId = tokenInfo.TxId;
        State.ContractAddress = tokenInfo.ContractAddress;
        State.Decimals = tokenInfo.Decimals;
        State.Supply = tokenInfo.Supply;
        State.Holders = tokenInfo.Holders;
        State.Symbol = tokenInfo.Symbol;
        State.Name = tokenInfo.Name;
        State.TotalSupply = tokenInfo.TotalSupply;
        State.Transfers = tokenInfo.Transfers;
        State.LastUpdateTime = tokenInfo.LastUpdateTime;
        await WriteStateAsync();
    }
}