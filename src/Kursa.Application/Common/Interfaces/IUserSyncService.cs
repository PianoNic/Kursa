using Kursa.Domain.Entities;

namespace Kursa.Application.Common.Interfaces;

public interface IUserSyncService
{
    Task<User> SyncUserAsync(string externalId, string email, string displayName, CancellationToken cancellationToken = default);
}
