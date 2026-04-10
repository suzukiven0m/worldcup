FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/WorldCupFormations.Web/WorldCupFormations.Web.csproj src/WorldCupFormations.Web/
RUN dotnet restore src/WorldCupFormations.Web/WorldCupFormations.Web.csproj

COPY . .
RUN dotnet publish src/WorldCupFormations.Web/WorldCupFormations.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "WorldCupFormations.Web.dll"]
