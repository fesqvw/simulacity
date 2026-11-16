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

The simulator starts with the **starting map**, [`maps/starter.txt`](maps/starter.txt): 1600 m × 800 m with a downtown grid, a river with a bridge and a tunnel, a lake, a hill and an interchange ([decision 0013](docs/decisions/0013-map-scripts.md)). The map is a script of simulator commands, in sections. To try out a new feature, add a section for it. Pass `--map <file>` to start with another map script, or `--empty` for an empty 640 m × 320 m map:

```sh
dotnet run --project src/Simulacity.Simulator -- --empty
```

The renderer draws the map from above, with north at the top: `.` ground, `~` water, `#` road, `=` bridge, `:` tunnel and `o` node. It starts at one row per 10 m cell. Move around with **WASD** or the **arrow keys**, zoom with **`+`** and **`-`**, and press **`F`** to fit the whole map ([decision 0014](docs/decisions/0014-renderer-view-controls.md)). The land is coloured by height, from dark green for the lowest ground to light grey for the highest, with a key under the map. Press **`H`** to turn the colours off ([decision 0015](docs/decisions/0015-height-as-colour.md)).

For example, on an empty map, to dig a river and build a road across it on a bridge:

```text
land water 30 0 33 31
road 200,160 440,160 290,160,8 350,160,8
roads
```

This turns a column of cells (300–340 m from the west edge) into water. It then builds a road from a node on the west bank to one on the east bank, which rises to an 8 m deck over the water. A road's ends can be nodes (`n1`) or positions in metres (`x,y`, or `x,y,z`). A position uses the node already there, or adds one.

A road can't cross water at ground level, so `road 200,160 440,160` would be refused. Where a road is more than 1 m above the ground or water it becomes a bridge, and more than 1 m below it a tunnel, with no extra command ([decision 0012](docs/decisions/0012-bridges-and-tunnels-from-height.md)).

Roads that cross at the same height are joined at a new junction node, and the roads they cross are split there ([decision 0011](docs/decisions/0011-roads-meet-at-crossings.md)). A road that passes more than 1 m above or below another, like the bridge above, doesn't meet it. Continuing the example, this adds two roads on the ground west of the river that cross at a new junction:

```text
road 50,100 250,100
road 150,40 150,200
```

The terrain can be shaped with `height` (one corner, or a rectangle of corners) and `hill`. `load <file>` runs a map script, `new <width> <height>` starts an empty map, and `help` lists every command.

## Project layout

| Path | What it is |
| --- | --- |
| `src/Simulacity.Core` | The simulation |
| `src/Simulacity.Contracts` | The interface for sending state to renderers |
| `src/Simulacity.Transport.NamedPipes` | That interface over named pipes |
| `src/Simulacity.Simulator` | Console app: takes commands and runs the simulation |
| `src/Simulacity.Renderer.Console` | Console app: draws the simulation |
| `maps` | Map scripts, including the starting map |
| `tests/Simulacity.Tests` | Tests |

See [decision 0006](docs/decisions/0006-solution-layout.md) for why it's laid out this way.
