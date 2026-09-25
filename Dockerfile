FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything from repository
COPY . .

# Restore using the exact slnx file
RUN dotnet restore "SmartMarket.slnx"

# Build and publish WebApi from inside src
WORKDIR "/app/src/SmartMarket.WebApi"
RUN dotnet publish "SmartMarket.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartMarket.WebApi.dll"]