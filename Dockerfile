FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

COPY ./src ./kiota/src
WORKDIR /app/kiota
RUN dotnet publish ./src/kiota/kiota.csproj -c Release

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:7.0 as runtime
WORKDIR /app

COPY --from=build-env /app/kiota/src/kiota/bin/Release/net7.0 ./

VOLUME /app/output
VOLUME /app/openapi.yml
ENV KIOTA_CONTAINER=true DOTNET_TieredPGO=1 DOTNET_TC_QuickJitForLoops=1
ENTRYPOINT ["dotnet", "kiota.dll"]
LABEL description="# Welcome to Kiota Generator \
To start generating SDKs checkout [the getting started documentation](https://microsoft.github.io/kiota/get-started/#run-in-docker)  \
[Source dockerfile](https://github.com/microsoft/kiota/blob/main/Dockerfile)"
