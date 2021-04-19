FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

COPY ./src ./kiota/src
COPY ./tests ./kiota/tests
COPY ./kiota.sln ./kiota
WORKDIR /app/kiota
RUN dotnet publish ./src/kiota/kiota.csproj -c Release

FROM mcr.microsoft.com/dotnet/runtime:5.0 as runtime
WORKDIR /app

COPY --from=build-env /app/kiota/src/kiota/bin/Release/net5.0 ./

VOLUME /app/output
VOLUME /app/openapi.yml
ENTRYPOINT ["dotnet", "kiota.dll"]