#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG CONFIG=Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG CONFIG
WORKDIR /src
COPY ["Marain.Tenancy.Host.AspNetCore/Marain.Tenancy.Host.AspNetCore.csproj", "Marain.Tenancy.Host.AspNetCore/"]
COPY ["Marain.Tenancy.Hosting.AspNetCore/Marain.Tenancy.Hosting.AspNetCore.csproj", "Marain.Tenancy.Hosting.AspNetCore/"]
COPY ["Marain.Tenancy.OpenApi.Service/Marain.Tenancy.OpenApi.Service.csproj", "Marain.Tenancy.OpenApi.Service/"]
RUN dotnet restore "Marain.Tenancy.Host.AspNetCore/Marain.Tenancy.Host.AspNetCore.csproj"
COPY . .
WORKDIR "/src/Marain.Tenancy.Host.AspNetCore"
RUN dotnet build "Marain.Tenancy.Host.AspNetCore.csproj" -c ${CONFIG} -o /app/build

FROM build AS publish
ARG CONFIG
RUN dotnet publish "Marain.Tenancy.Host.AspNetCore.csproj" -c ${CONFIG} -o /app/publish

FROM base AS final
ARG CONFIG
#install debugger for NET Core for debug builds
RUN if [ "${CONFIG}" = "Debug" ]; then \
        touch /is_debug_build  \
        && apt-get update \
        && apt-get install -y \
            curl \
            unzip \
        && curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg \
        && apt-get autoremove -y && apt-get clean -y && rm -rf /var/lib/apt/lists/*; \
    fi
# Default to non-root user
RUN useradd -c 'app user' -m -d /app -s /bin/bash app
USER app
ENV HOME /app
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Marain.Tenancy.Host.AspNetCore.dll"]