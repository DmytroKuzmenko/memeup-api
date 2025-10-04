using System;
using AutoMapper;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Tasks;
using DomainTask = Memeup.Api.Domain.Tasks.TaskItem;

namespace Memeup.Api.Features.Tasks;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<TaskOption, TaskOptionDto>();

        CreateMap<TaskOptionDto, TaskOption>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id ?? Guid.NewGuid()));

        CreateMap<DomainTask, TaskDto>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (int)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (int)s.Type))
            .ForMember(d => d.Options, m => m.MapFrom(s => s.Options));

        CreateMap<TaskCreateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type))
            .ForMember(d => d.Options, m => m.MapFrom(s => s.Options ?? Array.Empty<TaskOptionDto>()));

        CreateMap<TaskUpdateDto, DomainTask>()
            .ForMember(d => d.Status, m => m.MapFrom(s => (PublishStatus)s.Status))
            .ForMember(d => d.Type, m => m.MapFrom(s => (TaskType)s.Type))
            .ForMember(d => d.Options, m => m.MapFrom(s => s.Options ?? Array.Empty<TaskOptionDto>()));
    }
}
