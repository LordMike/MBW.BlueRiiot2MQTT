# BlueRiiot2MQTT
[![docker hub](https://img.shields.io/docker/pulls/lordmike/blueriiot2mqtt)](https://hub.docker.com/repository/docker/lordmike/blueriiot2mqtt)

![logo](Logo/Logo.png)

This is a proxy application to translate the status of a Blue Riiot pool manager, to Home Assistant using MQTT. You can run this application in docker, and it will periodically poll the Blue Riiot API for updates.

This project uses other libraries of mine, the [MBW.Client.BlueRiiotApi](https://github.com/LordMike/MBW.Client.BlueRiiotApi) ([nuget](https://www.nuget.org/packages/MBW.Client.BlueRiiotAPI)) and [MBW.HassMQTT](https://github.com/LordMike/MBW.HassMQTT) ([nuget](https://www.nuget.org/packages/MBW.HassMQTT)).

_This project is not affiliated with or endorsed by Blue Riiot._

# Features

* Creates binary sensors indicating issues with this service, or the BlueRiiot webservice
* Creates sensors for each pool in Blue Riiot
  * Tracks the latest measurements for pH, temperature, Cyanuric Acid and Alkalinity
  * Tracks warning / danger levels for all measurements
  * Weather forecast, with temperature, UV index and weather type e.g. 'rain'
  * Notifies when actions need to be done (use the Blue Riiot app to get more details on steps)
  * Pump schedules, also has commands to set pump schedules
* Ability to cope with metrics databases, by reporting unchanged values
* Creates sensors for each Blue device, with their battery status
* Automatically polls closely to the Blue device's reportings, to get 'live' data
* Ability to send commands to BlueRiiot2MQTT to interact with BlueRiiot.

Todo:

* Identify MIA Blue Connects
* Get more detailed states from Blue Connect (warnings and errors)

# Running it / Docker images

This software is distributed as ready-to-run docker images. Use one of the methods below to install it:

* Run directly using the docker CLI
* Run using docker-compose
* Install into HASS using this HASSIO addon

## Run in Docker CLI

> docker run -d -e MQTT__Server=myqueue.local -e BlueRiiot__Username=myuser -e BlueRiiot__Password=mypassword lordmike/blueriiot2mqtt:latest

## Run in Docker Compose

```yaml
# docker-compose.yml
version: '2.3'

services:
  blueriiot2mqtt:
    image: lordmike/blueriiot2mqtt:latest
    environment:
      MQTT__Server: myqueue.local
      BlueRiiot__Username: myuser
      BlueRiiot__Password: mypassword
```

## HASSIO add-on

I've prepared a repository [with a HASSIO addon](https://github.com/LordMike/hass-addons/) that you can install. 

## Available tags

You can use one of the following tags. Architectures available: `amd64`, `armv7` and `aarch64`

* `latest` (latest, multi-arch)
* `ARCH-latest` (latest, specific architecture)
* `vA.B.C` (specific version, multi-arch)
* `ARCH-vA.B.C` (specific version, specific architecture)

For all available tags, see [Docker Hub](https://hub.docker.com/repository/docker/lordmike/blueriiot2mqtt/tags).

# Setup

## Environment Variables

| Name | Required | Default | Note |
|---|---|---|---|
| MQTT__Server | yes | | A hostname or IP address |
| MQTT__Port | | 1883 | |
| MQTT__Username | | | |
| MQTT__Password | | | |
| MQTT__ClientId | | `blueriiot2mqtt` | |
| MQTT__ReconnectInterval | | `00:00:30` | How long to wait before reconnecting to MQTT |
| HASS__DiscoveryPrefix | | `homeassistant` | Prefix of HASS discovery topics |
| HASS__TopicPrefix | | `blueriiot` | Prefix of state and attribute topics |
| HASS__EnableHASSDiscovery | | `true` | Enable or disable the HASS discovery documents, disable with `false` |
| BlueRiiot__Username | yes | | |
| BlueRiiot__Password | yes | | |
| BlueRiiot__DiscoveryInterval | | 12:00:00 | How often new/removed pools should be checked, default: `12 hours` |
| BlueRiiot__UpdateInterval | | 01:00:00 | Fallback update interval, default: `1 hour` |
| BlueRiiot__UpdateIntervalJitter | | 00:02:00 | Update interval jitter, when BR reports reading interval, default: `2 minutes` |
| BlueRiiot__UpdateIntervalWhenAllDevicesAsleep | | | When all blue devices are asleep, use this interval, default: `BlueRiiot__UpdateInterval` |
| BlueRiiot__MaxBackoffInterval | | 03:00:00 | When the Blue device is not reporting data as it should, BlueRiiot2MQTT will backoff up to this value. Default: `3 hours` |
| BlueRiiot__Language | | `en` | Language for the API. Used for messages from BlueRiiot |
| BlueRiiot__ReportUnchangedValues | | `false` | Send unchanged values |
| Proxy__Uri | | | Set this to pass BlueRiiot API calls through an HTTP proxy |

# MQTT Commands

It is possible to send certain commands to the BlueRiiot2MQTT application, using MQTT topics. The following commands can be sent.

## Force sync
**Topic:** (prefix)/commands/force_sync

Sending a message to this topic will force the BR2MQTT app to poll BlueRiiot for new information.

## Set pump schedule
**Topic:** (prefix)/commands/pool/(pool_id)/set_pump_schedule

*At present, all times are in UTC timezone.*

Sending a message to this topic will configure the pump schedule for the specified pool. The accepted messages are: `none` - indicates no pump is present; `manual` - indicates the pump runs at manual intervals; or a schedule, as specified below:

> You can specify the intervals at which the pump runs, by making a JSON array of times. This example sets two intervals from `06:00 to 12:00` and `18:00 to 22:00`:
> 
> [["06:00", "12:00"],["18:00", "22:00"]]

The `pool_id` can be found on an attribute in most sensors within HASS. It is also used in topics related to values from that pool.

# How

Officially, Blue Riiot does _not_ have any API available. They have a number of integrations with select third parties, such as Alexa or IFTTT. I found these to be lacking for me, as I want to bring all my data into my domain, such as in my local HASS setup.

The API used here is reverse engineered from the apps that Blue Riiot offers.

# Troubleshooting

## Log level

Adjust the logging level using this environment variable:

> Logging__MinimumLevel__Default: Error | Warning | Information | Debug | Verbose

## HTTP Requests logging

Since this is a reverse engineering effort, sometimes things go wrong. To aid in troubleshooting, the requests and responses from the Blue Riiot API can be dumped to the console, by enabling trace logging.

Enable request logging with this environment variable:
> Logging__MinimumLevel__Override__MBW.Client.BlueRiiotApi: Verbose
