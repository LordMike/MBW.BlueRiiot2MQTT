# https://hub.docker.com/_/microsoft-dotnet-runtime
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
WORKDIR "/src/MBW.BlueRiiot2MQTT/"
RUN dotnet publish -c release -o /app

# Runtime image, use an arch-specific image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:10.0-alpine

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MBW.BlueRiiot2MQTT.dll"]