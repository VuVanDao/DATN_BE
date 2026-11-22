namespace DATN_BE.Modules.Security.Domain.Enum
{
    public enum ActivityActionType
    {
        Created,
        Updated,
        Deleted,
        Restored,
        MovedToColumn,
        [Obsolete("Sử dụng MovedToColumn thay thế, giữ lại để tương thích ngược dữ liệu cũ")]
        MovedColumn = MovedToColumn,
        UpdatedDueDate,
        AssignedUser,
        UnassignedUser,
        Commented,
        MemberAdded,
        MemberRemoved,
        RoleChanged
    }
}
