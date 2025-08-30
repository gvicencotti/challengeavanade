FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY StockService/ StockService/
COPY Shared.Messaging.Events/ Shared.Messaging.Events/
RUN dotnet restore StockService/StockService.csproj
RUN dotnet publish StockService/StockService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StockService.dll"]