public interface IRepositoryReviewer
{
    Task<ReviewReport> ReviewAsync(
        string repositoryPath,
        CancellationToken cancellationToken);
}

public sealed record ReviewReport(
    string Markdown,
    string SessionId,
    string AuditLogPath);
