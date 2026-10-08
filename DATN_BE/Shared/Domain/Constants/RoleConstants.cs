namespace DATN_BE.Shared.Domain.Constants
{
    /// <summary>
    /// Hằng số tên vai trò hệ thống — dùng thay cho magic string khi query/so sánh Role.Name.
    /// </summary>
    public static class RoleConstants
    {
        // === Global Roles ===
        public const string SystemAdmin = "SystemAdmin";
        public const string User = "User";

        // === Workspace Roles ===
        public const string WorkspaceOwner = "WorkspaceOwner";
        public const string WorkspaceMember = "WorkspaceMember";

        // === Project Roles ===
        public const string ProjectLeader = "ProjectLeader";
        public const string ProjectMember = "ProjectMember";
    }
}
