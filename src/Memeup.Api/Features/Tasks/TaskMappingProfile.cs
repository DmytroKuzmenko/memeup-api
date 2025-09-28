using AutoMapper;
using System.Linq;
using Memeup.Api.Domain.Enums;
using DomainTask = Memeup.Api.Domain.Tasks.TaskItem;
using DomainTaskOption = Memeup.Api.Domain.Tasks.TaskOption;

namespace Memeup.Api.Features.Tasks;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<DomainTask, TaskDto>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (int)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (int)s.Type))
            .ForMember(d => d.Options, m => m.MapFrom(s => s.Options.OrderBy(o => o.OrderIndex)));

        CreateMap<TaskCreateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type))
            .ForMember(d => d.Options, m => m.Ignore());

        CreateMap<TaskUpdateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type))
            .ForMember(d => d.Options, m => m.Ignore());

        CreateMap<DomainTaskOption, TaskOptionDto>();

        CreateMap<TaskOptionDto, DomainTaskOption>()
            .ForMember(d => d.TaskId, m => m.Ignore())
            .ForMember(d => d.Task, m => m.Ignore())
            .ForMember(d => d.OrderIndex, m => m.Ignore());
    }
}
