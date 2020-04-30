FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build

COPY . /src/
WORKDIR "/src/MBW.BlueRiiot2MQTT/"
RUN dotnet publish "MBW.BlueRiiot2MQTT.csproj" -c Release -o /app

FROM base AS final
COPY --from=build /app .

ENTRYPOINT ["dotnet", "MBW.BlueRiiot2MQTT.dll"]