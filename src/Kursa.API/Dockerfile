# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Backend dependencies restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-deps
WORKDIR /src
COPY ["src/Kursa.API/Kursa.API.csproj", "src/Kursa.API/"]
COPY ["src/Kursa.Application/Kursa.Application.csproj", "src/Kursa.Application/"]
COPY ["src/Kursa.Domain/Kursa.Domain.csproj", "src/Kursa.Domain/"]
COPY ["src/Kursa.Infrastructure/Kursa.Infrastructure.csproj", "src/Kursa.Infrastructure/"]
RUN dotnet restore "src/Kursa.API/Kursa.API.csproj"

# Stage 3: Backend build
FROM backend-deps AS backend-build
ARG BUILD_CONFIGURATION=Release
COPY src/ src/
WORKDIR "/src/src/Kursa.API"
RUN dotnet publish "Kursa.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Frontend dependencies
FROM oven/bun:1 AS frontend-deps
WORKDIR /src/frontend
COPY src/Kursa.Frontend/package.json src/Kursa.Frontend/bun.lock ./
RUN bun install --frozen-lockfile

# Stage 5: Frontend build
FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY --from=frontend-deps /src/frontend/node_modules ./node_modules
COPY src/Kursa.Frontend/ .
RUN npx ng build --configuration production

# Stage 6: Final image
FROM base AS final
WORKDIR /app
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /src/frontend/dist/Kursa.Frontend ./wwwroot
ENTRYPOINT ["dotnet", "Kursa.API.dll"]
