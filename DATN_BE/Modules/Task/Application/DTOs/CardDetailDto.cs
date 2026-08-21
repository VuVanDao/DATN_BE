namespace DATN_BE.Modules.Task.Application.DTOs
{
    public class CardDetailDto
    {
        public List<CardCommentDto> Comments { get; set; } = new();
    }
}
