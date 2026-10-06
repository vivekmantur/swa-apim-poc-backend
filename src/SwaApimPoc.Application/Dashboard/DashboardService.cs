using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Application.Common;
using SwaApimPoc.Domain.Workflows;

namespace SwaApimPoc.Application.Dashboard;

public sealed class DashboardService(
    ICurrentUser currentUser,
    IVerifiedIdentityStore store,
    IWorkflowItemRepository workflowItems,
    TimeProvider timeProvider)
{
    public DashboardDto GetDashboard()
    {
        if (!currentUser.IsAuthenticated || currentUser.SwaUserId is null)
        {
            throw new UnauthenticatedException();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        IReadOnlyList<WorkflowItem> items = workflowItems.GetAll();

        var summary = new DashboardSummaryDto(
            Total: items.Count,
            NotStarted: items.Count(item => item.Status == WorkflowStatus.NotStarted),
            InProgress: items.Count(item => item.Status == WorkflowStatus.InProgress),
            Completed: items.Count(item => item.Status == WorkflowStatus.Completed),
            Overdue: items.Count(item => item.IsOverdue(today)));

        WorkflowItemDto[] workflows = items
            .OrderBy(item => item.Status == WorkflowStatus.Completed)
            .ThenBy(item => item.DueDate)
            .Select(item => new WorkflowItemDto(
                item.Id, item.Title, item.Bucket, item.Status.ToString(), item.DueDate, item.IsOverdue(today)))
            .ToArray();

        return new DashboardDto(
            currentUser.UserDetails,
            currentUser.SwaUserId,
            currentUser.IdentityProvider,
            currentUser.Roles,
            store.Find(currentUser.SwaUserId),
            summary,
            workflows,
            now);
    }
}
