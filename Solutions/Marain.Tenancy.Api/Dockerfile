ARG BaseImage=aspnet
FROM mcr.microsoft.com/dotnet/${BaseImage}:8.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ARG Configuration=Debug
# This default will allow the container to be built locally via 'docker build' following a default manual publish.
# The build process will override this value and the ContextDir to reflect that it publishes projects to the '_packages' folder.
ARG SrcDir=./bin/${Configuration}/net9.0/publish
COPY ${SrcDir} .
ENTRYPOINT ["dotnet", "Marain.Tenancy.Api.dll"]