using TomorrowDAOServer.Grains.State.Token;
using TomorrowDAOServer.Token;
using Orleans;

namespace TomorrowDAOServer.Grains.Grain.Token;

public interface ITokenExchangeGrain : IGrainWithStringKey
{
    Task<TokenExchangeGrainDto> GetAsync();
    Task SetAsync(TokenExchangeGrainDto tokenExchangeGrainDto);
}

public class TokenExchangeGrain : Grain<TokenExchangeState>, ITokenExchangeGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task<TokenExchangeGrainDto> GetAsync()
    {
        return await Task.FromResult(new TokenExchangeGrainDto
        {
            LastModifyTime = State.LastModifyTime,
            ExpireTime = State.ExpireTime,
            ExchangeInfos = State.ExchangeInfos
        });
    }

    public async Task SetAsync(TokenExchangeGrainDto tokenExchangeGrainDto)
    {
        State.LastModifyTime = tokenExchangeGrainDto.LastModifyTime;
        State.ExpireTime = tokenExchangeGrainDto.ExpireTime;
        State.ExchangeInfos = tokenExchangeGrainDto.ExchangeInfos;
        await WriteStateAsync();
    }
}