FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Microservices/Gateway/Gateway.csproj", "Microservices/Gateway/"]
COPY ["Microservices/Shared/Shared.csproj", "Microservices/Shared/"]

# Restore dependencies
RUN dotnet restore "Microservices/Gateway/Gateway.csproj"

# Copy all source files
COPY . .

# Build
WORKDIR "/src/Microservices/Gateway"
RUN dotnet build "Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
