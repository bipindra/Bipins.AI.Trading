# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Bipins.AI.Trading.Web/Bipins.AI.Trading.Web.csproj", "Bipins.AI.Trading.Web/"]
COPY ["Bipins.AI.Trading.Application/Bipins.AI.Trading.Application.csproj", "Bipins.AI.Trading.Application/"]
COPY ["Bipins.AI.Trading.Infrastructure/Bipins.AI.Trading.Infrastructure.csproj", "Bipins.AI.Trading.Infrastructure/"]
COPY ["Bipins.AI.Trading.Domain/Bipins.AI.Trading.Domain.csproj", "Bipins.AI.Trading.Domain/"]

# Restore dependencies
RUN dotnet restore "Bipins.AI.Trading.Web/Bipins.AI.Trading.Web.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/Bipins.AI.Trading.Web"
RUN dotnet build "Bipins.AI.Trading.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bipins.AI.Trading.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Create directories for logs and database
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check (using wget which is available in aspnet image)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Bipins.AI.Trading.Web.dll"]
