namespace DATN_BE.Shared.Application.Interfaces
{
    public interface IProjectAuthorizationService
    {
        Task EnsureCanViewProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureCanModifyProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}
