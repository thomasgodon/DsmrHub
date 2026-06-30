# DsmrHub

DsmrHub reads Dutch/Belgian Smart Meter (DSMR) telegrams from a serial port (or a built-in
simulator) and fans the parsed readings out to a configurable set of sinks: an embedded **MQTT**
broker, a **KNX** bus, **Azure IoT Hub**, and **UDP**. It also serves an optional read-only **web
dashboard** that shows every DSMR value live.

The codebase follows a Domain-Driven Design / Clean Architecture layout: a rich domain model
decoupled from the parsing library, an application layer of use cases and ports, an infrastructure
layer of adapters, and a thin host. Readings are dispatched to the sinks as a domain event via
[MediatR](https://github.com/jbogard/MediatR).

## Solution layout

```
DsmrHub.slnx
├─ DsmrHub.Domain          Entities, value objects, domain events. No external dependencies
│                          (except MediatR.Contracts for the notification marker).
├─ DsmrHub.Application     Ports (ITelegramParser, IMeterReadingSource) and the
│                          TelegramIngestionService use case. Depends only on Domain + MediatR.
├─ DsmrHub.Infrastructure  Adapters: in-house DSMR/e-MUCS P1 parser, serial + simulator sources, and one
│                          MediatR notification handler per sink (MQTT, KNX, IoT Hub, UDP) plus the
│                          dashboard snapshot handler.
├─ DsmrHub                 Web host (composition root): wiring, configuration, Worker, and the
│                          dashboard endpoints (static page + Server-Sent Events).
└─ DsmrHub.Tests           xUnit tests for value objects, the aggregate, and telegram mapping.
```

Dependency direction is enforced by project references: `Domain ← Application ← Infrastructure ← Host`.

## Pipeline

```
IMeterReadingSource (serial | simulator)
    → raw telegram string
    → TelegramIngestionService
    → ITelegramParser  (DsmrTelegramParser: in-house P1 parse + CRC16 check → MeterReading)
    → IPublisher.Publish(MeterReadingReceived)
    → INotificationHandler<MeterReadingReceived>  ×  { MQTT, KNX, IoT Hub, UDP, Dashboard }
```

Each sink handler is gated by its own `Enabled` flag, so disabled sinks are no-ops.

## Web dashboard

When `DashboardOptions:Enabled` is `true` (the default), the host binds Kestrel to
`DashboardOptions:Port` (default `8080`) and serves a single-page, read-only dashboard at
`http://<host>:<port>/` showing **all** DSMR values: identity, energy registers, live power,
per-phase voltage/current/power, gas, water, every M-Bus channel, the long power-failure log and the
monthly max-demand history. Any value missing from the telegram (or that could not be parsed) simply
renders blank ("—").

The dashboard's `MeterReadingDashboardHandler` projects each reading to a `DashboardSnapshot` and
pushes it as JSON over **Server-Sent Events** (`/events`); the page updates on every telegram with no
polling. When `DashboardOptions:Enabled` is `false`, no HTTP port is bound and the host behaves like a
plain worker.

## Domain model

`MeterReading` is the aggregate root. Measurements are unit-aware value objects that validate their
own invariants: `EnergyValue` (kWh), `PowerValue` (kW, with a `Watts` helper), `VoltageValue` (V),
`CurrentValue` (A), `GasVolume` / `WaterVolume` (m3). Per-phase data (incl. voltage sags/swells) is
grouped in `ElectricityPhase`; electricity, gas and water measurements live in `ElectricityReading` /
`GasReading` / `WaterReading`, and every M-Bus channel is also preserved verbatim in `MBusDevices`.
`ElectricityReading` additionally captures the Fluvius e-MUCS extras: power-failure counts and log,
breaker state, limiter/fuse thresholds, and the monthly max-demand history. A raw telegram is
tokenised by OBIS code (`P1Telegram`, CRC16-validated) and mapped to this model in one place —
`MeterReadingFactory` (infrastructure) — so the rest of the code is OBIS-agnostic.

## Requirements

- .NET 10 SDK (LTS)

## Build, test, run

```pwsh
dotnet build DsmrHub.slnx
dotnet test  DsmrHub.slnx
dotnet run --project DsmrHub
```

To replay the bundled example telegram stream instead of reading a serial port, set
`DsmrOptions:UseExampleTelegram` to `true`.

## Running with Docker

A multi-stage `Dockerfile` publishes a framework-dependent image on `mcr.microsoft.com/dotnet/aspnet:10.0`
and runs as the non-root `$APP_UID` user. The dashboard is exposed on port `8080`.

Config is supplied via environment variables using the standard double-underscore convention
(`Section__Key`, e.g. `DsmrOptions__ComPort`).

### Quickstart (docker compose)

`docker-compose.yml` passes a host serial adapter into the container and maps the dashboard port:

```pwsh
docker compose up -d
```

Then open `http://localhost:8080`. Adjust the `devices:` host path (`/dev/ttyUSB0`,
`/dev/ttyAMA0`, `/dev/serial/by-id/...`) and the `environment:` overrides to match your setup.

### docker run

```pwsh
# Real meter: pass the serial device through and point ComPort at it
docker run -d --name dsmrhub --device=/dev/ttyUSB0 `
  -e DsmrOptions__ComPort=/dev/ttyUSB0 -p 8080:8080 `
  ghcr.io/thomasgodon/dsmrhub:latest

# No hardware: replay the bundled example telegram
docker run --rm -e DsmrOptions__UseExampleTelegram=true -p 8080:8080 `
  ghcr.io/thomasgodon/dsmrhub:latest
```

Note: serial port reads require a Linux host with the P1 USB cable attached — Docker `--device`
passthrough is not available on Docker Desktop for Windows/macOS, so use `UseExampleTelegram`
(or the UDP source) when running there.

## Releases

Pushing a `vX.Y.Z` tag triggers the **Docker Publish** workflow
(`.github/workflows/docker-publish.yml`), which builds the image and pushes it to
`ghcr.io/thomasgodon/dsmrhub` tagged with the full version, `major.minor`, and `latest`:

```pwsh
git tag v1.0.0
git push origin v1.0.0
```

Pushes and PRs to `main` run the **Integration Tests** workflow
(`.github/workflows/integration-tests.yml`): restore, build, and `dotnet test` against
`DsmrHub.slnx`, with test results uploaded as an artifact.

## Configuration

All configuration lives in `DsmrHub/appsettings.json`. Every sink is disabled by default.

| Section            | Purpose                                                               |
|--------------------|-----------------------------------------------------------------------|
| `DsmrOptions`   | Serial port (`ComPort`, `BaudRate`, `Parity`, `StopBits`, `DataBits`, `ReceiveTimeout`) and simulator (`UseExampleTelegram`, `SimulationRateInSeconds`). |
| `DashboardOptions` | `Enabled` (bind the web dashboard, default `true`) and `Port` (default `8080`). |
| `MqttOptions`   | `Enabled`, `Port`, `Username`, `Password` for the embedded broker.       |
| `UdpOptions`    | `Enabled`, `Host`. Values are broadcast to fixed ports 10000–10033.      |
| `IotHubOptions` | `IotDevices[]` — DPS-provisioned devices (`IdScope`, `DeviceId`, keys, `ProvisioningUri`, `SendInterval`). The reading is sent as JSON. |
| `KnxOptions`    | `Enabled`, `Host`, `Port`, `IndividualAddress` (e.g. `"1.1.1"`), and `GroupAddressMapping` (capability → KNX group address). |

### Sink notes

- **MQTT** publishes per-value topics under `dsmr/` (e.g. `dsmr/electricity/power-delivered`,
  `dsmr/gas/delivered`) with real values.
- **UDP** preserves the original fixed port assignments (e.g. `10015` = power delivered in kW,
  `10033` = gas delivered in m3). Every port is sent on each reading; absent values send an empty payload.
- **KNX** `GroupAddressMapping` keys must match the capability names (`PowerDelivered`,
  `EnergyDeliveredTariff1`, `GasDelivered`, …). Byte encodings and scale factors match the bus
  expectations (power in W, energy in kWh, running-month max as a `uint`).
- **IoT Hub** serializes the `MeterReading` to JSON with `System.Text.Json`.

## Known warnings

The Azure IoT Hub 1.x client (and KNX SDK) pull in transitive packages flagged by NuGet audit
(`log4net`, `System.Drawing.Common`, `System.Security.Cryptography.Xml`). These are build-time
`NU1902/NU1904` warnings only. Moving to the Azure IoT 2.x client (a separate API migration) would
retire them.

> **MediatR licensing:** MediatR is commercially licensed for production use above its revenue
> threshold. Review the license before deploying commercially.
