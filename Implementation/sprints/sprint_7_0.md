# Sprint 7.0 Checklist (5 days)

## Sprint Objective
Implement Arcade/Simulation modes, scoring system, XP progression, and unlocks.

## Definition of Done
1. Modes apply correct resource multipliers.
2. Fall handling per mode (checkpoint vs. end).
3. Detailed score calculated at end of run.
4. Readable debrief with recommendations.
5. XP progression L1-L10 functional with technique unlocks.

## Day 1
1. Implement `ModeService` (Arcade/Simulation).
2. Connect resource multipliers to endurance, grip, fatigue.
3. Implement fall logic per mode (Arcade checkpoint reset, Simulation run end).

## Day 2
1. Implement `ScoringSystem` (base, bonuses, penalties).
2. Add technique bonus (style score for clean technique usage).
3. Log score details per component.

## Day 3
1. Debrief screen with score breakdown.
2. Automatic improvement suggestion (1 strength, 1 improvement axis).
3. Implement `ProgressionSystem` (XP curve from plan).

## Day 4
1. Implement technique unlocks L2-L10 (flag, smearing, twist lock, drop knee, heel hook, toe hook, knee bar, bicycle).
2. Connect unlocks to FSM guard conditions.
3. Post-run progression UI + unlock notification.

## Day 5
1. Score calibration on test wall.
2. Progression playtest: verify L5 reachable in 60-120 min.
3. Quick QA on edge cases (abandon, multi-falls, no-fall bonus).

## Validation Criteria
1. Player understands why their score goes up/down.
2. Arcade is more permissive than Simulation.
3. Debrief displayable without error at every end of run.
4. Progression perceived as motivating.
5. Unlocks gate techniques correctly (cannot use heel hook before L5).
