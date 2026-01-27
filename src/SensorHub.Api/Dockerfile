FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SensorHub.Api/SensorHub.Api.csproj", "src/SensorHub.Api/"]
RUN dotnet restore "src/SensorHub.Api/SensorHub.Api.csproj"
COPY . .
WORKDIR "/src/src/SensorHub.Api"
RUN dotnet build "SensorHub.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SensorHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SensorHub.Api.dll"]
