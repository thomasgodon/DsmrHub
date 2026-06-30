# DsmrHub

DsmrHub reads Dutch/Belgian Smart Meter (DSMR) telegrams from a serial port (or a built-in
simulator) and fans the parsed readings out to a configurable set of sinks: an embedded **MQTT**
broker, a **KNX** bus, **Azure IoT Hub**, and **UDP**.

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
├─ DsmrHub.Infrastructure  Adapters: DSMR parser/mapper, serial + simulator sources, and one
│                          MediatR notification handler per sink (MQTT, KNX, IoT Hub, UDP).
├─ DsmrHub                 Console worker (composition root): wiring, configuration, Worker.
└─ DsmrHub.Tests           xUnit tests for value objects, the aggregate, and telegram mapping.
```

Dependency direction is enforced by project references: `Domain ← Application ← Infrastructure ← Host`.

## Pipeline

```
IMeterReadingSource (serial | simulator)
    → raw telegram string
    → TelegramIngestionService
    → ITelegramParser  (DsmrTelegramParser: DSMRParser.Net Telegram → MeterReading)
    → IPublisher.Publish(MeterReadingReceived)
    → INotificationHandler<MeterReadingReceived>  ×  { MQTT, KNX, IoT Hub, UDP }
```

Each sink handler is gated by its own `Enabled` flag, so disabled sinks are no-ops.

## Domain model

`MeterReading` is the aggregate root. Measurements are unit-aware value objects that validate their
own invariants: `EnergyValue` (kWh), `PowerValue` (kW, with a `Watts` helper), `VoltageValue` (V),
`CurrentValue` (A), `GasVolume` (m3). Per-phase data is grouped in `ElectricityPhase`; electricity and
gas measurements live in `ElectricityReading` / `GasReading`. The DSMR library's `Telegram` is mapped
to this model in one place — `TelegramMapper` (infrastructure) — so the rest of the code is
library-agnostic.

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

## Configuration

All configuration lives in `DsmrHub/appsettings.json`. Every sink is disabled by default.

| Section         | Purpose                                                                  |
|-----------------|--------------------------------------------------------------------------|
| `DsmrOptions`   | Serial port (`ComPort`, `BaudRate`, `Parity`, `StopBits`, `DataBits`, `ReceiveTimeout`) and simulator (`UseExampleTelegram`, `SimulationRateInSeconds`). |
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
