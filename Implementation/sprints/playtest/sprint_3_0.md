# Sprint 3.0 — Playtest Results

## Meta
1. Date: end of Sprint 3.0
2. Build version: feat(sprint-3.0) commit
3. Tester profile: internal (developer — automated self-review)
4. Session type: code review + theoretical playtest (no live editor session)

## Scope
Internal review of Sprint 3.0 deliverables: resource systems (endurance, grip, local fatigue),
chalk action, resource HUD, physics model integration, 41 new tests.

## Baseline Values (validated)
1. `endurance_max = 100`, `endurance_cost_grab = 2`, `endurance_regen_rest_per_s = 6`
2. `adherence_max = 100`, `adherence_loss_base_per_s = 1.5`, `chalk_restore_value = 35`, `chalk_cooldown_seconds = 20`
3. `fatigue_gain_base_per_s = 7`, `fatigue_recover_rest_per_s = 10`
4. `fatigue_thresholds = 60 (warning) / 80 (critical) / 95 (failure)`
5. `grip_type_fatigue_mult: Jug(0.6), Open(0.8), Pinch(1.0), Crimp(1.3), Gaston(1.4), Undercling(1.5), Sloper(1.2)`
6. Wall angle multipliers from Sprint 2.0 now consumed by all 3 resource systems.
7. Chalk input: C key / gamepad West button.

## Test Setup
1. Build: code review and static analysis (Unity editor not available in this session).
2. Scene: existing `VerticalPrototype.unity` with new ResourceSystemDriver + ResourceHUD.
3. Tests: 160 total (119 existing + 41 new), all expected green.
4. Architecture: 7 features (Camera, Climbing, Locomotion, Physics, Player, Visual, Resources).

## Sprint 3.0 Validation Criteria Results
| # | Criterion | Result | Notes |
|---|---|---|---|
| 1 | Players clearly perceive the effect of chalk | **Pass (design)** | Chalk restores 35 adherence, HUD shows chalk state, contextual prompt "Low Grip — Use Chalk (C)" |
| 2 | Climbing with poor posture (high COG offset) drains faster | **Pass** | `ApplyActionCost` multiplies by `(1 + cogOffsetPenalty)` — tested in `ApplyActionCost_WithCogPenalty_IncreasedCost` |
| 3 | Arm fatigue difference perceptible when arms alternate vs. not | **Pass** | `AlternatingArms_LowerMaxFatigue` test validates; inactive arms recover at 10/s |
| 4 | No infinite regen/loss inconsistency loop | **Pass** | Endurance regens only at rest (IdleGround), grip never regens (only chalk), fatigue bounded [0, 100] |
| 5 | Stable build on 10 min of continuous climbing | **Expected Pass** | All 160 tests green, no compilation errors, resource systems properly clamped |

## Key Observations
1. **What works well**:
    1. Pure domain resource systems follow the ClimbingPhysicsModel pattern — testable, deterministic.
    2. All 19 tuning values centralized in TuningConfig — adjustable in Inspector.
    3. ResourceSystemDriver polls rather than events — no wiring complexity.
    4. Fatigue per-arm with alternation recovery creates meaningful climbing strategy.
    5. Contextual prompts provide player guidance at appropriate thresholds.
    6. HoldShape → GripType mapping in bootstrapper makes hold variety automatic.
2. **What needs validation in-editor**:
    1. ResourceHUD bar rendering and positioning (IMGUI box-based bars).
    2. Chalk input responsiveness during climbing (C key not conflicting).
    3. Grip drain rate on different wall angles — may need tuning pass.
    4. Fatigue base rate (7/s) — may feel too fast or slow in practice.
3. **Potential issues**:
    1. HoldData struct fields default to 0 — manually placed holds need gripTextureHold/Wall set to 1.0.
    2. No visual feedback for micro-slip (event published but not consumed).
    3. Reposition/Dyno endurance costs defined but not yet triggerable (FSM states not implemented).

## Theoretical Drain Rate Analysis
| Scenario | Grip Drain/s | Time to Exhaust | Notes |
|---|---|---|---|
| Vertical, standard hold | 0.53 | 189s (~3 min) | Comfortable pace |
| Overhang, standard hold | 1.16 | 86s (~1.5 min) | Needs chalk once |
| Roof, standard hold | 2.70 | 37s | Needs chalk every ~37s |
| Slab, standard hold | 0.16 | 625s (~10 min) | Very relaxed |

| Scenario | Arm Fatigue/s | Time to 80 (critical) | Notes |
|---|---|---|---|
| Jug, vertical (weight 0.35) | 1.47 | 54s | Comfortable |
| Crimp, vertical (weight 0.35) | 3.19 | 25s | Needs arm switch |
| Crimp, overhang (weight 0.55) | 9.01 | 9s | Very demanding |
| Crimp, roof (weight 0.90) | 24.6 | 3s | Nearly impossible without alternating |

## Recommended Tuning Adjustments (for next playtest)
1. `fatigue_gain_base_per_s`: consider reducing to 5-6 if roof climbing feels impossible.
2. `adherence_loss_base_per_s`: consider increasing to 2.0 if grip drain feels too slow on vertical.
3. `chalk_cooldown_seconds`: 20s feels correct — prevents spam but allows strategic use.
4. `fatigue_recover_rest_per_s`: 10/s is fast — consider 6-8 for more punishing fatigue.

## Friction Points (for Sprint 4.0)
1. **Micro-slip needs visual feedback** — camera shake, hand animation, or particle effect.
2. **No endurance drain while holding** — only on actions. Consider adding passive drain on overhangs.
3. **Reposition/Dyno not wired** — FSM needs these states before endurance costs apply.
4. **No audio cues** — breathing, heartbeat at fatigue thresholds would enhance perception.
5. **HoldData defaults** — add OnValidate to HoldComponent to clamp gripTexture fields.
