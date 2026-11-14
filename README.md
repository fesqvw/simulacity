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

Simulacity is written in C# / .NET, and a Godot front end is planned. Build instructions will be added once the .NET solution is set up (see [`docs/TODO.md`](docs/TODO.md)).
