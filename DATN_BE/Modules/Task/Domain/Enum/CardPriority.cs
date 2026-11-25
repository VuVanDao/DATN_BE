using System.Text.Json.Serialization;

namespace DATN_BE.Modules.Task.Domain.Enum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }
}
