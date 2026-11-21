# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy global props, configs, and project files for caching dependency restore
COPY global.json Directory.Build.props Directory.Packages.props NuGet.Config ./
COPY backend/src/HRM.Domain/*.csproj ./backend/src/HRM.Domain/
COPY backend/src/HRM.Application/*.csproj ./backend/src/HRM.Application/
COPY backend/src/HRM.Infrastructure/*.csproj ./backend/src/HRM.Infrastructure/
COPY backend/src/HRM.Api/*.csproj ./backend/src/HRM.Api/

# Restore dependencies for API project
RUN dotnet restore backend/src/HRM.Api/HRM.Api.csproj

# Copy the rest of the source code
COPY backend/src/ ./backend/src/

# Build and Publish in Release mode
WORKDIR /src/backend/src/HRM.Api
RUN dotnet publish HRM.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Non-root user for security
USER $APP_UID

# Expose HTTP port
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

# Copy published binaries
COPY --from=build /app/publish .

# Healthcheck probe
HEALTHCHECK --interval=10s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "HRM.Next.Api.dll"]
