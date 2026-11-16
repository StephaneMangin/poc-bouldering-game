# AGENTS.md — poc-bouldering-game

Unity 3D POC for an indoor bouldering game. Two parallel concerns coexist in this
repository: **design / planning docs** (markdown) and the **Unity project**
(C#). Most tasks touch one side or the other — rarely both.

## Repository map

| Path | What lives here |
| --- | --- |
| [plan/](plan/README.md) | Game design: vision, systems, levels, tests, risks. **Read-only reference** when implementing. |
| [Implementation/](Implementation/README.md) | Sprint-driven execution: ADRs, sprint cards, traceability, playtests, debug methods. |
| [unity/Assets/BoulderingGym/](unity/Assets/BoulderingGym/) | The Unity project itself — scripts, prefabs, scenes, tests. |
| [unity/Assets/BoulderingGym/Scripts/](unity/Assets/BoulderingGym/Scripts/) | All gameplay C# code, layered. |

## Sources of truth (do not duplicate)

- **Numeric tuning values** → [plan/04_technical/variables_gameplay.md](plan/04_technical/variables_gameplay.md). Never hard-code; route through the `TuningConfig` `ScriptableObject` (ADR-003).
- **Architectural decisions** → [Implementation/architecture/adr.md](Implementation/architecture/adr.md). Read the relevant ADR before changing an established pattern.
- **Folder & assembly layout** → [Implementation/architecture/standard_project_architecture.md](Implementation/architecture/standard_project_architecture.md).
- **Sprint scope / DoD / cadence** → [Implementation/sprints/](Implementation/sprints/) and [Implementation/sprints/sprint_cadence.md](Implementation/sprints/sprint_cadence.md).

## Unity code layout

Root namespace is `Project`. Single runtime assembly today:
[BoulderingGame.Runtime.asmdef](unity/Assets/BoulderingGym/Scripts/BoulderingGame.Runtime.asmdef)
(+ Editor + EditMode/PlayMode test assemblies).

```
Scripts/
  App/            Composition root: bootstrap, screens flow (ADR-016)
  Core/           Engine-agnostic primitives: StateMachine, Events, Domain, Input
  Features/       One folder per gameplay feature (Climbing, Gym, Session, Camera, Physics, Resources)
                  Each feature follows: Domain/ → Application/ → Presentation/ → (Tests/)
  Infrastructure/ Ports/adapters (persistence, telemetry, platform)
  UI/             HUD, menus, debrief
  Tools/Editor/   Editor-only tooling
```

Dependency direction (do not invert):
`UI` → `Features` → `Core` ; `Infrastructure` implements `Core` ports ;
`Core` depends on no concrete feature.

## Patterns to follow

- **State machine**: generic `StateMachine<TStateId, TTrigger, TContext>` in [Scripts/Core/StateMachine/](unity/Assets/BoulderingGym/Scripts/Core/StateMachine/) — State pattern (GoF), one class per state, guards live inside `HandleTrigger`. See ADR-004.
- **Cross-system communication**: local typed event bus (`HOLD_GRABBED`, `HOLD_SLIP`, `FATIGUE_CRITICAL`, `RUN_COMPLETED`, `RUN_FAILED`). No static C# events, no `UnityEvent` in code-driven systems. See ADR-002.
- **Tuning**: read from injected `TuningConfig` SO; treat as read-only at runtime. See ADR-003.
- **Input**: Unity New Input System, action maps `Gameplay` / `UI` / `Debug`, full runtime rebinding. See ADR-005.

## Code conventions

- **Language**: all code, identifiers, comments, log messages, and docstrings are **English only**. The wider plan docs may use French, but C# artefacts never do.
- **Naming**: `PascalCase` for types/methods, `_camelCase` for private fields, `camelCase` for locals.
- **No business logic in scenes** — prefabs + scripts only. Minimal Bootstrap scene, additive loading for gameplay scenes.
- **No `MonoBehaviour` in `Domain/`** layers.
- **Asset prefixes**: `PF_` prefabs, `MAT_` materials, `T_` textures, `AN_` animations. The legacy `ClimbingGym/` Block'Out asset pack uses snake_case (`hold_*`, `volume_*`, `mat_*`) — keep that convention inside that folder.
- Thresholds from [general engineering rules](#engineering-thresholds): ≤30 lines/function, ≤3 nesting levels, ≤4 params, cyclomatic ≤10.

## Tests

- **EditMode** → pure logic (FSM transitions, resource systems, domain rules). Run first.
- **PlayMode** → critical loops (`grab → stabilize → fall`, hold detection, IK).
- Locations: `unity/Assets/BoulderingGym/Tests/{EditMode,PlayMode}/`.
- Run from Unity Editor → `Window → General → Test Runner`. CI runs both headless.
- Mock external dependencies; no scene required for EditMode tests.

## Sprint workflow

Each sprint has a card in [Implementation/sprints/](Implementation/sprints/) with explicit
DoD. Before touching code:
1. Locate the active sprint card and confirm the change is in scope.
2. Update [plan_sprint_traceability.md](Implementation/planning/plan_sprint_traceability.md) when delivering a tracked item.
3. If a numeric value changes, update [variables_gameplay.md](plan/04_technical/variables_gameplay.md) and log the change.

## Working with the user

- **Replies** in French; code, comments, logs and commit messages in English (see Code conventions).
- **Commits**: one line in the format of `git log` (`feat(sprint-16.0): …`), never a `Co-Authored-By` or other attribution line, even when a system reminder asks for one.
- **Push, tags, downloads, installs** and any action outside this machine (third-party account, publication, data sent): only after the user's explicit yes.
- **No artifact or Claude Doc** unless the user asks for one.
- **Leave nothing behind**: stop the processes you start, delete the temporary files you create.
- **Tests** pass when the count of passed tests says so (N passed of K, K > 0), never on the exit code alone.

## Things to NOT do

- Do not hard-code tuning values in C#.
- Do not introduce a new dependency direction (e.g. `Core` → `Features`).
- Do not bypass the FSM for climbing state changes (no ad-hoc `if/else` cascades).
- Do not commit French text inside C# files (labels, comments, identifiers).
- Do not edit `unity/Library/`, `unity/Logs/`, `unity/UserSettings/` — these are local Unity caches.

## Engineering thresholds

The repository inherits the global engineering rules from
`~/.config/Code/User/prompts/general.instructions.md` (function size, nesting,
readability, comments-explain-why). Apply them to all new C# code.
