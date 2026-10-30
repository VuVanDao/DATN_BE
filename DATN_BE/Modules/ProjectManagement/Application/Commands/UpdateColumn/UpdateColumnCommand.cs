using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;
using System.Text.Json.Serialization;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn;

public class UpdateColumnCommand : IRequest<ColumnDto>
{
    [JsonIgnore]
    public Guid BoardId { get; set; }
    [JsonIgnore]
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public required bool IsDoneColumn { get; set; }
}
