FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY AgroMarket.slnx ./
COPY AgroMarket.Api/AgroMarket.Api.csproj AgroMarket.Api/
RUN dotnet restore AgroMarket.Api/AgroMarket.Api.csproj

COPY AgroMarket.Api/ AgroMarket.Api/
WORKDIR /src/AgroMarket.Api
RUN dotnet publish AgroMarket.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgroMarket.Api.dll"]
