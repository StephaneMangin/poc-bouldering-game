# Player FSM (Climbing)

## Objective
Define clear states to guarantee predictable and debuggable behavior.

## States
1. `IdleGround`: on the ground before the start.
2. `Reach`: aiming and extending a limb toward a hold.
3. `GripStable`: hold gripped with stability.
4. `Reposition`: hips/feet adjustment.
5. `DynamicMove`: push/dyno.
6. `Resting`: active rest on a stable position.
7. `SlipRecover`: correction after a micro-slip.
8. `BarnDoor`: pendular swing after losing a contact point — recovery window active.
9. `Mantling`: top-out push sequence on final edge.
10. `TechniqueActive`: executing a body technique (heel hook, drop knee, knee bar, etc.).
11. `Falling`: falling.
12. `Landing`: crash pad impact, landing quality assessment.
13. `RunEnd`: success or failure.

## Main Transitions
1. `IdleGround -> Reach`: valid grab action.
2. `Reach -> GripStable`: hold captured within valid window.
3. `GripStable -> Reposition`: movement/foot input.
4. `GripStable -> DynamicMove`: push + valid conditions.
5. `GripStable -> Resting`: stable posture detected.
6. `GripStable -> SlipRecover`: `HOLD_SLIP` event.
7. `GripStable -> BarnDoor`: `COG_UNSTABLE` + contact lost on one side.
8. `GripStable -> TechniqueActive`: technique input (heel hook, drop knee, etc.).
9. `GripStable -> Mantling`: both hands on top edge + push input.
10. `SlipRecover -> GripStable`: successful recovery.
11. `SlipRecover -> Falling`: failed recovery.
12. `BarnDoor -> GripStable`: caught a hold within `swing_recovery_window_s`.
13. `BarnDoor -> Falling`: recovery window expired.
14. `TechniqueActive -> GripStable`: technique completed or cancelled.
15. `TechniqueActive -> Falling`: technique failed (insufficient fatigue/grip).
16. `Mantling -> RunEnd`: successful top-out.
17. `Mantling -> Falling`: insufficient endurance/fatigue to complete.
18. `DynamicMove -> GripStable`: caught target hold within `dyno_catch_window_s`.
19. `DynamicMove -> BarnDoor`: missed catch, still has contact point.
20. `DynamicMove -> Falling`: missed catch, no contact.
21. `Falling -> Landing`: body reaches crash pad zone.
22. `Landing -> RunEnd`: Simulation mode or fatal fall.
23. `Landing -> IdleGround`: Arcade mode restart.
24. `Falling -> GripStable`: Arcade mode + valid checkpoint.

## Guard Conditions
1. Forbid `DynamicMove` if arm fatigue >= 95.
2. Forbid new grab if no active support limb.
3. Allow `Resting` only if valid foothold and stabilized load.
4. Forbid `TechniqueActive` if required technique is not unlocked (level gating).
5. Forbid `Mantling` if endurance < 20 or arm fatigue > 90.
6. Allow `BarnDoor` recovery only if `swing_recovery_window_s` has not elapsed.
7. Forbid `DynamicMove` if COG is unstable (cog_offset > threshold).

## Emitted Events
1. `ON_STATE_ENTER`
2. `ON_STATE_EXIT`
3. `ON_CRITICAL_RESOURCE`
4. `ON_FALL`
5. `ON_ROUTE_COMPLETE`
6. `ON_COG_UNSTABLE`
7. `ON_BARN_DOOR`
8. `ON_TECHNIQUE_ACTIVATED`
9. `ON_TECHNIQUE_COMPLETED`
10. `ON_LANDING`
11. `ON_MANTLE_START`

## Debug
1. FSM overlay in dev mode (current state + last event).
2. Log invalid transitions with reason.
3. Transition loop counter per run.
