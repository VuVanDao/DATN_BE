namespace DATN_BE.Shared.Application.Interfaces
{
    public interface IProjectAuthorizationService
    {
        Task EnsureCanViewProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureCanModifyProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureProjectLeaderAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureCanViewProjectAuditAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}
