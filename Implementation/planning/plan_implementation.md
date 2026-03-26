# Implementation Plan - Climbing Game

## 1) Engine Choice First

### Recommendation
I recommend **Unity** for this project.

### Why Unity is the Best Choice Here
1. Mature ecosystem for a 3D technical movement game.
2. Tools suited for rapid prototyping then polish:
1. `Cinemachine` for readable wall camera.
2. `Animation Rigging` for arm/leg IK.
3. `Input System` for keyboard/gamepad rebinding in UI.
4. Good documentation and strong community.
3. Efficient production pipeline to go from prototype to MVP in a few weeks.

### Alternatives
1. **Godot 4**
1. Advantages: open source, lightweight, fast iteration.
2. Limits: fewer tools and experience for complex 3D climbing gameplay with advanced IK.
2. **Unreal Engine**
1. Advantages: rendering, animation, AAA tools.
2. Limits: too heavy for a small MVP scope, complexity beyond current needs.

### Proposed Technical Decision
1. Engine: `Unity` (LTS version).
2. Language: `C#`.
3. MVP target: PC (Linux/Windows), 60 FPS.

## 2) Implementation Plan (MVP)

### Phase A - Setup (Week 1)
1. Create Unity LTS project.
2. Install packages:
1. Input System
2. Cinemachine
3. Animation Rigging
4. Configure vertical test scene (grey wall + holds).
5. Set up folder structure (ADR-006 — layered architecture):
   1. `Assets/_Project/Scripts/Core/` (Events, Domain, Utilities)
   2. `Assets/_Project/Scripts/Features/Climbing/` (Domain, Application, Presentation, Tests)
   3. `Assets/_Project/Scripts/Infrastructure/` (Persistence, Telemetry)
   4. `Assets/_Project/Scripts/UI/`
   5. `Assets/_Project/Data/Gameplay/Tuning/` (TuningConfig SO — ADR-003)
6. Configure two-segment test scene: flat + overhang ~15°, 10–12 holds (ADR-012).

Deliverable:
1. Basic playable scene + movement/camera.

### Phase B - Control and FSM (Weeks 2-3)
1. Implement player FSM via **State pattern GoF** — abstract `ClimbingState` + concrete subclasses, `ClimbingStateMachine`, `ClimbingContext` (ADR-004). Sprint 1.0: 4 states; full 9 states by Sprint 3.0.
2. Implement hands/feet control with per-hand hold detection (`SphereOverlap` from each hand anchor — ADR-011) and Two Bone IK for hand placement (ADR-009).
3. Implement critical guards:
   1. Dyno blocked under overload.
   2. Fall if invalid support.
4. Add state debug overlay (state class name + last trigger).

Deliverable:
1. Functional climbing loop on a test route.

### Phase C - Resource Systems (Weeks 4-5)
1. Implement `EnduranceSystem`.
2. Implement `GripSystem` with hold/wall grip texture.
3. Implement `LocalFatigueSystem` per arm.
4. Connect real-time feedback (UI + basic audio).

Deliverable:
1. Complete and readable gameplay tension.

### Phase D - Modes, Score, Progression (Weeks 6-7)
1. Implement `Arcade` and `Simulation` modes.
2. Implement detailed scoring and debrief.
3. Implement XP + levels 1 to 10.
4. Add simple anti-exploit.

Deliverable:
1. Complete run with result and progression.

### Phase E - UI/UX and Accessibility (Week 8)
1. Run UI (gauges, contextual alerts).
2. Controls menu with full keyboard rebinding.
3. Feedback accessibility options (intensity, colorblindness).
4. Settings and bindings persistence.

Deliverable:
1. Coherent end-to-end UX.

### Phase F - MVP Content and Playtests (Weeks 9-10)
1. Integrate 5 MVP routes.
2. Balance difficulties (`Easy`, `Medium`, `Hard`).
3. Run targeted playtests and fix P0 priorities.
4. Stabilize demo build.

Deliverable:
1. Presentable and testable MVP.

## 3) Proposed Code Architecture

1. `GameManager`: run cycle, mode, global state.
2. `PlayerController`: inputs to locomotion actions.
3. `ClimbingStateMachine`: state logic.
4. `StaminaFacade`:
1. `EnduranceSystem`
2. `GripSystem`
3. `LocalFatigueSystem`
5. `HoldComponent`: hold metadata (quality, texture, fragility).
6. `ScoreSystem`: points + debrief.
7. `ProgressionSystem`: XP/unlocks.
8. `UIFeedbackController`: contextual messages.
9. `SettingsService`: bindings + options.

## 4) Technical Risks to Address Early

1. Camera on wall hard to read.
2. IK unstable in some postures.
3. Control feel too "mechanical".
4. Performance with too many real-time effects.

Mitigation:
1. Prototype camera/IK before adding content.
2. Instrument run telemetry from Phase C.
3. Iterate with small tuning modifications.

## 5) Implementation Validation Criteria

1. Player understands controls in < 2 min.
2. A complete run lasts 5-15 min.
3. Keyboard keys are reconfigurable without restart.
4. Failure causes are readable in the debrief.
5. Build holds 60 FPS on MVP target scene.

## 6) Immediate Next Step

1. Initialize the Unity LTS project.
2. Create the vertical prototype scene.
3. Implement minimal FSM + left/right hand control.
