namespace DATN_BE.Modules.Task.Application.DTOs
{
    public class CardDetailDto : CardDto
    {
        public List<CardCommentDto> Comments { get; set; } = new();
    }
}
