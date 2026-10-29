using System.Text.Json.Serialization;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference;

public class UpdateBoardPreferenceCommand : IRequest<BoardPreferenceDto>
{
    [JsonIgnore]
    public Guid BoardId { get; set; }
    public bool? IsPinned { get; set; }
    public bool MarkViewed { get; set; }
}
