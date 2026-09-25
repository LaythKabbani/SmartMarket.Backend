FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy all files from repository
COPY . .

# Restore dependencies directly using WebApi.csproj inside src/
RUN dotnet restore "src/SmartMarket.WebApi/SmartMarket.WebApi.csproj"

# Build and publish WebApi from inside src/
WORKDIR "/app/src/SmartMarket.WebApi"
RUN dotnet publish "SmartMarket.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartMarket.WebApi.dll"]