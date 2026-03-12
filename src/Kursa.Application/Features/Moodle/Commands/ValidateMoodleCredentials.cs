using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using FluentValidation;
using Mediator;

namespace Kursa.Application.Features.Moodle.Commands;

public sealed record ValidateMoodleCredentialsCommand(string Username, string Password) : ICommand<Result>;

public sealed class ValidateMoodleCredentialsValidator : AbstractValidator<ValidateMoodleCredentialsCommand>
{
    public ValidateMoodleCredentialsValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(512);
    }
}

public sealed class ValidateMoodleCredentialsHandler(
    IMoodleService moodleService) : ICommandHandler<ValidateMoodleCredentialsCommand, Result>
{
    public async ValueTask<Result> Handle(ValidateMoodleCredentialsCommand request, CancellationToken cancellationToken)
    {
        var token = await moodleService.GetTokenAsync(request.Username, request.Password, cancellationToken);

        return token is not null
            ? Result.Success()
            : Result.Failure("Invalid Moodle credentials.");
    }
}
