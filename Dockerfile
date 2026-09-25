FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SmartMarket.WebApi/SmartMarket.WebApi.csproj", "SmartMarket.WebApi/"]
COPY ["SmartMarket.Application/SmartMarket.Application.csproj", "SmartMarket.Application/"]
COPY ["SmartMarket.Infrastructure/SmartMarket.Infrastructure.csproj", "SmartMarket.Infrastructure/"]
COPY ["SmartMarket.Domain/SmartMarket.Domain.csproj", "SmartMarket.Domain/"]

RUN dotnet restore "SmartMarket.WebApi/SmartMarket.WebApi.csproj"

COPY . .
WORKDIR "/src/SmartMarket.WebApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartMarket.WebApi.dll"]