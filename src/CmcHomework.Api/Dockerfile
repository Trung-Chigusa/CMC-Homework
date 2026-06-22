FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/CmcHomework.Api/CmcHomework.Api.csproj src/CmcHomework.Api/
RUN dotnet restore src/CmcHomework.Api/CmcHomework.Api.csproj

COPY src/CmcHomework.Api/ src/CmcHomework.Api/
RUN dotnet publish src/CmcHomework.Api/CmcHomework.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CmcHomework.Api.dll"]
