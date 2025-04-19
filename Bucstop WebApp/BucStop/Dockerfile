FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BucStop.csproj", "."]
RUN dotnet restore "./BucStop.csproj"
COPY . .
RUN dotnet build "BucStop.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BucStop.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for volumes
RUN mkdir -p /app/Snapshots /app/Logs \
    && chmod 777 /app/Snapshots \
    && chmod 777 /app/Logs

ENTRYPOINT ["dotnet", "BucStop.dll"]
