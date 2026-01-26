# Docker Setup for Bipins.AI.Trading

This directory contains Docker configuration for running the Bipins.AI.Trading platform in containers.

## Prerequisites

- Docker Desktop or Docker Engine 20.10+
- Docker Compose 2.0+

## Quick Start

### Start All Services

```bash
cd docker
docker-compose up -d
```

This will start:
- **Qdrant**: Vector database on ports 6333 (HTTP) and 6334 (gRPC)
- **Web Application**: Trading platform on port 8080

### Access the Application

- Web Portal: http://localhost:8080
- Qdrant Dashboard: http://localhost:6333/dashboard
- Health Check: http://localhost:8080/health

Default credentials:
- Username: `admin`
- Password: `admin`

## Building the Application

### Build Docker Image

```bash
cd docker
docker-compose build
```

### Build Without Cache

```bash
docker-compose build --no-cache
```

## Configuration

### Environment Variables

The application can be configured via environment variables in `docker-compose.yml`:

- `ASPNETCORE_ENVIRONMENT`: Development, Staging, or Production
- `ConnectionStrings__DefaultConnection`: Database connection string
- `VectorDb__Qdrant__Endpoint`: Qdrant endpoint (default: http://qdrant:6333)
- `LLM__Provider`: LLM provider (OpenAI, Anthropic, AzureOpenAI)
- `LLM__OpenAI__ApiKey`: OpenAI API key
- `LLM__Anthropic__ApiKey`: Anthropic API key
- `LLM__AzureOpenAI__ApiKey`: Azure OpenAI API key

### Using Environment File

Create a `.env` file in the `docker` directory:

```env
ASPNETCORE_ENVIRONMENT=Development
LLM__OpenAI__ApiKey=your-api-key-here
Broker__Alpaca__ApiKey=your-alpaca-key
Broker__Alpaca__ApiSecret=your-alpaca-secret
```

Then reference it in `docker-compose.yml`:

```yaml
services:
  web:
    env_file:
      - .env
```

## Volumes

The following volumes are created:

- `qdrant_storage`: Persistent storage for Qdrant vector database
- `web_data`: Application data directory (database files)
- `web_logs`: Application logs

## Networking

All services are connected via the `trading-network` bridge network, allowing them to communicate using service names:
- Web application can reach Qdrant at `http://qdrant:6333`

## Health Checks

Both services include health checks:

- **Qdrant**: Checks `/health` endpoint every 30 seconds
- **Web**: Checks `/health` endpoint every 30 seconds, includes database and Qdrant dependency checks

View health status:

```bash
docker-compose ps
```

## Logs

### View All Logs

```bash
docker-compose logs -f
```

### View Specific Service Logs

```bash
docker-compose logs -f web
docker-compose logs -f qdrant
```

## Stopping Services

```bash
docker-compose down
```

To also remove volumes:

```bash
docker-compose down -v
```

## Development Workflow

### Hot Reload (Optional)

For development with hot reload, you can mount the source code:

1. Uncomment volume mounts in `docker-compose.override.yml`
2. Use `dotnet watch` inside the container or run locally with Docker for Qdrant only

### Running Locally with Docker Qdrant Only

```bash
# Start only Qdrant
docker-compose up -d qdrant

# Run application locally
cd ../src/Bipins.AI.Trading.Web
dotnet run
```

## Troubleshooting

### Container Won't Start

1. Check logs: `docker-compose logs web`
2. Verify ports are not in use: `netstat -an | grep 8080`
3. Check Docker resources (memory, CPU)

### Database Connection Issues

- Ensure the database file path in volume mount is correct
- Check file permissions on mounted volumes
- Verify connection string in environment variables

### Qdrant Connection Issues

- Verify Qdrant is healthy: `curl http://localhost:6333/health`
- Check network connectivity: `docker-compose exec web ping qdrant`
- Verify endpoint configuration in environment variables

### Health Check Failures

- Check application logs: `docker-compose logs web`
- Verify all dependencies are running: `docker-compose ps`
- Test health endpoint manually: `curl http://localhost:8080/health`

## Production Considerations

For production deployments:

1. **Use production Dockerfile**: Ensure `ASPNETCORE_ENVIRONMENT=Production`
2. **Secrets Management**: Use Docker secrets or external secret management (Azure Key Vault, AWS Secrets Manager)
3. **Resource Limits**: Set CPU and memory limits in `docker-compose.yml`
4. **Persistent Storage**: Ensure volumes are backed up
5. **Network Security**: Use internal networks and reverse proxy
6. **Monitoring**: Integrate with container monitoring solutions
7. **Logging**: Configure log aggregation (ELK, Splunk, etc.)

## Clean Up

Remove all containers, networks, and volumes:

```bash
docker-compose down -v --rmi all
```
