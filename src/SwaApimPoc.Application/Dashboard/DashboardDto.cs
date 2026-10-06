using SwaApimPoc.Domain.Identity;

namespace SwaApimPoc.Application.Dashboard;

public sealed record DashboardDto(
    string? UserDetails,
    string? SwaUserId,
    string? IdentityProvider,
    IReadOnlyCollection<string> Roles,
    VerifiedIdentity? VerifiedIdentity,
    DashboardSummaryDto Summary,
    IReadOnlyList<WorkflowItemDto> Workflows,
    DateTimeOffset ServerTimeUtc);

public sealed record DashboardSummaryDto(
    int Total,
    int NotStarted,
    int InProgress,
    int Completed,
    int Overdue);

public sealed record WorkflowItemDto(
    Guid Id,
    string Title,
    string Bucket,
    string Status,
    DateOnly DueDate,
    bool IsOverdue);
