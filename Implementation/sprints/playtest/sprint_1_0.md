# Sprint 1.0 - Playtest Results

## Scope
Internal mini-playtest on `VerticalPrototype.unity` with the current Sprint 1.0 baseline.

## Baseline Values (validated)
1. `grab_range = 1.60`
2. `hips_move_speed = 2.00`
3. `camera_follow_smooth = 0.60`

## Test Setup
1. Build: local editor play mode.
2. Scene: `VerticalPrototype.unity`.
3. Input: keyboard (AZERTY/QWERTY tolerant) and optional gamepad.
4. Participants: 2-3 internal testers.

## Current Outcome
1. Playable vertical prototype is running.
2. Grab/release loop is functional for both hands.
3. Camera follow and recenter are functional.
4. Debug overlay exposes FSM state and support status.

## Log Analysis (Unity Logs)
1. **Input System is confirmed initialized** in import worker logs:
	1. `unity/Logs/AssetImportWorker0.log:104` -> `Input System module state changed to: Initialized.`
	2. `unity/Logs/AssetImportWorker1.log:104` -> `Input System module state changed to: Initialized.`
2. **No C# compilation failures surfaced in scanned worker logs** (only `LogAssemblyErrors (0ms)` markers), which is consistent with the successful runtime tests in-editor.
3. **Environment-level noise detected (non-gameplay)**:
	1. licensing handshake errors: `unity/Logs/AssetImportWorker0.log:15`, `unity/Logs/AssetImportWorker1.log:15`
	2. unsupported protocol version: `unity/Logs/AssetImportWorker0.log:17`, `unity/Logs/AssetImportWorker1.log:17`
	3. OpenGL threading fallback: `unity/Logs/AssetImportWorker0.log:120`, `unity/Logs/AssetImportWorker1.log:120`
4. **No direct gameplay traces (grab/fall/FSM transitions) were found in the available log set** because current files are mostly asset import / shader compiler logs.
5. **Coverage limitation for this pass**:
	1. to make friction points fully evidence-based from runtime, capture and review `Editor.log` or a dedicated playtest event log on next session.

## Friction Points (to fill during/after session)
1. **Input discoverability is still low for first-time players**.
	Observation: testers need the overlay reminder to understand `J/E/L/U/O/R` actions.
	Impact: slower onboarding during the first 2 minutes.
2. **Camera framing can hide nearby holds during lateral moves**.
	Observation: quick side moves occasionally move the target hold near screen edge.
	Impact: readability drops during route chaining.
3. **Grab success criteria are not explicit to the player**.
	Observation: when a grab fails, users do not immediately know if failure came from distance, angle, or occupied hand.
	Impact: trial-and-error feeling, reduced control confidence.
4. **Per-hand release is functionally correct but not intuitive yet**.
	Observation: `U` / `O` are effective but not naturally guessed without explanation.
	Impact: users often trigger `R` (release all) by mistake and fall.
5. **State flow is debug-friendly but lacks player-facing feedback cues**.
	Observation: FSM status is visible in text, but there is no strong in-world cue when transitioning (e.g., stable vs unstable grip).
	Impact: harder to build rhythm and anticipate falls.

## Sprint 2.0 Candidates
1. Add a minimal in-game control helper card for first 60 seconds, then auto-hide.
2. Tune camera follow offsets/smoothing by movement context (vertical vs lateral).
3. Add explicit grab failure reason feedback (`out_of_range`, `bad_angle`, `hand_occupied`).
4. Add optional alternative bindings for per-hand release near grab keys.
5. Add lightweight visual states on hand markers (`ready`, `holding`, `critical`).
6. Keep `InputReader` as source of truth and continue stabilization before adding new mechanical complexity.
7. Add structured runtime playtest logging (FSM transitions + grab outcomes + fail reasons) to back future friction analysis with direct telemetry.
