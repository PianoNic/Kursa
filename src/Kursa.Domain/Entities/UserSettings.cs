namespace Kursa.Domain.Entities;

public class UserSettings : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Theme { get; set; } = "dark";

    public string Language { get; set; } = "en";

    public string Timezone { get; set; } = "UTC";

    public bool NotificationsEnabled { get; set; } = true;
}
