using AutoMapper;
using Memeup.Api.Domain.Enums;
using DomainSection = Memeup.Api.Domain.Sections.Section;

namespace Memeup.Api.Features.Sections;

public class SectionMappingProfile : Profile
{
    public SectionMappingProfile()
    {
        CreateMap<DomainSection, SectionDto>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (int)s.Status));

        CreateMap<SectionCreateDto, DomainSection>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status));

        CreateMap<SectionUpdateDto, DomainSection>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status));
    }
}
