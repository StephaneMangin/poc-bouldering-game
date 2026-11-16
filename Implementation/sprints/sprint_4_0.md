# Sprint 4.0 Checklist (5 days)

## Sprint Objective
Implement body mechanics: grip type differentiation, basic foot techniques, and flag/torso rotation.

## Definition of Done
1. Hold affordances define `allowedGripTypes[]` per hold.
2. Grip type selection affects fatigue and grip drain rates.
3. Edging and smearing foot techniques functional.
4. Flag (balance leg extension) functional.
5. Basic torso rotation (±16° → ±45°) controlled by input.

## Day 1
1. Implement `BodyMechanicsSystem`.
2. Extend `HoldComponent` with `allowedGripTypes[]` property.
3. Implement grip type auto-selection (best compatible = lowest fatigue mult).

## Day 2
1. Connect grip type fatigue multipliers to `LocalFatigueSystem`.
2. Connect grip type drain multipliers to `GripSystem`.
3. Update 4 hold prefabs with appropriate `allowedGripTypes`.

## Day 3
1. Implement edging foot technique (standard precise placement).
2. Implement smearing (wall surface support, no hold needed, slab only).
3. Connect foot techniques to weight distribution model.

## Day 4
1. Implement flag (leg extension for COG balance).
2. Expand torso rotation range with player input.
3. Connect rotation to reach model (twist lock bonus for lateral reach).

## Day 5
1. Tune grip type multipliers on test wall.
2. Internal playtest: verify technique choice impacts performance.
3. List max 5 frictions for Sprint 5.0.

## Validation Criteria
1. Players can feel the difference between jug and crimp fatigue.
2. Smearing only works on slab angles.
3. Flag visibly prevents barn-door in lateral moves.
4. Torso rotation extends reach toward out-of-range holds.

## Implementation Status (code-only pass)

Pure-Domain code and EditMode tests delivered headless (no Unity Editor required).
Scene/prefab wiring is deferred to the Editor session below.

| DoD | Status | Artefact |
| --- | --- | --- |
| 1. `allowedGripTypes[]` on holds | ✅ Code | `Features/Climbing/Presentation/HoldComponent.cs` (`HoldData.allowedGripTypes`) |
| 2. Grip type → fatigue + drain | ✅ Code | `LocalFatigueSystem.GetGripTypeMult`, `GripSystem.GetGripTypeDrainMult`, `ResourceSystemDriver.GetAverageGripTypeDrainMult` |
| 3. Edging + smearing | ✅ Code | `Features/Climbing/Domain/FootTechnique.cs`, `FootMechanicsSystem.cs` |
| 4. Flag | ✅ Code | `Features/Climbing/Domain/FlagSystem.cs` |
| 5. Torso/hip rotation (±60° / ±45°, 120°/s) | ✅ Code | `Features/Climbing/Domain/BodyOrientationSystem.cs`, `InputReader` (arrows + PgUp/PgDn + G) |

EditMode tests added (Tests/EditMode/Features/Climbing/):
`BodyMechanicsSystemTests`, `FootMechanicsSystemTests`,
`BodyOrientationSystemTests`, `FlagSystemTests`.

### Editor session checklist (manual, deferred from Day 2 + Day 5)

1. Open Unity Editor; let it import the new scripts.
2. **Day 2 prefab population** — for each of the 4 MVP hold prefabs
   (`PF_Hold_Jug`, `PF_Hold_Crimp`, `PF_Hold_Pinch`, `PF_Hold_Sloper`),
   fill `HoldComponent.data.allowedGripTypes` (e.g. Jug → {Jug, Open};
   Crimp → {Crimp, Open}; Pinch → {Pinch}; Sloper → {Sloper, Open}).
3. **Day 5 driver wiring** (one-shot, scene-side):
   - Add a `BodyOrientationSystem` instance under `PlayerStateMachineDriver`
     and feed it `InputReader.ReadHipRotationAxis()` /
     `ReadTorsoTwistAxis()` each frame.
   - Add `FootMechanicsSystem` consumption in `ResourceSystemDriver` (or
     a new tiny driver) to scale weight-on-feet via the
     `GetWeightShareToFoot` output once the foot weight pipeline is
     wired into `ClimbingPhysicsModel`.
   - Hook `FlagSystem.GetLateralOffsetMultiplier` to the COG lateral
     offset penalty in `ClimbingPhysicsModel.ComputeCog` (apply only on
     the X component of `cogOffset`).
4. **Tuning pass** — open `TuningConfig.asset` and adjust the new sections
   "Foot Techniques", "Body Orientation", "Flag" against the values listed
   in `plan/04_technical/variables_gameplay.md`.
5. **Internal playtest** — run a session on the slab + vertical test wall;
   record up to 5 frictions in `Implementation/sprints/sprint_5_0.md`
   "Inputs from S4.0" section.

### Risks carried to Sprint 5.0

- Foot weight pipeline not yet plugged into `ClimbingPhysicsModel.ComputeWeight`
  (currently only hand weights are distributed). `FootMechanicsSystem` is
  ready to feed it; wiring requires adding a `footWeightShareModifier`
  parameter to the weight signature.
- No Animator parameters yet for hip/torso rotation; visual feedback for
  Day 4 will require an Animator IK setup in Sprint 5.0.

