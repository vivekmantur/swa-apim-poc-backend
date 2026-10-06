namespace SwaApimPoc.Domain.Workflows;

public enum WorkflowStatus
{
    NotStarted,
    InProgress,
    Completed,
}

/// <summary>
/// Sample data shown on the dashboard, so the POC proves real data flows back through APIM and SWA.
/// </summary>
public sealed record WorkflowItem(
    Guid Id,
    string Title,
    string Bucket,
    WorkflowStatus Status,
    DateOnly DueDate)
{
    public bool IsOverdue(DateOnly today) => Status != WorkflowStatus.Completed && DueDate < today;
}
