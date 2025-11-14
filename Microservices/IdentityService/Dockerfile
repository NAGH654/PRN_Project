FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Microservices/IdentityService/IdentityService.csproj", "Microservices/IdentityService/"]
COPY ["Microservices/Shared/Shared.csproj", "Microservices/Shared/"]

# Restore dependencies
RUN dotnet restore "Microservices/IdentityService/IdentityService.csproj"

# Copy all source files
COPY . .

# Build
WORKDIR "/src/Microservices/IdentityService"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
