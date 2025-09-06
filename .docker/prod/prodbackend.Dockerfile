# ----------------------------
# Build stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies first (Docker layer caching)
COPY backend/*.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY backend/. ./

# Publish the app (restore already done)
RUN dotnet publish -c Release -o /app/publish

# ----------------------------
# Runtime stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

EXPOSE 8080

# Set entrypoint to your app dll (replace with actual dll name)
ENTRYPOINT ["dotnet", "WebZcan.dll"]
