using Gloria.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly GloriaDbContext _db;
    public RoleRepository(GloriaDbContext db) => _db = db;

    public Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Roles.OrderBy(r => r.Code).ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(role, ct);
}

public sealed class UserRepository : IUserRepository
{
    private readonly GloriaDbContext _db;
    public UserRepository(GloriaDbContext db) => _db = db;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Users.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync(ct);

    public Task<bool> ExistsAsync(string username, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Username == username, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _db.Users.CountAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly GloriaDbContext _db;
    public RefreshTokenRepository(GloriaDbContext db) => _db = db;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, DateTime nowUtc, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > nowUtc)
            .ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);
}
