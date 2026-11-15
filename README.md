# Simulacity

A city simulation: roads, buildings, land and the people and vehicles moving through them.

> **Status:** starting over from scratch. Nothing is implemented yet. The previous attempt is still in the git history (commits before `9e0210f`).

## Vision

Simulate a city at a level of detail where the overall behaviour (traffic, growth, how land gets used) comes from how its individual parts interact.

### Planned features

- **Transport**
  - Foot
  - Car
  - Bus
  - Train
  - Tram
  - Subway
- **Buildings**
  - Residential
  - Industrial
  - Commercial
  - *(optional)* Special industry
- **Land**
  - Ground
  - Water
- *(optional)* **Utilities**
  - Water
  - Electricity

### Ideas to explore

- Import real cities from OpenStreetMap and simulate them.
- Compare simulation techniques on the same city:
  - a classic city-wide (aggregate) simulation
  - a purely agent-based simulation

## Documentation

Project documentation is in [`docs/`](docs/README.md):

- [`docs/TODO.md`](docs/TODO.md): what's next
- [`docs/decisions/`](docs/decisions/README.md): design decisions and why they were made

## Getting started

Simulacity is written in C# / .NET, and a Godot front end is planned. You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build   # build everything
dotnet test    # run the tests
```

The simulation runs as two programs, each in its own terminal:

```sh
# Terminal 1: the simulator. Type commands here ('help' lists them).
dotnet run --project src/Simulacity.Simulator

# Terminal 2: the renderer. It only draws what the simulator sends.
dotnet run --project src/Simulacity.Renderer.Console
```

You can start them in either order. The renderer waits for the simulator, and reconnects if the simulator restarts.

The simulator starts with a flat 640 m × 320 m map of ground. For example, to dig a river and build a road across it on a bridge:

```text
land water 30 0 33 31
node 200 160
node 440 160
road n1 n2 290,160,8 350,160,8
roads
```

This turns a column of cells (300–340 m from the west edge) into water, adds a node on the ground on each bank, and joins them with a road that rises to an 8 m deck over the water. `help` lists every command. The renderer draws the map from above, with north at the top: `.` ground, `~` water, `#` road, `=` bridge, `:` tunnel and `o` node.

## Project layout

| Path | What it is |
| --- | --- |
| `src/Simulacity.Core` | The simulation |
| `src/Simulacity.Contracts` | The interface for sending state to renderers |
| `src/Simulacity.Transport.NamedPipes` | That interface over named pipes |
| `src/Simulacity.Simulator` | Console app: takes commands and runs the simulation |
| `src/Simulacity.Renderer.Console` | Console app: draws the simulation |
| `tests/Simulacity.Tests` | Tests |

See [decision 0006](docs/decisions/0006-solution-layout.md) for why it's laid out this way.
