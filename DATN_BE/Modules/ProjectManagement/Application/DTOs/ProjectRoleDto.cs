namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    // trả về danh sách các vai trò trong dự án
    public class ProjectRoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}