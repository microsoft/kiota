FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
ARG version_suffix
WORKDIR /app

COPY ./src ./kiota/src
COPY ./resources ./kiota/resources
WORKDIR /app/kiota
RUN if [ -z "$version_suffix" ]; then \
    dotnet publish ./src/kiota/kiota.csproj -c Release -p:TreatWarningsAsErrors=false -f net10.0; \
    else \
    dotnet publish ./src/kiota/kiota.csproj -c Release -p:TreatWarningsAsErrors=false -f net10.0 --version-suffix "$version_suffix"; \
    fi

# Don't use the chiseled image without extras 
# (see https://github.com/microsoft/kiota/issues/4600)
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

COPY --from=build-env /app/kiota/src/kiota/bin/Release/net10.0 ./

VOLUME /app/output
VOLUME /app/openapi.yaml
VOLUME /app/apimanifest.json
ENV KIOTA_CONTAINER=true DOTNET_TieredPGO=1 DOTNET_TC_QuickJitForLoops=1
ENTRYPOINT ["dotnet", "kiota.dll"]
LABEL description="# Welcome to Kiota Generator \
    To start generating SDKs checkout [the getting started documentation](https://learn.microsoft.com/openapi/kiota/install#run-in-docker)  \
    [Source dockerfile](https://github.com/microsoft/kiota/blob/main/Dockerfile)"