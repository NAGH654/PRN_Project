# Docker Setup Guide

This document explains how to run the PRN232 Assignment Project using Docker and Docker Compose.

## Prerequisites

- Docker Desktop installed on your machine
- Docker Compose (usually included with Docker Desktop)

## Quick Start

### 1. Build and Start All Services

```bash
docker-compose up --build
```

This command will:
- Build the .NET API Docker image
- Pull the SQL Server 2022 image
- Start both containers
- Create a network for them to communicate
- Initialize the database

### 2. Access the Application

Once the containers are running:
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **SQL Server**: localhost:1433
  - Username: `sa`
  - Password: `YourStrong@Passw0rd`

## Docker Commands

### Start Services (Detached Mode)
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### Stop Services and Remove Volumes (Clean Database)
```bash
docker-compose down -v
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f sqlserver
```

### Rebuild Images
```bash
docker-compose build --no-cache
```

### Restart a Specific Service
```bash
docker-compose restart api
```

## Configuration

### Database Connection

The connection string is configured in `docker-compose.yml`:
```
Server=sqlserver;Database=Swd392;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
```

### Change SQL Server Password

1. Update the password in `docker-compose.yml`:
   ```yaml
   sqlserver:
     environment:
       - MSSQL_SA_PASSWORD=YourNewPassword
   
   api:
     environment:
       - ConnectionStrings__Default=Server=sqlserver;Database=Swd392;User Id=sa;Password=YourNewPassword;...
   ```

2. Rebuild and restart:
   ```bash
   docker-compose down -v
   docker-compose up --build
   ```

### Ports

Default ports can be changed in `docker-compose.yml`:
- API: Change `"5000:8080"` to `"YOUR_PORT:8080"`
- SQL Server: Change `"1433:1433"` to `"YOUR_PORT:1433"`

## Troubleshooting

### Database Connection Issues

If the API can't connect to the database:
1. Check if SQL Server is healthy:
   ```bash
   docker-compose ps
   ```
2. Wait for SQL Server to be fully ready (can take 30-60 seconds on first run)
3. Check logs:
   ```bash
   docker-compose logs sqlserver
   ```

### API Not Starting

1. Check logs:
   ```bash
   docker-compose logs api
   ```
2. Ensure the database is running:
   ```bash
   docker-compose ps sqlserver
   ```

### Port Already in Use

If you get a "port already allocated" error:
1. Stop any local SQL Server or API instances
2. Or change the port in `docker-compose.yml`

### Clean Restart

To completely reset everything:
```bash
docker-compose down -v
docker system prune -f
docker-compose up --build
```

## Data Persistence

- Database data is stored in a Docker volume named `sqlserver_data`
- Application storage files are mounted from `./API/storage` to `/app/storage` in the container
- To reset the database, remove the volume: `docker-compose down -v`

## Development Workflow

### Running Migrations

To run EF Core migrations inside the container:
```bash
docker-compose exec api dotnet ef migrations add MigrationName --project /src/Repositories
docker-compose exec api dotnet ef database update --project /src/Repositories
```

### Connect to SQL Server from Host

Use any SQL client (SSMS, Azure Data Studio, etc.):
- Server: `localhost,1433`
- Username: `sa`
- Password: `YourStrong@Passw0rd`
- Database: `Swd392`

### Access Container Shell

```bash
# API container
docker-compose exec api /bin/bash

# SQL Server container
docker-compose exec sqlserver /bin/bash
```

## Production Deployment

For production:
1. Create a `.env` file based on `.env.example`
2. Use stronger passwords
3. Remove development configurations
4. Consider using `docker-compose.prod.yml` with production settings
5. Use proper secrets management
6. Configure reverse proxy (nginx, traefik)
7. Set up SSL/TLS certificates

## Additional Notes

- The API automatically seeds the database on startup (via `AppDbSeeder.SeedAsync`)
- 7-Zip is installed in the container for file processing
- Health checks ensure SQL Server is ready before the API starts
- The application runs on port 8080 inside the container, mapped to 5000 on the host
