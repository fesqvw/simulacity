# 0008. Simulation approach: a shared world, with agent-based simulation first

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

The README wants to compare two techniques on the same city: a classic city-wide (aggregate) simulation and a purely agent-based one. The overall behaviour of the city (traffic, growth, how land is used) should come from how its parts interact.

Both techniques work on the same things: the map, the roads and the buildings ([0007](0007-map-geometry.md)). They differ in how they move people and goods and how they update the city each step.

## Options considered

- **Aggregate first**: model flows and zone statistics city-wide. It's cheap to run on large maps, but nothing moves through the network as individual entities, which the "first playable slice" and the vision both want.
- **Agent-based only**: simulate individual people and vehicles, with no abstraction for other techniques. It's the simplest start, but adding an aggregate model later would mean pulling the world apart from the agent logic after the fact.
- **A shared world, with the technique behind an interface, agent-based first**: the world model doesn't depend on any technique. A simulation technique is a pluggable part that reads and updates the world each step. The agent-based technique is built first, and an aggregate one can be added later on the same world.

## Decision

Use **a shared world, with the technique behind an interface, agent-based first**.

- The **world** (map, roads, land, and later buildings) is its own model in `Simulacity.Core`. It doesn't know which technique is running.
- A **simulation technique** implements an interface that advances the world by one step. `Simulation` owns the world and one technique, and calls it on each tick.
- The first technique is **agent-based**: individual agents (people, vehicles) with their own state, moving through the road graph.
- The interface stays minimal until a second technique exists. Don't design it around a hypothetical aggregate model.

## Consequences

- The aggregate model can be added later and run on the same city, which is what the comparison needs.
- World code mustn't contain technique-specific logic. Anything that only one technique needs belongs to that technique.
- Agent-based simulation gets expensive on large maps (for example an imported city). Performance will need attention before OpenStreetMap import is useful.
- The renderer draws the world and, for now, agents. What exactly gets sent is still decided separately (see [0005](0005-render-channel.md)).
