#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Solutions/Marain.Tenancy.MinimalApi/Marain.Tenancy.MinimalApi.csproj", "Solutions/Marain.Tenancy.MinimalApi/"]
COPY ["Solutions/Marain.Tenancy.Storage.Azure.BlobStorage/Marain.Tenancy.Storage.Azure.BlobStorage.csproj", "Solutions/Marain.Tenancy.Storage.Azure.BlobStorage/"]
COPY ["Solutions/Marain.Tenancy.ClientTenantProvider/Marain.Tenancy.ClientTenantProvider.csproj", "Solutions/Marain.Tenancy.ClientTenantProvider/"]
RUN dotnet restore "./Solutions/Marain.Tenancy.MinimalApi/Marain.Tenancy.MinimalApi.csproj"
COPY . .
WORKDIR "/src/Solutions/Marain.Tenancy.MinimalApi"
RUN dotnet build "./Marain.Tenancy.MinimalApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Marain.Tenancy.MinimalApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Marain.Tenancy.MinimalApi.dll"]