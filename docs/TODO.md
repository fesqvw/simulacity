# TODO

## Now: project foundations

- [x] Reset the repository
- [x] Write the README, CLAUDE.md and the docs structure
- [ ] Choose the tech stack: language, runtime, rendering (decision record)
- [ ] Choose the simulation approach to start with: aggregate, agent-based, or both behind one interface (decision record)
- [ ] Choose how the map and geometry are represented (grid, graph, splines…) (decision record)
- [ ] Set up the project skeleton, build, tests and `.gitignore` for the chosen stack
- [ ] Document the build, test and run commands in the README and CLAUDE.md

## Next: first playable slice

- [ ] Load or define a small map (ground and water)
- [ ] Roads and their topology (graph of intersections and segments)
- [ ] Render the map

## Later

- [ ] Buildings: residential, industrial, commercial
- [ ] Transport: foot, car, then public transport (bus, tram, train, subway)
- [ ] Entities moving through the network
- [ ] Import real cities from OpenStreetMap
- [ ] *(optional)* Utilities: water, electricity
- [ ] *(optional)* Special industry

## Lessons from the previous attempt

These are things to keep in mind, not tasks.

- The geometry model was reworked several times ("feeling it does not make sense"). Settle the geometry and topology model early, in a decision record.
- Road intersections (bounding boxes, a spatial index such as a BVH, segment intersection) were the next hard problem. Plan for it.
