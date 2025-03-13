FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Ship.Ses.Extractor.Domain/Ship.Ses.Extractor.Domain.csproj", "src/Ship.Ses.Extractor.Domain/"]
COPY ["src/Ship.Ses.Extractor.Application/Ship.Ses.Extractor.Application.csproj", "src/Ship.Ses.Extractor.Application/"]
COPY ["src/Ship.Ses.Extractor.Infrastructure/Ship.Ses.Extractor.Infrastructure.csproj", "src/Ship.Ses.Extractor.Infrastructure/"]
COPY ["src/Ship.Ses.Extractor.WebApi/Ship.Ses.Extractor.WebApi.csproj", "src/Ship.Ses.Extractor.WebApi/"]

RUN dotnet restore "./src/Ship.Ses.Extractor.WebApi/Ship.Ses.Extractor.WebApi.csproj" --disable-parallel
COPY . .
WORKDIR "src/Ship.Ses.Extractor.WebApi"

RUN dotnet publish "./Ship.Ses.Extractor.WebApi.csproj" -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
COPY --from=build /app ./

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Ship.Ses.Extractor.WebApi.dll"]