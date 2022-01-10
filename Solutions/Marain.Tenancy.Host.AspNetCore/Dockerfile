#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Marain.Tenancy.Host.AspNetCore/Marain.Tenancy.Host.AspNetCore.csproj", "Marain.Tenancy.Host.AspNetCore/"]
COPY ["Marain.Tenancy.Hosting.AspNetCore/Marain.Tenancy.Hosting.AspNetCore.csproj", "Marain.Tenancy.Hosting.AspNetCore/"]
COPY ["Marain.Tenancy.OpenApi.Service/Marain.Tenancy.OpenApi.Service.csproj", "Marain.Tenancy.OpenApi.Service/"]
RUN dotnet restore "Marain.Tenancy.Host.AspNetCore/Marain.Tenancy.Host.AspNetCore.csproj"
COPY . .
WORKDIR "/src/Marain.Tenancy.Host.AspNetCore"
RUN dotnet build "Marain.Tenancy.Host.AspNetCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Marain.Tenancy.Host.AspNetCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Marain.Tenancy.Host.AspNetCore.dll"]