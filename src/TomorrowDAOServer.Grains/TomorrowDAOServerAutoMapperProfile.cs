using AutoMapper;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Grains.State.Token;
using TomorrowDAOServer.Grains.State.Users;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Grains;

public class TomorrowDAOServerGrainsAutoMapperProfile : Profile
{
    public TomorrowDAOServerGrainsAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserState>().ReverseMap();
        CreateMap<UserState, UserDto>().ReverseMap();
        CreateMap<TokenGrainDto, TokenState>().ReverseMap();
    }
}