FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MetaExchange.sln .
COPY MetaExchange.Api/MetaExchange.Api.csproj MetaExchange.Api/
COPY MetaExchange.Core/MetaExchange.Core.csproj MetaExchange.Core/
COPY MetaExchange.Console/MetaExchange.Console.csproj MetaExchange.Console/
COPY MetaExchange.Tests/MetaExchange.Tests.csproj MetaExchange.Tests/

RUN dotnet restore

COPY . .

WORKDIR /src/MetaExchange.Api
RUN dotnet build -c Release --no-restore

RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

COPY exchanges /app/exchanges

ENTRYPOINT ["dotnet", "MetaExchange.Api.dll"]