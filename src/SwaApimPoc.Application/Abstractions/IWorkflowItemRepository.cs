using SwaApimPoc.Domain.Workflows;

namespace SwaApimPoc.Application.Abstractions;

public interface IWorkflowItemRepository
{
    IReadOnlyList<WorkflowItem> GetAll();
}
