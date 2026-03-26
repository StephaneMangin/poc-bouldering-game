# Standard Project Architecture (Game Industry) - Unity

## Objective
Define a production architecture that remains:
1. Readable for the team.
2. Scalable to add content.
3. Testable to limit regressions.
4. Compatible with CI/CD and frequent deliveries.

## Guiding Principles
1. Clear separation `Core` / `Gameplay` / `Presentation` / `Infrastructure`.
2. Unidirectional dependencies (no cycles).
3. Feature flags and data via `ScriptableObject` to iterate without touching code.
4. Minimal scene files, logic in prefabs + scripts.
5. Automated tests at least on critical systems.

## Recommended Folder Structure
```text
ProjectRoot/
  Assets/
    _Project/
      Art/
        Characters/
        Environment/
        UI/
        VFX/
      Audio/
        Music/
        SFX/
      Data/
        Gameplay/
          Tuning/
          Routes/
          Progression/
        Localization/
      Prefabs/
        Gameplay/
        UI/
        Environment/
      Scenes/
        Bootstrap/
        Frontend/
        Gameplay/
        Test/
      Scripts/
        Core/
          Domain/
          Application/
          Events/
          Utilities/
        Features/
          Climbing/
            Domain/
            Application/
            Presentation/
            Tests/
          Stamina/
            Domain/
            Application/
            Presentation/
            Tests/
          Scoring/
          Progression/
          Input/
          Accessibility/
        Infrastructure/
          Persistence/
          Telemetry/
          Platform/
        UI/
          HUD/
          Menus/
          Debrief/
      Settings/
        Addressables/
        Quality/
        Input/
      Tools/
        Editor/
        Debug/
    Plugins/
    TextMesh Pro/
  Packages/
  ProjectSettings/
  UserSettings/
  Tests/
    EditMode/
    PlayMode/
  Builds/
    CI/
    Local/
  Docs/
    GDD/
    Tech/
    Changelog/
```

## Recommended Assemblies (.asmdef)
1. `Project.Core`
2. `Project.Features.Climbing`
3. `Project.Features.Stamina`
4. `Project.Features.Scoring`
5. `Project.Features.Progression`
6. `Project.UI`
7. `Project.Infrastructure`
8. `Project.Tests.EditMode`
9. `Project.Tests.PlayMode`

## Dependency Rules
1. `Core` depends on no concrete gameplay module.
2. `Features.*` may depend on `Core`.
3. `UI` depends on `Features` via interfaces/view-models.
4. `Infrastructure` implements the ports/interfaces of `Core`.
5. `Tests` depend only on the tested modules.

## Feature Architecture Pattern
Each feature follows this schema:
1. `Domain`: pure business rules, no MonoBehaviour.
2. `Application`: orchestration of use cases.
3. `Presentation`: MonoBehaviours, UI binding, VFX/SFX hooks.
4. `Tests`: unit + integration of the feature.

## Code Conventions
1. One class = one responsibility.
2. No business logic in scenes.
3. Explicit names (`EnduranceSystem`, `ClimbingStateMachine`).
4. Centralized tuning values (source: `plan/04_technical/variables_gameplay.md`).
5. Controlled async logic (no spaghetti coroutines).

## Data and Tuning
1. Runtime tuning via versioned `ScriptableObject`.
2. Route data separated from code (`Assets/_Project/Data/Gameplay/Routes`).
3. Tuning version stored in saves (`tuning_version`).
4. All tuning changes tracked in `Docs/Changelog/tuning.md`.

## Scenes and Bootstrapping
1. Minimal `Bootstrap` scene to initialize global services.
2. Additive loading of gameplay scenes.
3. No fragile cross-scene references.
4. Global managers limited and well-scoped.

## Input and Rebinding
1. Separate action maps: `Gameplay`, `UI`, `Debug`.
2. Rebinding via UI, persistent, without restart.
3. Real-time conflict detection.
4. Fallback to default preset if binding is corrupted.

## Telemetry and Logs
1. Standard business events: `RUN_STARTED`, `RUN_FAILED`, `RUN_COMPLETED`, `HOLD_SLIP`, `FATIGUE_CRITICAL`.
2. Log levels: `Info`, `Warning`, `Error`.
3. No log spam in frame loop.
4. JSON/CSV export in dev mode for playtest analysis.

## Test Strategy
1. Unit tests on `Domain` and `Application`.
2. PlayMode tests on critical loops (`grab -> stabilize -> fall`).
3. Non-regression suite run at the end of each sprint.
4. Smoke test build before release merge.

## Minimum CI/CD Pipeline
1. C# lint/format (analyzers) on PR.
2. Headless EditMode + PlayMode tests.
3. Automatic build per target platform.
4. Artifacts stored with version number.

## Asset Management
1. Standard naming:
1. Prefabs: `PF_`
2. Materials: `MAT_`
3. Textures: `T_`
4. Animations: `AN_`
2. Stable folders, no mass moves without reason.
3. Addressables for dynamically loaded content.
4. Memory/performance budget defined per sprint.

## Technical Definition of Done (global)
1. Code compiles without critical warnings.
2. Critical tests pass.
3. Performance KPIs respected (60 FPS gameplay target).
4. No open blocking bug on the delivered feature.
5. Documentation updated (plan + implementation + changelog).

## Direct Application to This Project
1. Use `Implementation/planning/plan_sprint_traceability.md` to link requirements and deliverables.
2. Keep `plan/04_technical/variables_gameplay.md` as the numeric source of truth.
3. Prioritize `FSM + Resources + Rebinding` stability before adding visual content.
