---
description: "Use when writing or editing Unity C# scripts in unity/Assets/BoulderingGym/Scripts/. Covers layered architecture (Core/Features/Infrastructure/UI), namespacing, MonoBehaviour boundaries, FSM usage, tuning injection, and Unity API anti-patterns."
applyTo: "unity/Assets/BoulderingGym/Scripts/**/*.cs"
---

# Unity C# — poc-bouldering-game

## Namespaces follow folder layout

Root namespace is `Project`. Mirror the path under `Scripts/`:

| Folder | Namespace |
| --- | --- |
| `Core/StateMachine/` | `Project.Core.StateMachine` |
| `Core/Events/` | `Project.Core.Events` |
| `Features/Climbing/Domain/` | `Project.Features.Climbing.Domain` |
| `Features/Climbing/Presentation/` | `Project.Features.Climbing.Presentation` |
| `Infrastructure/Persistence/` | `Project.Infrastructure.Persistence` |
| `UI/HUD/` | `Project.UI.HUD` |

One public type per file. File name = type name.

## Layer boundaries (enforce, do not invert)

- `Domain/` — pure C# only. **No `using UnityEngine;` for `MonoBehaviour`** (a `Vector3` value type is fine, but no scene-bound classes, no `GameObject`, no coroutines).
- `Application/` — orchestration of domain use cases. Still no `MonoBehaviour`.
- `Presentation/` — `MonoBehaviour`s, scene bindings, IK hooks, VFX/SFX glue. Holds no business rules.
- `Infrastructure/` — implements ports declared in `Core`. Never the other way around.

Dependency direction: `UI → Features → Core`, `Infrastructure → Core (ports)`. `Core` knows nothing about a concrete feature.

## State machines

Use the generic `StateMachine<TStateId, TTrigger, TContext>` from
[Scripts/Core/StateMachine/StateMachine.cs](../../unity/Assets/BoulderingGym/Scripts/Core/StateMachine/StateMachine.cs).
One class per state, implementing `IState<,,>`. Guards live in `HandleTrigger` — never an ad-hoc `if/else` cascade in a `MonoBehaviour.Update`. See ADR-004.

## Tuning values

Never hard-code a numeric constant from
[plan/04_technical/variables_gameplay.md](../../plan/04_technical/variables_gameplay.md).
Read it from a `TuningConfig` `ScriptableObject` injected through the
`ClimbingContext` (or equivalent feature context). Treat the SO as read-only at runtime. See ADR-003.

## Cross-system events

Use the local typed event bus in `Core/Events/`. Forbidden:
- `static event Action ...` on `MonoBehaviour`s,
- `UnityEvent` in code-driven systems (only acceptable on prefab-authored UI buttons),
- direct cross-feature references (`Features.Climbing` looking up `Features.Resources` via `FindObjectOfType`).

Event names match the contract: `HOLD_GRABBED`, `HOLD_SLIP`, `FATIGUE_CRITICAL`, `RUN_COMPLETED`, `RUN_FAILED`.

## Forbidden Unity APIs

- `GameObject.Find`, `GameObject.FindWithTag`, `FindObjectOfType`, `FindObjectsOfType` — wire references via `[SerializeField]`, prefab links, or a `Bootstrap` registry.
- `Resources.Load` at runtime — use `Addressables` or an injected reference.
- `Camera.main` in hot loops — cache once.
- `SendMessage` / `BroadcastMessage` — use the event bus.
- Allocations in `Update` / `FixedUpdate` (`new`, LINQ, string concat, boxing). Cache lists, reuse buffers.

## Logging

- `Debug.Log` only behind `#if UNITY_EDITOR` or a `DEBUG` build flag for hot-path code.
- Invalid FSM transitions: log with state name + attempted trigger (the generic `StateMachine` already does this — do not double-log).
- Never log per-frame in builds.

## Naming

- `PascalCase` for types, methods, properties, public fields.
- `_camelCase` for private fields (including serialized: `[SerializeField] private GameObject _holdPrefab;`).
- `camelCase` for locals and parameters.
- Constants: `PascalCase` (`public const int MaxHands = 2;`).
- Async methods end with `Async`.

## Code conventions inherited from root rules

- English only (identifiers, comments, log messages, XML docs).
- ≤30 lines per function, ≤3 nesting levels, ≤4 parameters, cyclomatic ≤10.
- Comments explain **why**, not **what**.
- Compose over inherit; prefer small focused classes.

## Tests

Mirror the production namespace in `unity/Assets/BoulderingGym/Tests/EditMode/` for pure logic and `Tests/PlayMode/` for scene-driven loops. EditMode must not require a scene — use the generic `StateMachine<>` + a fake context.
