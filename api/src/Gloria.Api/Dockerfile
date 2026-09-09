# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Gloria.sln ./
COPY src/Gloria.Domain/Gloria.Domain.csproj src/Gloria.Domain/
COPY src/Gloria.Application/Gloria.Application.csproj src/Gloria.Application/
COPY src/Gloria.Infrastructure/Gloria.Infrastructure.csproj src/Gloria.Infrastructure/
COPY src/Gloria.Api/Gloria.Api.csproj src/Gloria.Api/
COPY tests/Gloria.Domain.UnitTests/Gloria.Domain.UnitTests.csproj tests/Gloria.Domain.UnitTests/
COPY tests/Gloria.Application.Tests/Gloria.Application.Tests.csproj tests/Gloria.Application.Tests/
RUN dotnet restore src/Gloria.Api/Gloria.Api.csproj

COPY . .
RUN dotnet publish src/Gloria.Api/Gloria.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gloria.Api.dll"]
