using AutoMapper;
using Memeup.Api.Domain.Enums;
using DomainLevel = Memeup.Api.Domain.Levels.Level;

namespace Memeup.Api.Features.Levels;

public class LevelMappingProfile : Profile
{
    public LevelMappingProfile()
    {
        CreateMap<DomainLevel, LevelDto>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (int)s.Status));

        CreateMap<LevelCreateDto, DomainLevel>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status));

        CreateMap<LevelUpdateDto, DomainLevel>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status));
    }
}
