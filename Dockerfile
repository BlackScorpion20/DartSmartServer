# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /app

# Copy solution and project files
COPY DartSmartServer.sln ./
COPY src/DartSmart.Domain/*.csproj ./src/DartSmart.Domain/
COPY src/DartSmart.Application/*.csproj ./src/DartSmart.Application/
COPY src/DartSmart.Infrastructure/*.csproj ./src/DartSmart.Infrastructure/
COPY src/DartSmart.WebApi/*.csproj ./src/DartSmart.WebApi/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build and publish
RUN dotnet publish src/DartSmart.WebApi/DartSmart.WebApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --gid 1000 appgroup && \
    adduser --uid 1000 --ingroup appgroup --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DartSmart.WebApi.dll"]
