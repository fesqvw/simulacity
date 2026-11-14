# 0003. Use C# and .NET

- **Status:** Accepted
- **Date:** 2026-11-14

## Context

The project needs a language and runtime. The plan is to connect the simulation to a Godot project at some point, as its visual front end. Godot supports C# scripting through its .NET build, so a C# simulation can be used from Godot directly. The first attempt was also written in C# / .NET.

## Options considered

- **C# / .NET**: Godot can use it directly, and it's already familiar from the first attempt.
- **Other languages**: would need bindings or a separate process to talk to Godot. Not pursued.

## Decision

Write Simulacity in C# on .NET.

## Consequences

- Keep the simulation core in a class library that doesn't depend on Godot or on any renderer. A Godot project can then reference it, and other front ends (console, tests) can use the same core.
- Choose the core library's target framework so that the Godot version in use can reference it. Check this when the project is set up and again when Godot is integrated.
- Use the standard .NET tooling: the `dotnet` CLI, a solution file, and a test project.
