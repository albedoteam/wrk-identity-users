FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
ARG FEED_URL
ARG PAT

# download and install latest credential provider. Not required after https://github.com/dotnet/dotnet-docker/issues/878
RUN wget -qO- https://aka.ms/install-artifacts-credprovider.sh | bash

WORKDIR /app

# copy csproj an restore as distinct layers
COPY *.csproj ./

# Optional: Sometimes the http client hangs because of a .NET issue. Setting this in dockerfile helps 
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

# Environment variable to enable seesion token cache. More on this here: https://github.com/Microsoft/artifacts-credprovider#help
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true

# Environment variable for adding endpoint credentials. More on this here: https://github.com/Microsoft/artifacts-credprovider#help
# Add "FEED_URL" AND "PAT" using --build-arg in docker build step. "endpointCredentials" field is an array, you can add multiple endpoint configurations.
# Make sure that you *do not* hard code the "PAT" here. That is a sensitive information and must not be checked in.
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS "{\"endpointCredentials\": [{\"endpoint\":\"${FEED_URL}\", \"username\":\"ArtifactsDocker\", \"password\":\"${PAT}\"}]}"

# Use this if you have a nuget.config file with all the endpoints.
RUN dotnet restore -s "${FEED_URL}" -s "https://api.nuget.org/v3/index.json"

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Identity.Business.Users.dll"]