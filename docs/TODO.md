# TODO

## Now: project foundations

- [x] Reset the repository
- [x] Write the README, CLAUDE.md and the docs structure
- [x] Choose the language and runtime: C# / .NET ([0003](decisions/0003-csharp-dotnet.md))
- [x] Choose the first front end: a console simulator that takes commands, and a separate console renderer ([0004](decisions/0004-console-front-end.md))
- [x] Decide how the simulator sends state to the renderer: through an interface, using named pipes first ([0005](decisions/0005-render-channel.md))
- [x] Decide how the renderer is started: by hand, in a second terminal ([0005](decisions/0005-render-channel.md))
- [x] Choose the simulation approach: a shared world, with agent-based simulation first ([0008](decisions/0008-simulation-approach.md))
- [x] Choose how the map and geometry are represented: a road graph with polyline segments, and a land grid ([0007](decisions/0007-map-geometry.md))
- [x] Set up the .NET solution: core, contracts, named-pipe transport, simulator, renderer and tests, targeting `net10.0` ([0006](decisions/0006-solution-layout.md))
- [x] Document the build, test and run commands in the README and CLAUDE.md

## Next: first playable slice

- [ ] Once work on the simulation model has started, decide what state the simulator sends to renderers and in what format (decision record, see 0005)
- [ ] Load or define a small map (ground and water)
- [ ] Roads and their topology (graph of intersections and segments)
- [ ] Split segments where a new road crosses an existing one (segment intersection, then a spatial index)
- [ ] Render the map

## Later

- [ ] Buildings: residential, industrial, commercial
- [ ] Transport: foot, car, then public transport (bus, tram, train, subway)
- [ ] Entities moving through the network
- [ ] Spline road segments for smooth curves, as a second segment shape alongside polylines ([0007](decisions/0007-map-geometry.md))
- [ ] Connect the simulation core to a Godot project (first check that the Godot version can reference `net10.0` libraries, see 0006)
- [ ] Import real cities from OpenStreetMap
- [ ] *(optional)* Utilities: water, electricity
- [ ] *(optional)* Special industry

## Lessons from the previous attempt

These are things to keep in mind, not tasks.

- The geometry model was reworked several times ("feeling it does not make sense"). Settle the geometry and topology model early, in a decision record.
- Road intersections (bounding boxes, a spatial index such as a BVH, segment intersection) were the next hard problem. Plan for it.
