using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Domain.Workflows;

namespace SwaApimPoc.Infrastructure.Workflows;

/// <summary>
/// POC only: seeded sample data. The real app would read from Azure SQL with managed identity.
/// Due dates are relative to today so the overdue count always has something to show.
/// </summary>
public sealed class InMemoryWorkflowItemRepository(TimeProvider timeProvider) : IWorkflowItemRepository
{
    public IReadOnlyList<WorkflowItem> GetAll()
    {
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return
        [
            new(Guid.Parse("8f1c2a10-0001-4c3e-9a51-000000000001"), "Map current intake process", "Discovery", WorkflowStatus.Completed, today.AddDays(-10)),
            new(Guid.Parse("8f1c2a10-0002-4c3e-9a51-000000000002"), "Pick AI tools for document review", "Tooling", WorkflowStatus.InProgress, today.AddDays(3)),
            new(Guid.Parse("8f1c2a10-0003-4c3e-9a51-000000000003"), "Draft huddle agenda for week 1", "Huddles", WorkflowStatus.InProgress, today.AddDays(-2)),
            new(Guid.Parse("8f1c2a10-0004-4c3e-9a51-000000000004"), "Schedule coach sessions", "Coaching", WorkflowStatus.NotStarted, today.AddDays(7)),
            new(Guid.Parse("8f1c2a10-0005-4c3e-9a51-000000000005"), "Review role-based activity list", "Discovery", WorkflowStatus.Completed, today.AddDays(-4)),
            new(Guid.Parse("8f1c2a10-0006-4c3e-9a51-000000000006"), "Send launch email to the team", "Huddles", WorkflowStatus.NotStarted, today.AddDays(-1)),
        ];
    }
}
