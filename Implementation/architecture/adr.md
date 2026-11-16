# ADR - Architecture Decision Record

## Usage
An ADR captures an important decision, its context, alternatives, and impact.

## Template
### ADR-XXX - Title
1. Date:
2. Status: proposed | accepted | deprecated
3. Context:
4. Decision:
5. Alternatives considered:
6. Positive consequences:
7. Negative consequences:
8. Mitigation plan:

## ADR-001 - Game Engine
1. Date: 2026-03-26
2. Status: accepted
3. Context: need for a fast 3D prototype, readable camera, UI rebinding, and frequent tuning iteration.
4. Decision: use Unity LTS.
5. Alternatives considered:
1. Godot 4
2. Unreal Engine
6. Positive consequences:
1. Mature ecosystem (Input System, Cinemachine, Animation Rigging).
2. Fast for MVP.
7. Negative consequences:
1. Dependency on the Unity ecosystem.
2. Scene/prefab versioning management requires discipline.
8. Mitigation plan:
1. Structure prefabs and scenes from S1.0.
2. Avoid non-essential plugins.

## ADR-002 - Local Event Bus
1. Date: 2026-03-26
2. Status: accepted
3. Context: multiple systems (FSM, stamina, UI, scoring) must react to gameplay events without direct coupling. The architecture mandates a local event bus for `HOLD_GRABBED`, `HOLD_SLIP`, `FATIGUE_CRITICAL`, `RUN_COMPLETED`, `RUN_FAILED`.
4. Decision: implement a lightweight custom event bus (`Dictionary<EventType, List<Action<EventData>>>`) in `Core/Events/`. No external library.
5. Alternatives considered:
   1. C# `static events / delegates`: zero infrastructure but causes static coupling, hard to test in isolation.
   2. `ScriptableObject` event channels (Unity OCA pattern): strong decoupling, inspector-visible, but requires one asset per event — overhead for a prototype.
   3. `UnityEvents` on MonoBehaviour: verbose, inspector-heavy, not suitable for code-driven systems.
6. Positive consequences:
   1. Zero coupling between emitting and listening systems.
   2. Testable in EditMode without a scene.
   3. Easy to extend with new event types.
7. Negative consequences:
   1. Custom code to maintain.
   2. Runtime subscription errors not caught at compile time.
8. Mitigation plan:
   1. Use a typed `enum EventType` to prevent string-based errors.
   2. Log unhandled or unknown events in debug mode.

## ADR-003 - Tuning Values via ScriptableObject
1. Date: 2026-03-26
2. Status: accepted
3. Context: `variables_gameplay.md` centralizes all numeric constants. These must be editable at runtime for fast iteration without recompiling, and versioned for telemetry (field `tuning_version`).
4. Decision: expose all tuning values through a versioned `TuningConfig` ScriptableObject asset (`Assets/_Project/Data/Gameplay/Tuning/`). One global SO for the MVP; split by system (EnduranceConfig, GripConfig…) if the file exceeds ~80 fields.
5. Alternatives considered:
   1. Static C# class: zero infra, but no live tweaking — rebuild required for every balance change.
   2. External JSON loaded at runtime: enables hot-reload but heavier setup for a prototype; deferred to Sprint 7.0+ for telemetry export.
6. Positive consequences:
   1. Values editable in Inspector without recompiling.
   2. Asset swappable per scene (e.g., test scene uses extreme values).
   3. `tuning_version` field supports save/telemetry integrity.
7. Negative consequences:
   1. SO must be assigned in every scene that needs it.
   2. Asset file conflicts in version control if modified concurrently.
8. Mitigation plan:
   1. Inject via a `GameContext` singleton initialized in the Bootstrap scene.
   2. Treat the SO as read-only at runtime (no code writes to it).

## ADR-004 - FSM for Player Climbing States (State Pattern GoF)
1. Date: 2026-03-26
2. Status: accepted
3. Context: climbing behavior has 9 distinct states with non-trivial guard conditions (e.g., forbid `DynamicMove` if arm fatigue ≥ 95). Ad-hoc `if/else` chains would be untestable and fragile.
4. Decision: implement a code-driven FSM using the **State pattern (GoF)**: an abstract `ClimbingState` base class with `OnEnter()`, `OnExit()`, `HandleTrigger()`, and a concrete subclass per state (`IdleGroundState`, `ReachState`, `GripStableState`, `RepositionState`, `DynamicMoveState`, `RestingState`, `SlipRecoverState`, `FallingState`, `RunEndState`). The `ClimbingStateMachine` owns the current state reference and delegates all logic to it.
5. Alternatives considered:
   1. Table of transitions (`Dictionary<(State,Trigger), State>`) with `OnEnter`/`OnExit` delegates: compact, testable, but guard conditions become lambdas in the table — harder to read and debug as complexity grows.
   2. Unity Animator Controller: visual but tightly coupled to animation assets, hard to test without a rig.
   3. Behavior trees: overkill for 9 linear states at MVP scope.
6. Positive consequences:
   1. Each state encapsulates its own logic — easy to add/remove a state without touching others.
   2. Guard conditions live inside the state's `HandleTrigger()` — co-located with the behavior they protect.
   3. Invalid transition attempts are caught and logged with reason per state.
   4. FSM overlay in dev mode (current state class name + last trigger).
7. Negative consequences:
   1. More files than a transition table approach (one class per state).
   2. Cross-state data (e.g., shared resource references) must be passed via a `ClimbingContext` object to avoid coupling.
8. Mitigation plan:
   1. Define a `ClimbingContext` record passed to every state — holds references to `TuningConfig`, resource systems, and event bus.
   2. Keep FSM state count at 4 for Sprint 1.0 (`IdleGround`, `Reach`, `GripStable`, `Falling`); add remaining states incrementally.
   3. Log every invalid transition with state name + attempted trigger.

## ADR-005 - Unity New Input System with Full Rebinding
1. Date: 2026-03-26
2. Status: accepted
3. Context: the game targets keyboard+mouse and gamepad. All actions must be rebindable at runtime without restart, with real-time conflict detection. The old `Input` class does not support this.
4. Decision: use the Unity **New Input System** package with separate action maps (`Gameplay`, `UI`, `Debug`). Bindings persisted via `input_bindings.json`.
5. Alternatives considered:
   1. Legacy `Input` class: no rebinding support, single input path.
   2. Third-party input library (Rewired, InControl): mature but paid, adds a dependency not justified for an MVP.
6. Positive consequences:
   1. Gamepad and keyboard handled identically in game code.
   2. Runtime rebinding with conflict detection built into the API.
   3. Action maps enforce context-based input separation.
7. Negative consequences:
   1. More complex API than the legacy system.
   2. Requires discipline to avoid mixing old and new input APIs.
8. Mitigation plan:
   1. Ban `Input.*` legacy calls via `.asmdef` constraints.
   2. Implement a single `InputReader` MonoBehaviour in `Core/` that translates Input System callbacks into domain events.

## ADR-006 - Layered Folder Architecture (Core / Features / Infrastructure)
1. Date: 2026-03-26
2. Status: accepted
3. Context: a flat `Scripts/` folder leads to coupling between gameplay systems, UI, and infrastructure. The project needs to scale across 10 sprints without accumulating technical debt.
4. Decision: apply a layered architecture with strict unidirectional dependencies: `Core` ← `Features.*` ← `UI` / `Infrastructure`. Each feature follows `Domain / Application / Presentation / Tests`.
5. Alternatives considered:
   1. Flat `Scripts/` by file type (Controllers, Managers, Utils): simple but does not enforce separation.
   2. Full Clean Architecture with interfaces everywhere: too heavy for a 10-week prototype.
6. Positive consequences:
   1. `Domain` classes are pure C# — testable in EditMode with no scene.
   2. Clear boundary between gameplay logic and Unity-specific code.
   3. Features are independently loadable and testable.
7. Negative consequences:
   1. More folders and files to navigate.
   2. Requires discipline to not shortcut with MonoBehaviour references across layers.
8. Mitigation plan:
   1. Enforce via `.asmdef` assembly definitions (`Project.Core`, `Project.Features.Climbing`, etc.).
   2. Code review checklist item: no cross-layer MonoBehaviour references.

## ADR-007 - JSON Files for Persistence
1. Date: 2026-03-26
2. Status: accepted
3. Context: the game needs to persist player profile (XP, unlocks), input bindings, and settings across sessions. Persistence must be lightweight, human-readable for debugging, and cross-platform.
4. Decision: use JSON files written via `JsonUtility` (or `Newtonsoft.Json` if nested types require it). Three files: `player_profile.json`, `input_bindings.json`, `settings.json`. Stored in `Application.persistentDataPath`.
5. Alternatives considered:
   1. PlayerPrefs: not suitable for structured data, limited size.
   2. SQLite: overkill for flat key-value save data at MVP scope.
   3. Binary serialization: not human-readable, harder to debug during playtests.
6. Positive consequences:
   1. Readable and editable manually during playtesting.
   2. Easy to version (add `tuning_version` field).
   3. Zero external dependency with `JsonUtility`.
7. Negative consequences:
   1. No schema enforcement — malformed files silently fail.
   2. `JsonUtility` does not support dictionaries or polymorphism.
8. Mitigation plan:
   1. Validate on load; fall back to defaults on parse error with a warning log.
   2. Switch to `Newtonsoft.Json` if data structures require it (already available via Unity Package Manager).

## ADR-008 - Cinemachine for Camera
1. Date: 2026-03-26
2. Status: accepted
3. Context: a climbing game on a vertical wall requires a camera that follows the player smoothly, recenters on demand, and avoids obscuring nearby holds. Manual camera scripting is error-prone and time-intensive.
4. Decision: use **Cinemachine** (Unity package) with a `CinemachineVirtualCamera` configured for vertical follow and damping. R3 / `C` key triggers recentering.
5. Alternatives considered:
   1. Custom camera script: full control but requires significant tuning time per sprint.
   2. Third-party camera asset: adds cost and dependency not justified at this stage.
6. Positive consequences:
   1. Smooth follow and damping configurable in Inspector without code.
   2. Camera blends between states (ground → wall) are trivial to add later.
7. Negative consequences:
   1. Cinemachine API learning curve for custom behaviors.
   2. Brain/Virtual Camera setup must be consistent across scenes.
8. Mitigation plan:
   1. Set up a single reusable camera prefab in Sprint 1.0; do not duplicate per scene.
   2. Tune damping values during Day 5 mini-playtest.

## ADR-009 - Two Bone IK (Animation Rigging) from Sprint 1.0
1. Date: 2026-03-26
2. Status: deprecated — superseded by ADR-015 (humanoid Animator IK); code removed in Sprint 16.0
3. Context: the **Animation Rigging** package is installed. Hand placement on holds is a core visual read of the climbing mechanics. The Day 5 mini-playtest will be the first external validation; visual fidelity directly impacts tester comprehension and feedback quality.
4. Decision: implement **Two Bone IK** via `TwoBoneIKConstraint` (Animation Rigging) from Sprint 1.0 Day 3. The IK target is set to the hold's `grip_point` transform when a grab succeeds and released when the hand is freed.
5. Alternatives considered:
   1. Position snap (no IK): zero risk for the sprint timeline but produces visually confusing hand-through-hold artifacts during playtest.
   2. Partial IK (position snap + `Quaternion.Slerp` rotation): a ~2 h compromise that avoids full rig setup — rejected in favour of the complete solution given that Animation Rigging is already installed.
6. Positive consequences:
   1. Immediate visual feedback matches player intent — grab reads clearly on the wall.
   2. IK infrastructure is ready for foot placement (Sprint 3.0) and dynamic poses (Sprint 5.0).
   3. Higher fidelity playtest feedback from Day 5 session.
7. Negative consequences:
   1. Requires a rigged humanoid character from Sprint 1.0 — cannot use a placeholder capsule.
   2. Rig weight painting and constraint configuration add ~0.5 day of setup risk.
   3. IK solver must not run during `Reach` state before the hold is confirmed — requires explicit constraint enable/disable.
8. Mitigation plan:
   1. Prepare a minimal pre-rigged character prefab before Day 1 (or use a Unity starter asset rig).
   2. Gate IK activation on FSM state: enable constraint only on `GripStable` entry, disable on exit.
   3. If rig setup blocks Day 3, fall back to position snap for Sprint 1.0 and revisit on Day 4 — document decision in sprint retrospective.

## ADR-010 - HoldComponent as MonoBehaviour with Serialized Metadata
1. Date: 2026-03-26
2. Status: deprecated — superseded by ADR-014 (holds are generated layout data drawn in batched meshes); code removed in Sprint 16.0
3. Context: each Hold prefab needs gameplay metadata (`hold_quality`, `grip_texture_hold`). In later sprints, fragility, surface type, and visual type will be added. The data structure must be inspectable and extensible.
4. Decision: use a `HoldComponent` MonoBehaviour with a serialized `HoldData` struct inline. No ScriptableObject per hold at MVP stage.
5. Alternatives considered:
   1. `ScriptableObject HoldData` referenced by `HoldComponent`: clean data sharing across similar holds, but requires one SO asset per hold type from Sprint 1.0 — premature for a prototype.
   2. Plain public fields on MonoBehaviour: fast but unorganized; harder to migrate to SO later.
6. Positive consequences:
   1. All hold data visible in Inspector on the prefab.
   2. `HoldData` struct is easily extracted to a SO later without changing `HoldComponent`'s API.
   3. No asset management overhead for 6–8 prototype holds.
7. Negative consequences:
   1. Hold data is not shared between prefab variants (each prefab stores its own copy).
   2. Migration to SO requires updating all existing prefabs.
8. Mitigation plan:
   1. Group all fields inside a `[Serializable] HoldData` struct from the start to make future extraction trivial.
   2. Re-evaluate at Sprint 3.0 when hold variety increases.

## ADR-011 - Hold Accessibility Detection via Per-Hand Raycast
1. Date: 2026-03-27
2. Status: deprecated — superseded by ADR-015 (reach decided by the pose solver); code removed in Sprint 16.0
3. Context: the player can grab holds with the left or right hand independently. Reachability must reflect the actual arm origin — a grab initiated by the left hand has a different reach envelope than the right hand, especially when the player is already in an asymmetric posture.
4. Decision: detect accessible holds by casting a **sphere per hand** (radius = `grab_range` from `TuningConfig`) from the respective hand's world position (tracked on the character rig). A hold is a valid grab target if it is within range **and** the angle between the hand→hold vector and the wall normal is within `grab_angle_max`.
5. Alternatives considered:
   1. Single `OverlapSphere` from the character centre + angle filter: simpler but produces symmetric reach envelopes — incorrect when one arm is already extended far.
   2. `BoxCast` oriented toward the wall: avoids lateral false positives but complex to orient correctly on overhanging surfaces.
6. Positive consequences:
   1. Left/right hand reach asymmetry is physically plausible and readable.
   2. Both `grab_range` and `grab_angle_max` are exposed in `TuningConfig` for easy balancing.
   3. Detection is independent of IK — hand world positions are updated by the rig at all times.
7. Negative consequences:
   1. Requires tracking hand world positions, which depends on the character rig being set up (coupled with ADR-009).
   2. If IK is disabled for debugging, hand positions revert to bind pose — detection range becomes incorrect.
8. Mitigation plan:
   1. Expose `LeftHandAnchor` and `RightHandAnchor` as serialized transforms on the `PlayerHandsController` MonoBehaviour — fallback to a fixed offset from hips if rig is absent.
   2. Draw `Gizmos.DrawWireSphere` from each anchor in debug mode to visualise reach in the editor.

## ADR-012 - Prototype Scene Layout: Two Segments (Flat + Overhang)
1. Date: 2026-03-27
2. Status: deprecated — superseded by ADR-014 (BlockOutRennes scene built from layout data); scene removed in Sprint 16.0
3. Context: Sprint 1.0 builds the vertical prototype scene (`VerticalPrototype.unity`). The scene must validate FSM transitions, camera behaviour, grip detection, and control readability within a 5-day sprint.
4. Decision: build the scene with **two contiguous wall segments**: a flat vertical panel (~4 m × 6 m) and an overhanging panel (~4 m × 4 m, ~15° overhang), totalling **10–12 manually placed holds**. The player starts at ground level and can attempt both segments in a single run.
5. Alternatives considered:
   1. Single flat wall ~5 m × 8 m, 6–8 holds: quickest setup, sufficient for FSM validation but does not stress the camera or centre-of-gravity systems under overhang conditions.
   2. Single overhanging wall (~10°), 6–8 holds: more representative of final gameplay but misses the transition between wall angles that stresses camera interpolation.
6. Positive consequences:
   1. Camera behaviour tested across two distinct wall angles in Sprint 1.0 — avoids regressions when overhangs are added later.
   2. Centre-of-gravity and endurance costs differ between segments — early signal on tuning needs.
   3. Day 5 playtest covers more gameplay variety without additional sprint scope.
7. Negative consequences:
   1. Scene setup takes longer than a single flat panel — estimated +0.5 day on Day 1.
   2. Two segments require at least one camera blend zone, adding Cinemachine configuration complexity.
8. Mitigation plan:
   1. Use Unity ProBuilder or simple scaled cubes for geometry — no art assets required.
   2. If the second segment is not ready by Day 2, gate it with a visible barrier and unblock it without losing Day 1 deliverables.
   3. Place camera blend trigger volume at the junction between the two segments.

## ADR-013 — AI Playtest Harness (uat_records)

1. Status: Accepted.
2. Date: 2025-01.
3. Context:
   1. The project's primary tester is operational, not a developer — they cannot perform Inspector-based verification.
   2. The agent must therefore drive and observe gameplay end-to-end (locomotion, state transitions, fatigue) without manual GUI interaction.
   3. EditMode tests alone are insufficient: they validate logic but produce no visual evidence.
4. Decision:
   1. Build a standalone UPM package at `uat_records/unity/` (name `com.pocbouldering.uatrecords`), registered via `file:` reference in `unity/Packages/manifest.json`.
   2. Game-side coupling is minimal: introduce `Project.Core.Input.IInputSource` plus `InputReader.OverrideSource(IInputSource)`. The game does not depend on the package.
   3. Scenarios are JSON files parsed via `JsonUtility`; telemetry written as JSONL per frame; gameplay events captured via `IEventBus` subscription.
   4. Screenshots captured with `ScreenCapture.CaptureScreenshot` every N frames; MP4 produced in post by `ffmpeg` from the PNG sequence.
   5. Linux orchestrator (`uat_records/scripts/playtest.sh`) wraps Unity batchmode (`-executeMethod UATRecords.Editor.PlaytestCLI.Run`) with `xvfb-run` for headless rendering.
5. Alternatives considered:
   1. Unity Recorder package — rejected: extra dependency for a feature `ScreenCapture` covers.
   2. Pure EditMode tests — rejected: no visual artefact, cannot exercise MonoBehaviour update loop or rendering.
   3. Python-driven remote test framework — rejected: overkill for v0, defers value.
6. Positive consequences:
   1. The agent can submit a JSON scenario and inspect `telemetry.jsonl` + PNG frames without delegating to the human.
   2. Package is self-contained: deleting `uat_records/` removes one manifest line and the game still compiles.
   3. Zero new Unity dependency.
7. Negative consequences:
   1. Screenshot batchmode rendering requires xvfb on Linux — software-only, low FPS.
   2. `JsonUtility` cannot serialise heterogeneous payloads — events use a hand-rolled JSON line.
   3. Adds a CLI surface (`-scenario`, `-artefacts`) to maintain.
8. Mitigation plan:
   1. Document xvfb requirement in `uat_records/README.md`; fall back to JSONL-only mode if rendering fails.
   2. If event payloads grow complex, switch `TelemetrySink.WriteEvent` to `Newtonsoft.Json` (already transitive via test framework).
   3. CLI args wrapped by `playtest.sh` so callers only see one entry point.

## ADR-014 — Gym Built at Runtime from Layout Data, Problems Baked

1. Status: Accepted.
2. Date: 2026-11.
3. Context:
   1. The game must reproduce Block'Out Rennes (32 sectors) and stay editable when the reconstruction is corrected.
   2. Problem generation with climbability validation takes seconds: too slow for every start.
4. Decision:
   1. One layout file (`blockout_rennes_layout.json`) holds structures, sector outlines, edge kinds (exposed / back / internal), wall profiles, panel colors and patterns; it is produced by `Implementation/sprints/sprint_16_0.d/build_layout.py` from the floor plan and `wall_appearance.json`.
   2. The plan outline is the wall's outermost point: overhanging walls stand with their foot set back inside it.
   3. Meshes (faces with mitered corners, caps, holds, volumes, hall dressing) are generated by pure domain code at load; the scene only holds the bootstrap.
   4. Problems and volumes are generated once (`Project/Gym/Bake Problems` or the preview tool) into `blockout_rennes_problems.json`; the game loads them.
   5. Level design passes (whitebox, greybox, art) are a runtime switch; the same domain code feeds an OBJ export for offline photo-matched renders (`sprint_16_0.d/preview`).
5. Consequences:
   1. Fixing the gym is a data edit plus a rebake; no scene merge.
   2. The baked file must be regenerated after a layout or circuit tuning change (problem ids may change, invalidating saved records).

## ADR-015 — Climbing Core: Pose Solver and Humanoid Animator IK

1. Status: Accepted. Supersedes the sprint 3.x body solver, constraint IK pipeline and hands controller (ADR-009 Two Bone IK).
2. Date: 2026-11.
3. Context: the sprint 3.x pipeline placed the pelvis under the floor and refused valid foot placements; the prototype could not be climbed.
4. Decision:
   1. `PoseSolver` (pure domain): the pelvis is the point nearest a natural posture inside the intersection of one reach ball per supporting limb and wall / floor / posture half-spaces (Dykstra projection). A placement is legal when that intersection is not empty.
   2. The same solver validates generated problems (`ClimbabilityValidator`, depth-first search obeying the start rule) and drives the game, so every baked problem is climbable in play.
   3. `ClimberAvatar` poses the Mixamo humanoid with Animator IK (body position, hands, feet, elbow and knee hints) and re-aims hands and feet from their child bones after the IK pass.
   4. Climber states (Ground, OnWall, Falling, Landing) use the generic FSM (ADR-004).
5. Consequences: reach rules are explicit and testable offline; the visual pose depends on Unity humanoid IK quality.

## ADR-016 — App Composition Root and IMGUI Screens

1. Status: Accepted.
2. Date: 2026-11.
3. Decision:
   1. `Scripts/App/` is the top layer: `GameBootstrap` wires features, `GameFlow` owns the screens (title, climbing, problem list, pause, controls). Dependencies: App → UI → Features → Core.
   2. Screens and HUD use IMGUI with a small theme (`UiTheme`): no extra package, works in batch-mode playtests.
   3. Key labels come from the Input System keyboard layout (`KeyLabels`): the game reads physical keys, the UI prints the player's labels.
4. Consequences: IMGUI costs draw calls per frame and has limited styling; acceptable for this scope.

