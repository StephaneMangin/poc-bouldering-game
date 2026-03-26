# Technical Risks

## T1: FSM Instability (Invalid Transitions)
1. Probability: medium.
2. Impact: high.
3. Mitigation: transition logs + state-by-state tests.

## T2: Incomplete Keyboard Rebinding
1. Probability: medium.
2. Impact: high.
3. Mitigation: dedicated binding screen, conflict detection, tested persistence.

## T3: Tight Gameplay/UI Coupling
1. Probability: medium.
2. Impact: medium.
3. Mitigation: event bus and derived states on the UI side.

## T4: Performance Degraded by Feedback
1. Probability: low to medium.
2. Impact: medium.
3. Mitigation: limit real-time effects, profile target build.

## T5: Scattered Tuning (Magic Numbers)
1. Probability: high.
2. Impact: medium.
3. Mitigation: centralize in `variables_gameplay.md` and version.

## T6: Physics Model Performance
1. Probability: medium.
2. Impact: high.
3. Mitigation: COG and weight distribution computed per-frame but simplified (no full rigid-body solver). Profile early. Cache results per tick.

## T7: Audio Asset Pipeline
1. Probability: medium.
2. Impact: medium.
3. Mitigation: use placeholder sounds early. Define asset list by Sprint 6.0. Source free/CC0 sounds for prototype.

## T8: IK + Body Mechanics Conflicts
1. Probability: high.
2. Impact: high.
3. Mitigation: body mechanics system outputs target poses, IK solver resolves. Clear layering: mechanics → IK → animation. Test heel hook/drop knee IK early.

## T9: PhysicMaterial Interaction Complexity
1. Probability: low.
2. Impact: medium.
3. Mitigation: limit to 6 materials. Use lookup table for friction pairs. No runtime material generation.

## T10: Pendular Swing Feel
1. Probability: medium.
2. Impact: medium.
3. Mitigation: simplified pendulum (single pivot point, damped oscillation). Tunable swing_recovery_window_s. Bypass in Arcade mode if too punishing.
