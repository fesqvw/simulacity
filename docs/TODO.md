# TODO

## Now: project foundations

- [x] Reset the repository
- [x] Write the README, CLAUDE.md and the docs structure
- [x] Choose the language and runtime: C# / .NET ([0003](decisions/0003-csharp-dotnet.md))
- [x] Choose the first front end: a console simulator that takes commands, and a separate console renderer ([0004](decisions/0004-console-front-end.md))
- [ ] Decide how the simulator sends state to the renderer: the transport, how the renderer starts, and what is sent (decision record, see the open questions in 0004)
- [ ] Choose the simulation approach to start with: aggregate, agent-based, or both behind one interface (decision record)
- [ ] Choose how the map and geometry are represented (grid, graph, splines…) (decision record)
- [ ] Set up the .NET solution: an engine-agnostic core class library, a test project, and the two console apps (the simulator, which takes commands, and the renderer). Pick a target framework that Godot can reference.
- [ ] Document the build, test and run commands in the README and CLAUDE.md

## Next: first playable slice

- [ ] Load or define a small map (ground and water)
- [ ] Roads and their topology (graph of intersections and segments)
- [ ] Render the map

## Later

- [ ] Buildings: residential, industrial, commercial
- [ ] Transport: foot, car, then public transport (bus, tram, train, subway)
- [ ] Entities moving through the network
- [ ] Connect the simulation core to a Godot project
- [ ] Import real cities from OpenStreetMap
- [ ] *(optional)* Utilities: water, electricity
- [ ] *(optional)* Special industry

## Lessons from the previous attempt

These are things to keep in mind, not tasks.

- The geometry model was reworked several times ("feeling it does not make sense"). Settle the geometry and topology model early, in a decision record.
- Road intersections (bounding boxes, a spatial index such as a BVH, segment intersection) were the next hard problem. Plan for it.
