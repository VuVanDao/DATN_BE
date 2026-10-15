using System.Text.Json.Serialization;

namespace DATN_BE.Modules.ProjectManagement.Domain.Enum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BoardType
    {
        Kanban,
        Scrum
    }
}
