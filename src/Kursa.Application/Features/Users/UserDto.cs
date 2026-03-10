using Kursa.Domain.Enums;

namespace Kursa.Application.Features.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    UserRole Role,
    bool OnboardingCompleted,
    string? MoodleUrl,
    bool HasMoodleToken,
    DateTime CreatedAt);

public sealed record UserSettingsDto(
    string Theme,
    string Language,
    string Timezone,
    bool NotificationsEnabled);
