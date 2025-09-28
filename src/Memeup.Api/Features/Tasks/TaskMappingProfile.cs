using AutoMapper;
using Memeup.Api.Domain.Enums;
using DomainTask = Memeup.Api.Domain.Tasks.TaskItem;

namespace Memeup.Api.Features.Tasks;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<DomainTask, TaskDto>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (int)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (int)s.Type));

        CreateMap<TaskCreateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type));

        CreateMap<TaskUpdateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type));
    }
}
