Create a new CQRS feature for the backend. The feature should follow the established pattern:

1. Create a feature folder under `src/StudyApp.Application/Features/{FeatureName}/`
2. Add Command/Query record types as needed
3. Add Handler(s) implementing `IRequestHandler<TRequest, TResponse>`
4. Add FluentValidation validator(s) if the feature has input
5. Add DTO record(s) for responses
6. Add the API endpoint in `src/StudyApp.Api/Controllers/` or `Endpoints/`
7. Register any new services in DI
8. Write at least one unit test in `tests/StudyApp.Application.Tests/`
9. Run `dotnet build` and `dotnet test` to verify everything compiles and passes

Feature to create: $ARGUMENTS
