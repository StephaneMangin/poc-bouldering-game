# UAT Records — AI Playtest Harness

Standalone package that lets an agent (or any operator) drive the Unity game
from a JSON scenario, capture telemetry + screenshots, and produce a
post-process MP4 — without manually clicking through the Editor.

## Why

The game's main tester is non-developer. Without an automated harness, the
agent cannot verify gameplay end-to-end (state transitions, locomotion,
fatigue evolution) and must delegate Inspector checks to the human. This
package closes that gap.

## Layout

```
uat_records/
├── unity/                 # Local UPM package (registered in unity/Packages/manifest.json)
│   ├── package.json
│   ├── Runtime/           # ScriptedInputSource, ScenarioLoader, TelemetrySink, ScreenshotCapture, PlaytestBootstrap
│   ├── Editor/            # PlaytestCLI (batchmode entry)
│   └── Tests/             # EditMode tests
├── scenarios/             # JSON scenario files
├── scripts/playtest.sh    # Linux orchestrator (xvfb-run + Unity batchmode + ffmpeg)
└── artefacts/             # Output (gitignored): telemetry.jsonl, events.jsonl, screenshots/, report.md, recording.mp4
```

## Coupling with the game

The package depends on `BoulderingGame.Runtime` for two extension points only:

1. `Project.Core.Input.IInputSource` + `InputReader.OverrideSource(IInputSource)` —
   lets the harness substitute a scripted input source at runtime.
2. `Project.Core.Events.IEventBus` — optional subscription for `events.jsonl`.

The game does not depend on the package. Removing `uat_records/` and the
matching manifest entry compiles cleanly.

## Requirements

- Unity 2022.3 LTS
- Linux + `xvfb-run` for headless rendering (`sudo apt install xvfb`)
- `ffmpeg` (optional, for PNG → MP4) (`sudo apt install ffmpeg`)
- `UNITY_EDITOR` env var pointing to the Editor binary, or `unity-editor` on `PATH`

## Run

```bash
export UNITY_EDITOR=/opt/unity/editors/2022.3.x/Editor/Unity
./uat_records/scripts/playtest.sh uat_records/scenarios/walk_forward.json
```

Artefacts land in `uat_records/artefacts/run_<timestamp>/`:

- `telemetry.jsonl` — one line per frame (position, FSM state, held keys)
- `events.jsonl` — one line per gameplay event (if `IEventBus` is wired in)
- `screenshots/frame_NNNNN.png` — periodic captures
- `report.md` — short summary
- `recording.mp4` — built from PNGs if `ffmpeg` is available

## Scenario format

```json
{
  "name": "walk_forward",
  "scene": "VerticalPrototype",
  "durationSeconds": 6.0,
  "screenshotEveryNFrames": 30,
  "steps": [
    { "atSeconds": 0.5, "durationSeconds": 4.0, "key": "W", "mouseButton": -1 }
  ]
}
```

A step represents one held key OR one mouse-button press over a window
`[atSeconds, atSeconds + durationSeconds]`. `key` uses `UnityEngine.KeyCode`
names; set `mouseButton = -1` when using `key` and `key = ""` when using
`mouseButton`.

## Limitations (v0)

- No GPU (xvfb renders software-only). Frame rate ≈ 10–20 FPS in batchmode.
- `IEventBus` discovery is stubbed — `events.jsonl` is empty until a `GameContext`
  exposes the scene bus.
- No assertion DSL yet: the agent inspects `telemetry.jsonl` / PNG / MP4 to verify.
- Screenshots in batchmode require a render-capable context (xvfb is enough on Linux).

## Tests

EditMode tests for `ScenarioLoader` run via Unity Test Runner under the
`UATRecords.Tests.EditMode` assembly.
