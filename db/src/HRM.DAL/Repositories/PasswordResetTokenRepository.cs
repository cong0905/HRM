using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;
using HRM.Domain.Entities;

namespace HRM.DAL.Repositories;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
}

public class PasswordResetTokenRepository : Repository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(HrmDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);
    }
}
