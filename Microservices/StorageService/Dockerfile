FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update && \
    apt-get install -y curl p7zip-full && \
    rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy Shared project
COPY ["Microservices/Shared/Shared.csproj", "Microservices/Shared/"]
RUN dotnet restore "Microservices/Shared/Shared.csproj"

# Copy StorageService project
COPY ["Microservices/StorageService/StorageService.csproj", "Microservices/StorageService/"]
RUN dotnet restore "Microservices/StorageService/StorageService.csproj"

# Copy all source files
COPY Microservices/Shared/ Microservices/Shared/
COPY Microservices/StorageService/ Microservices/StorageService/

# Build
WORKDIR "/src/Microservices/StorageService"
RUN dotnet build "StorageService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StorageService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create storage directories with proper permissions
USER root
RUN mkdir -p /app/storage/uploads /app/storage/jobs && \
    chown -R app:app /app/storage && \
    chmod -R 755 /app/storage

USER app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StorageService.dll"]
