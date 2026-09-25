FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy full repository content
COPY . .

# Restore dependencies via the solution file
RUN dotnet restore "SmartMarket.sln"

# Build and publish the WebApi project from inside src/
WORKDIR "/app/src/SmartMarket.WebApi"
RUN dotnet publish "SmartMarket.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartMarket.WebApi.dll"]