using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Kursa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Infrastructure.Services;

public sealed class UserSyncService(AppDbContext dbContext) : IUserSyncService
{
    public async Task<User> SyncUserAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                ExternalId = externalId,
                Email = email,
                DisplayName = displayName,
                LastLoginAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.DisplayName = displayName;
            user.LastLoginAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
