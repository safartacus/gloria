namespace Gloria.Application.Abstractions;

/// <summary>Commits all pending aggregate changes in one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
