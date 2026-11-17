FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY AgroMarket.slnx ./
COPY AgroMarket.Api/AgroMarket.Api.csproj AgroMarket.Api/
RUN dotnet restore AgroMarket.Api/AgroMarket.Api.csproj

COPY AgroMarket.Api/ AgroMarket.Api/
WORKDIR /src/AgroMarket.Api
RUN dotnet publish AgroMarket.Api.csproj -c Release -o /app/publish --no-restore

FROM build AS migrator-build
WORKDIR /src/AgroMarket.Api
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --self-contained -o /app/efbundle --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=migrator-build /app/efbundle /app/efbundle
RUN chmod +x /app/efbundle
ENTRYPOINT ["/app/efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgroMarket.Api.dll"]
