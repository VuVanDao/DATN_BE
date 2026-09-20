namespace DATN_BE.Modules.Workspaces.Application.DTOs
{
    public class AddWorkspaceMembersResultDto
    {
        public int TotalRequested { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
        public List<MemberAddResult> Results { get; set; } = new();
    }
    public class MemberAddResult
    {
        public string Email { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
        public WorkspaceMemberDto? Member { get; set; }// dung de gui ve cho FE update danh sach thanh vien
    }
}
