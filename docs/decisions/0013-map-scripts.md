# 0013. Maps are command scripts, and the simulator starts with a starting map

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

The simulator started with an empty 64 × 32 map, so every session began by typing the same commands to get anything to look at. The user asked for a starting map with a bit of everything, which can grow a section at a time to test new features.

That needs a format for maps. It should be easy to read and to edit by hand, and adding a section shouldn't mean renumbering the rest.

## Options considered

- **Build the map in C# code**: type-checked, but every change needs a rebuild, and it duplicates what the commands already do.
- **A data format (JSON, or an image for the land)**: compact for big maps, but it needs its own loader and checks, and it's hard to edit roads by hand.
- **A script of simulator commands**: no new format, the same checks as typing the commands (water, crossings), and readable with comments. It's slower for very large maps, and node IDs depend on the order of commands.

## Decision

- **A map is a script of simulator commands**, one per line, where `#` starts a comment. The `load <file>` command runs one, and scripts can load other scripts.
- A script **stops at the first command that fails**, and reports the file and line. What the earlier lines did stays. The state is published once at the end, not after every line.
- **`new <width> <height> [cell size]`** starts an empty map. A map script starts with it.
- **Road ends can be positions** (`road 100,100 500,100`) as well as node IDs. The node already at that position is used, or a new one is added. That way, scripts never need node IDs, and sections can be added, removed or reordered without breaking each other.
- `height` also takes a rectangle of corners, and **`hill <x> <y> <radius> <height>`** adds a smooth bump (a cosine shape) to the terrain, so scripts can shape land in a few lines.
- **The starting map** is [`maps/starter.txt`](../../maps/starter.txt): 1600 m × 800 m, in sections (terrain and water, a downtown grid, river crossings, the east side, a lake, a hill, an interchange). The simulator loads it at startup unless it's given `--map <file>` or `--empty`.
- To test a new feature, **add a section for it** to the starting map, on a part of the map that's still free. A test checks that every command in the starting map succeeds.

## Consequences

- The starting map exercises the rules as they're added: if a change breaks a section (for example, a road that now gets refused), the starting map test fails.
- Maps stay small and readable. A large map, such as an OpenStreetMap import, will need a different format, and that will be a new decision.
- A script can't refer to nodes by ID safely, since IDs depend on what ran before. Positions are the way to name places.
