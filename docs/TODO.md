# TODO

## Now: project foundations

- [x] Reset the repository
- [x] Write the README, CLAUDE.md and the docs structure
- [x] Choose the language and runtime: C# / .NET ([0003](decisions/0003-csharp-dotnet.md))
- [x] Choose the first front end: a console simulator that takes commands, and a separate console renderer ([0004](decisions/0004-console-front-end.md))
- [x] Decide how the simulator sends state to the renderer: through an interface, using named pipes first ([0005](decisions/0005-render-channel.md))
- [x] Decide how the renderer is started: by hand, in a second terminal ([0005](decisions/0005-render-channel.md))
- [x] Choose the simulation approach: a shared world, with agent-based simulation first ([0008](decisions/0008-simulation-approach.md))
- [x] Choose how the map and geometry are represented: a road graph with polyline segments, and a land grid ([0009](decisions/0009-3d-world.md))
- [x] Set up the .NET solution: core, contracts, named-pipe transport, simulator, renderer and tests, targeting `net10.0` ([0006](decisions/0006-solution-layout.md))
- [x] Document the build, test and run commands in the README and CLAUDE.md

## Next: first playable slice

- [x] Decide what the simulator sends to renderers: full snapshots of land, terrain and road polylines, as JSON ([0010](decisions/0010-snapshot-contents.md))
- [x] Define a starting map with a bit of everything, as a command script the simulator loads at startup (`maps/starter.txt`, [0013](decisions/0013-map-scripts.md)). Add a section to it for each new feature
- [x] Let the renderer move around big maps: WASD or arrows, `+`/`-` to zoom, `F` to fit ([0014](decisions/0014-renderer-view-controls.md))
- [ ] Show terrain height in the console renderer (the starting map's hill can't be seen yet)
- [x] Simulator commands to add roads, paint land and set terrain height (`node`, `road`, `remove`, `roads`, `land`, `height`)
- [x] Land grid and road graph (intersections and polyline segments) in `Simulacity.Core`, owned by `World`
- [x] Make the world 3D: z elevation on every point, and a terrain heightmap on the land grid ([0009](decisions/0009-3d-world.md))
- [x] Decide what happens when a road runs over water at ground level: it is refused, and water is crossed on bridges or in tunnels, worked out from height ([0012](decisions/0012-bridges-and-tunnels-from-height.md))
- [x] Split segments where a new road crosses an existing one at the same height. Roads at different heights pass over or under each other ([0011](decisions/0011-roads-meet-at-crossings.md))
- [ ] Add a spatial index for road pieces, so adding a road doesn't check every segment (0011)
- [ ] Handle a road that crosses itself, and decide whether two nodes in the same place should be merged (0011)
- [x] Render the map: land, roads, bridges, tunnels and nodes, top-down and scaled to fit the console (`MapView`)

## Later

- [ ] Buildings: residential, industrial, commercial
- [ ] Transport: foot, car, then public transport (bus, tram, train, subway)
- [ ] Entities moving through the network
- [x] Bridges and tunnels: only derived from road height against terrain, never stored ([0012](decisions/0012-bridges-and-tunnels-from-height.md))
- [ ] Bridge clearance and maximum road slope, once building has a cost (0012)
- [ ] Spline road segments for smooth curves, as a second segment shape alongside polylines ([0009](decisions/0009-3d-world.md))
- [ ] Connect the simulation core to a Godot project (first check that the Godot version can reference `net10.0` libraries, see 0006)
- [ ] Import real cities from OpenStreetMap
- [ ] *(optional)* Utilities: water, electricity
- [ ] *(optional)* Special industry

## Lessons from the previous attempt

These are things to keep in mind, not tasks.

- The geometry model was reworked several times ("feeling it does not make sense"). Settle the geometry and topology model early, in a decision record.
- Road intersections (bounding boxes, a spatial index such as a BVH, segment intersection) were the next hard problem. Plan for it.
