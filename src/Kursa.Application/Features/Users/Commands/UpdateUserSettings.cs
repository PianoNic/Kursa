using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Users.Commands;

public sealed record UpdateUserSettingsCommand(
    string? Theme,
    string? Language,
    string? Timezone,
    bool? NotificationsEnabled) : IRequest<Result<UserSettingsDto>>;

public sealed class UpdateUserSettingsValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    private static readonly string[] ValidThemes = ["light", "dark", "system"];

    public UpdateUserSettingsValidator()
    {
        RuleFor(x => x.Theme)
            .Must(t => ValidThemes.Contains(t))
            .When(x => x.Theme is not null)
            .WithMessage("Theme must be 'light', 'dark', or 'system'.");

        RuleFor(x => x.Language)
            .MaximumLength(16)
            .When(x => x.Language is not null);

        RuleFor(x => x.Timezone)
            .MaximumLength(64)
            .When(x => x.Timezone is not null);
    }
}

public sealed class UpdateUserSettingsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<UpdateUserSettingsCommand, Result<UserSettingsDto>>
{
    public async Task<Result<UserSettingsDto>> Handle(
        UpdateUserSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null)
        {
            return Result<UserSettingsDto>.Failure("User is not authenticated.");
        }

        User? user = await dbContext.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
        {
            return Result<UserSettingsDto>.Failure("User not found.");
        }

        UserSettings settings = user.Settings ?? new UserSettings { UserId = user.Id };

        if (request.Theme is not null)
            settings.Theme = request.Theme;

        if (request.Language is not null)
            settings.Language = request.Language;

        if (request.Timezone is not null)
            settings.Timezone = request.Timezone;

        if (request.NotificationsEnabled.HasValue)
            settings.NotificationsEnabled = request.NotificationsEnabled.Value;

        if (user.Settings is null)
        {
            user.Settings = settings;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UserSettingsDto>.Success(new UserSettingsDto(
            settings.Theme,
            settings.Language,
            settings.Timezone,
            settings.NotificationsEnabled));
    }
}
