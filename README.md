# BlueRiiot2MQTT

This is a proxy application to translate the status of a Blue Riiot pool manager, to Home Assistant using MQTT. You can run this application in docker, and it will periodically poll the Blue Riiot API for updates.

This project is not affiliated with or endorsed by Blue Riiot.

# Features

* Creates sensors for each pool in Blue Riiot
  * Tracks the latest measurements for pH, temperature, Cyanuric Acid and Alkalinity
  * Tracks warning / danger levels for the measurements
  * Weather forecast, with temperature, UV index and weather type e.g. 'rain'

Todo:

* Track battery status
* Identify MIA Blue Connects
* Get more detailed states from Blue Connect (warnings and errors)
* Get recommendations from Blue Riiot (put in chemicals, etc..)
* Adjust the polling interval automatically

# Setup

## Environment Variables

| Name | Required | Default | Note |
|---|---|---|---|
| MQTT__Server | yes | | A hostname or IP address |
| MQTT__Port | | 1883 | |
| MQTT__Username | | | |
| MQTT__Password | | | |
| MQTT__ClientId | | `blueriiot2mqtt-RANDOM` | |
| MQTT__ReconnectInterval | | `00:00:30` | How long to wait before reconnecting to MQTT |
| HASS__DiscoveryPrefix | | `homeassistant` | Prefix of HASS discovery topics |
| HASS__BlueRiiotPrefix | | `blueriiot2mqtt` | Prefix of state and attribute topics |
| BlueRiiot__Username | yes | | |
| BlueRiiot__Password | yes | | |
| BlueRiiot__UpdateInterval | | 01:00:00 | Update interval, default: `1 hour` |
| BlueRiiot__Language | | `en` | Language for the API. Used for messages from BlueRiiot |
| Proxy__Uri | | | Set this to pass BlueRiiot API calls through an HTTP proxy |

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
> Logging__MinimumLevel__MBW.Client.BlueRiiotApi: Verbose
