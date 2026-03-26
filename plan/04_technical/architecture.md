# Technical Architecture (prototype)

## Objective
Structure the code for fast gameplay iteration while keeping a clean foundation for production.

## Main Modules
1. `InputSystem`: captures inputs + keyboard rebinding.
2. `CharacterController`: movement, limb placement, collisions.
3. `ClimbingStateMachine`: climbing states and transitions.
4. `PhysicsModelSystem`: COG calculation, weight distribution, wall angle physics, pendular swing, body-wall contact.
5. `BodyMechanicsSystem`: grip type selection, foot technique application, torso rotation, anatomical reach, body tension.
6. `StaminaSystem`: endurance, grip, local fatigue — driven by physics model outputs.
7. `HoldSystem`: holds, textures, quality, grip type affordances, orientation, color coding.
8. `AudioSystem`: spatial climbing sounds, gym ambiance, breathing, feedback signals, music.
9. `ScoringSystem`: run score, bonuses, penalties, XP.
10. `UIFeedbackSystem`: HUD + prompts + contextual signals.
11. `VisualEffectsSystem`: chalk particles, finger animation, muscle feedback, breathing animation.
12. `RunManager`: run start/end, Arcade checkpoints, debrief.

## Data Flow (high-level)
1. `InputSystem` emits normalized actions.
2. `ClimbingStateMachine` validates action based on state.
3. `CharacterController` applies movement.
4. `PhysicsModelSystem` computes COG, weight distribution, wall angle effects.
5. `BodyMechanicsSystem` resolves grip type, foot technique, torso rotation.
6. `StaminaSystem` updates resources using physics outputs.
7. `AudioSystem` plays spatial sounds tied to actions and state.
8. `VisualEffectsSystem` renders particles, animations, muscle feedback.
9. `UIFeedbackSystem` displays feedback tied to resources/hold quality.
10. `ScoringSystem` logs events and calculates score.

## Minimum Contracts
1. Every gameplay system exposes `update(delta_time)`.
2. Critical events pass through a local event bus:
1. `HOLD_GRABBED`
2. `HOLD_SLIP`
3. `FATIGUE_CRITICAL`
4. `RUN_COMPLETED`
5. `RUN_FAILED`
6. `COG_UNSTABLE`
7. `BARN_DOOR_SWING`
8. `FOOT_CUT`
9. `TECHNIQUE_ACTIVATED`
10. `AUDIO_TRIGGER`
3. UIs read derived states, never direct gameplay logic.
4. `PhysicsModelSystem` outputs are consumed by `StaminaSystem` — never the reverse.
5. `AudioSystem` reacts to events and state — never drives gameplay.

## Persistence
1. `player_profile.json`: level, XP, unlocks, preferences.
2. `input_bindings.json`: custom keyboard keys.
3. `settings.json`: mode, difficulty, accessibility options.

## Code Rules
1. Tuning values centralized in `variables_gameplay`.
2. No magic numbers in runtime code.
3. Each system must be testable in isolation.

## Technical Milestones
1. M1: complete loop on a route with basic resources.
2. M2: physics model (COG + weight distribution) driving resource systems.
3. M3: body mechanics (grip types, foot techniques, torso rotation) integrated.
4. M4: audio system functional (climbing sounds + ambiance).
5. M5: scoring + debrief + XP progression.
6. M6: complete contextual UI/UX feedback + visual effects.
7. M7: exportable playtest telemetry.
