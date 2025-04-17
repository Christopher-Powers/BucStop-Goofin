FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Fix: Use correct relative path for the .csproj file
COPY ["BucStop.csproj", "./"]

RUN dotnet restore "BucStop.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "BucStop.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BucStop.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for volumes
RUN mkdir -p /app/Logs && \
    mkdir -p /app/wwwroot && \
    chmod -R 755 /app/Logs && \
    chmod -R 755 /app/wwwroot

ENTRYPOINT ["dotnet", "BucStop.dll"]
