FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files relative to repository root
COPY ["SmartMarket.WebApi/SmartMarket.WebApi.csproj", "SmartMarket.WebApi/"]
COPY ["SmartMarket.Application/SmartMarket.Application.csproj", "SmartMarket.Application/"]
COPY ["SmartMarket.Infrastructure/SmartMarket.Infrastructure.csproj", "SmartMarket.Infrastructure/"]
COPY ["SmartMarket.Domain/SmartMarket.Domain.csproj", "SmartMarket.Domain/"]

# Restore dependencies
RUN dotnet restore "SmartMarket.WebApi/SmartMarket.WebApi.csproj"

# Copy full source code and build
COPY . .
WORKDIR "/src/SmartMarket.WebApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartMarket.WebApi.dll"]