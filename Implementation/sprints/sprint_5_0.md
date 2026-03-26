# Sprint 5.0 Checklist (5 days)

## Sprint Objective
Implement advanced climbing techniques (heel hook, toe hook, drop knee) and dyno ballistics.

## Definition of Done
1. Heel hook and toe hook functional with weight transfer.
2. Drop knee and twist lock extend reach as per body mechanics plan.
3. Dyno ballistic arc with gravity, launch vector, and catch window.
4. Pendular physics (barn-door swing) with recovery window.
5. FSM `BarnDoor` and `TechniqueActive` states integrated.

## Day 1
1. Implement heel hook (foot anchor above hip + weight transfer off hands).
2. Implement toe hook (anti-swing stabilization on overhangs).
3. Connect techniques to `weight_ratio_hands` reduction.

## Day 2
1. Implement drop knee (hip rotation + vertical reach bonus).
2. Implement twist lock (torso rotation + lateral reach bonus).
3. Connect to anatomical reach model (`arm_length` + technique bonuses).

## Day 3
1. Implement dyno ballistics: launch vector, `dyno_base_force`, gravity arc.
2. Implement catch window (`dyno_catch_window_s`).
3. Failed dyno → barn-door swing or fall.

## Day 4
1. Implement pendular physics (barn-door swing).
2. Recovery window (`swing_recovery_window_s`) to catch a hold during swing.
3. Add FSM states: `BarnDoor`, `TechniqueActive`.
4. Connect FSM transitions for technique activation/completion/failure.

## Day 5
1. Tune all technique values on test wall (overhang section).
2. Internal playtest: verify 4 techniques are usable and feel rewarding.
3. List max 5 frictions for Sprint 6.0.

## Validation Criteria
1. Heel hook visibly reduces arm fatigue on overhang holds.
2. Drop knee extends reach to previously unreachable holds.
3. Dyno follows a readable ballistic arc (not teleportation).
4. Barn-door swing feels physical and recovery window is tight but fair.
