# Sprint 15.0 — AI Playtest Harness v0 (uat_records)

## Objective

Deliver a standalone scenario-driven playtest harness so the agent can verify
gameplay end-to-end (input → locomotion → state transitions) and produce
visual artefacts (PNG + MP4) without manual Editor interaction.

## Scope

- Game-side: introduce `Project.Core.Input.IInputSource`, `UnityInputSource`,
  and refactor `InputReader` to consume the abstraction via
  `OverrideSource(IInputSource)`.
- Package: scaffold `uat_records/` at the repo root as a self-contained
  Unity local package (`com.pocbouldering.uatrecords`) registered through
  `unity/Packages/manifest.json` via a `file:` reference.
- Runtime components: `ScriptedInputSource`, `ScenarioLoader`,
  `ScenarioModels`, `TelemetrySink`, `ScreenshotCapture`, `PlaytestBootstrap`.
- Editor entry: `PlaytestCLI.Run` callable via `-executeMethod` from batchmode.
- EditMode tests: `ScenarioLoaderTests` cover happy path + validation errors.
- One reference scenario: `scenarios/walk_forward.json` (scene
  `VerticalPrototype`, 6 s, hold `W`).
- Linux orchestrator `scripts/playtest.sh`: wraps `xvfb-run` + Unity batchmode
  + optional `ffmpeg` post-process.
- Governance: append ADR-013 to `Implementation/architecture/adr.md`.

## Out of scope

- Unity Recorder package integration (post-process via `ffmpeg` is enough).
- Windows / macOS orchestrators (Linux primary per AGENTS.md).
- Assertion DSL on telemetry (agent inspects `telemetry.jsonl` manually for v0).
- `IEventBus` discovery from a `GameContext` singleton — stubbed for v0;
  `events.jsonl` will be empty until that hook is wired.
- New gameplay features.

## Definition of Done

1. `uat_records/unity/package.json` is resolved by Unity and appears in the
   Package Manager.
2. `ScenarioLoaderTests` are green in the Unity Test Runner (EditMode).
3. `./uat_records/scripts/playtest.sh uat_records/scenarios/walk_forward.json`
   produces under `uat_records/artefacts/run_<timestamp>/`:
   - `telemetry.jsonl` with one line per frame (≥ 60 lines for a 6 s run).
   - `screenshots/frame_*.png` (at least 1 frame captured).
   - `report.md` with scenario name, duration, screenshot count.
   - `unity.log` containing `[UATRecords] Scenario 'walk_forward' finished`.
4. Removing `com.pocbouldering.uatrecords` from `unity/Packages/manifest.json`
   leaves the game project compiling.
5. `InputReader` public API is unchanged from callers' perspective — no
   existing controller is modified by this sprint.
6. ADR-013 appended and consistent with the implementation.

## Top 3 risks

1. **xvfb GPU compatibility**: software rendering may fail to capture frames
   on some hosts. *Mitigation*: harness still produces `telemetry.jsonl` even
   if screenshots are zero; document fallback in README.
2. **`JsonUtility` flat-schema limitation**: nested arrays of polymorphic
   payloads cannot be serialised. *Mitigation*: keep `ScenarioStep` flat
   (`key` xor `mouseButton`); hand-roll event JSON line.
3. **Game-side coupling drift**: `InputReader` API may evolve and break
   `ScriptedInputSource`. *Mitigation*: `IInputSource` lives in the game's
   `Project.Core.Input` namespace; the package depends on the game's
   runtime assembly so any rename surfaces as a compile error.

## KPIs

- Test runner: ≥ 5 EditMode tests passing.
- Reference scenario: completes in < 30 s wall-clock on a developer laptop
  with `xvfb-run`.
- Lines of game code touched: ≤ 150 (interface + adapter + InputReader refactor).
- Artefacts size for the 6 s reference scenario: < 5 MB without MP4.

## External dependencies

- Unity 2022.3 LTS (already pinned).
- `xvfb` package on the host (`apt install xvfb`).
- `ffmpeg` on the host (optional, for MP4 post-process).
- `UNITY_EDITOR` env var (or `unity-editor` on `PATH`) pointing at the Editor
  binary.

## Deliverable

Single PR adding `uat_records/` at the repo root, the three game-side input
abstraction files, the `manifest.json` entry, ADR-013, and this sprint card.
