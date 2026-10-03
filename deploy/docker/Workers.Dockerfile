FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MarketPulse.sln ./
COPY src/MarketPulse.Domain/MarketPulse.Domain.csproj src/MarketPulse.Domain/
COPY src/MarketPulse.Application/MarketPulse.Application.csproj src/MarketPulse.Application/
COPY src/MarketPulse.Infrastructure/MarketPulse.Infrastructure.csproj src/MarketPulse.Infrastructure/
COPY src/MarketPulse.Api/MarketPulse.Api.csproj src/MarketPulse.Api/
COPY src/MarketPulse.Workers/MarketPulse.Workers.csproj src/MarketPulse.Workers/
COPY tests/MarketPulse.UnitTests/MarketPulse.UnitTests.csproj tests/MarketPulse.UnitTests/
RUN dotnet restore src/MarketPulse.Workers/MarketPulse.Workers.csproj
COPY src/ src/
COPY tests/ tests/
COPY samples/ samples/
RUN dotnet publish src/MarketPulse.Workers/MarketPulse.Workers.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 9465
USER app
ENTRYPOINT ["dotnet", "MarketPulse.Workers.dll"]
