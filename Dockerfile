FROM mcr.microsoft.com/azure-functions/dotnet:3.0-dotnet3-core-tools AS base
WORKDIR /app
EXPOSE 7071

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
# RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
# USER appuser

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Solutions", "./Solutions/"]
RUN dotnet restore ./Solutions

WORKDIR "/src/Solutions"
RUN dotnet build --no-restore -c Release
RUN dotnet test --no-build -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/Solutions/Marain.Tenancy.Host.Functions"
RUN dotnet publish "Marain.Tenancy.Host.Functions.csproj" --no-build -c Release -o /app/publish

FROM base AS final
ENV AzureWebJobsScriptRoot=/app \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=publish /app/publish .

CMD [ "func", "host", "start", "--csharp" ]