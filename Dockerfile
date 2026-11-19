
# Stage 1: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

# Stage 2: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ProductCatalog.Api/ProductCatalog.Api.csproj", "ProductCatalog.Api/"]

RUN dotnet restore "ProductCatalog.Api/ProductCatalog.Api.csproj"

COPY src/ProductCatalog.Api/ ProductCatalog.Api/

WORKDIR "/src/ProductCatalog.Api"

RUN dotnet build "ProductCatalog.Api.csproj" -c Release -o /app/build

# Stage 3: Publish the application
FROM build AS publish

RUN dotnet publish "ProductCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProductCatalog.Api.dll"]
