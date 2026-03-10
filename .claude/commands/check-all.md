Run all project checks to ensure everything is in good shape:

1. **Backend build**: `dotnet build StudyApp.sln --no-restore`
2. **Backend tests**: `dotnet test StudyApp.sln --no-build`
3. **Backend format check**: `dotnet format StudyApp.sln --verify-no-changes`
4. **Frontend build**: `cd src/StudyApp.Web && ng build`
5. **Frontend lint**: `cd src/StudyApp.Web && ng lint` (if configured)
6. **Frontend tests**: `cd src/StudyApp.Web && ng test --watch=false --browsers=ChromeHeadless`
7. **Docker build**: `docker compose build`

Report any failures with clear descriptions of what went wrong and suggest fixes.
