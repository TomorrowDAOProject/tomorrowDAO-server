using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.State.Users;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Grains.Grain.Users;

public interface IUserGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<UserGrainDto>> CreateUser(UserGrainDto input);
    Task<GrainResultDto<UserGrainDto>> UpdateUser(UserGrainDto input);
    Task<GrainResultDto<UserGrainDto>> GetUser();
}

public class UserGrain : Grain<UserState>, IUserGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IUserAppService _userAppService;

    public UserGrain(IObjectMapper objectMapper, IUserAppService userAppService)
    {
        _objectMapper = objectMapper;
        _userAppService = userAppService;
    }

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

    public async Task<GrainResultDto<UserGrainDto>> CreateUser(UserGrainDto input)
    {
        if (State.Id == Guid.Empty)
        {
            State.Id = this.GetPrimaryKey();
        }
        
        State.UserId = input.UserId;
        State.UserName = input.UserName;
        State.AddressInfos = input.AddressInfos;
        State.AppId = input.AppId;
        State.CaHash = input.CaHash;
        var now = DateTime.UtcNow.ToUtcMilliSeconds();
        State.CreateTime = State.CreateTime == 0 ? now : State.CreateTime;
        State.ModificationTime = now;
        State.GuardianIdentifier = input.GuardianIdentifier;
        State.Address = input.Address;
        State.Extra = input.Extra;
        State.UserInfo = input.UserInfo;

        await WriteStateAsync();

        await _userAppService.CreateUserAsync(_objectMapper.Map<UserState, UserDto>(State));
        
        return new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        };
    }
    
    public async Task<GrainResultDto<UserGrainDto>> UpdateUser(UserGrainDto input)
    {
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<UserGrainDto>
            {
                Success = false,
                Message = "User not exists."
            };
        }
        
        State.UserId = input.UserId;
        State.UserName = input.UserName;
        State.AddressInfos = input.AddressInfos;
        State.AppId = input.AppId;
        State.CaHash = input.CaHash;
        State.ModificationTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.GuardianIdentifier = input.GuardianIdentifier;
        State.Address = input.Address;
        State.Extra = input.Extra;
        State.UserInfo = input.UserInfo;

        await WriteStateAsync();

        return new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        };
    }

    public Task<GrainResultDto<UserGrainDto>> GetUser()
    {
        if (State.Id == Guid.Empty)
        {
            return Task.FromResult(new GrainResultDto<UserGrainDto>()
            {
                Success = false,
                Message = "User not exists."
            });
        }
        
        return Task.FromResult(new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        });
    }
}