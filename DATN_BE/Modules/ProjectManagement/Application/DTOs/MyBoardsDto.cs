namespace DATN_BE.Modules.ProjectManagement.Application.DTOs;

public class MyBoardsDto
{
    public List<MyBoardItemDto> RecentBoards { get; set; } = [];
    public List<MyBoardItemDto> PinnedBoards { get; set; } = [];
}

public class MyBoardItemDto
{
    public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BoardSlug { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset LastViewedAt { get; set; }
}
