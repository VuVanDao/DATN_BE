using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.RemoveCardDependency
{
    public class RemoveCardDependencyCommand : IRequest<bool>
    {
        public Guid DependencyId { get; set; }

        public RemoveCardDependencyCommand(Guid dependencyId)
        {
            DependencyId = dependencyId;
        }
    }
}
