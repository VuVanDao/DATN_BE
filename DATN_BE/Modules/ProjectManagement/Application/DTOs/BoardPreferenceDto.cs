namespace DATN_BE.Modules.ProjectManagement.Application.DTOs;

public class BoardPreferenceDto
{
    public Guid BoardId { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset LastViewedAt { get; set; }
}
