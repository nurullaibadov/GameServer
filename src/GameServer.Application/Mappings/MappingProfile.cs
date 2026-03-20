using AutoMapper;
using GameServer.Application.DTOs.Auth;
using GameServer.Application.DTOs.Game;
using GameServer.Domain.Entities;

namespace GameServer.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserInfoDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));
        CreateMap<Game, GameDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));
    }
}
