# 0014. The renderer can move its view around the map

- **Status:** Accepted
- **Date:** 2026-11-15
- **Supersedes:** [0004](0004-console-front-end.md)

## Context

[0004](0004-console-front-end.md) split the console front end into a simulator that takes commands and a renderer that only draws. It said the renderer doesn't keep its own state or accept commands.

The renderer scaled the whole map to fit the window. With the starting map ([0013](0013-map-scripts.md)), 160 × 80 cells, that's too coarse to read. The user asked for the renderer to accept WASD and the arrow keys to move around when the map is too big. That means the renderer takes key presses and remembers what part of the map it shows.

Everything else in 0004 still holds. This record restates it with that change, so 0004 can be fully superseded.

## Options considered

- **Move the view from the simulator, with commands**: keeps the renderer free of input, but the view would be shared by every renderer, and moving around would mean switching windows to type.
- **Keys in the renderer, for the view only**: each renderer has its own view. The keys only change what's drawn, never the simulation.

## Decision

The console front end is two programs, each with its own console:

- **The simulator** reads commands from its console. It owns and runs the simulation, and is the only program that changes its state.
- **The renderer** draws the state the simulator sends it. It doesn't run the simulation or keep a copy of its state beyond the latest snapshot.
- **The renderer has a view**: which part of the map is on screen, and at what scale. It's the renderer's only state of its own, and its keys only change the view:
  - **WASD or the arrow keys** move the view by a quarter of the screen. The middle of the view stays on the map.
  - **`+` and `-`** zoom in and out by a factor of two, from four rows per cell to the whole map.
  - **`F`** fits the whole map in the window.
  - **`Q`, Escape or Ctrl+C** exit.
- The renderer starts at **one row per cell**, centred on the map, or with the whole map if it fits at that scale. A new map (a different size) resets the view.

## Consequences

- Big maps can be read at full detail and still be seen whole with `F`.
- Data still goes in one direction only, from the simulator to the renderer. The renderer can be closed, restarted or moved around without affecting the simulation.
- Two renderers can show different parts of the map.
- A Godot front end will have its own camera, which is the same idea.
- Godot can later take the renderer's place, or sit beside it as another renderer, and rendering stays out of the simulation core, as [0003](0003-csharp-dotnet.md) requires.
