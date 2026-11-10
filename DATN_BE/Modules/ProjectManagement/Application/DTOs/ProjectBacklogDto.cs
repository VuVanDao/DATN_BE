using DATN_BE.Modules.Task.Application.DTOs;

namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class ProjectBacklogDto
    {
        public Guid ProjectId { get; set; }
        public Guid? BoardId { get; set; }
        public string? BoardTitle { get; set; }
        public Guid? DefaultScrumColumnId { get; set; }
        public SprintDto? ActiveSprint { get; set; }
        public List<SprintDto> FutureSprints { get; set; } = new();
        public List<CardDto> BacklogCards { get; set; } = new();
        public int BacklogTotalCards { get; set; }
        public int BacklogTotalStoryPoints { get; set; }
    }
}
