# Sprint 3.0 Addendum — Post-Sprint Review

## Outcome
Objective fully achieved. Three resource systems (Endurance, Grip, Local Fatigue) implemented as pure domain classes, connected to the physics model multipliers, with a chalk action, resource HUD, and 41 unit tests.

## Definition of Done — Results
| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | Endurance decreases/regens according to actions + COG offset + wall angle | ✅ | `EnduranceSystem.ApplyActionCost()` with cogOffsetPenalty × wallAngleEnduranceMult |
| 2 | Grip decreases according to hold texture + weight on hand + wall angle | ✅ | `GripSystem.UpdateDrain()` with gripTexture × weightOnHands × gripDrainMult |
| 3 | Local fatigue per arm calculated with physics-driven weight inputs | ✅ | `LocalFatigueSystem.Update()` per-arm with gripTypeMult × weight × fatigueMult |
| 4 | Chalk action functional with 20s cooldown | ✅ | `GripSystem.TryUseChalk()` + InputReader (C key) + cooldown tracking |
| 5 | Resource HUD displays endurance, grip, and arm fatigue gauges | ✅ | `ResourceHUD.cs` — 4 bars + chalk indicator + contextual prompts |

## Execution Summary

### Day 1: Domain Layer — Resource Systems
| File | LOC | Purpose |
|---|---|---|
| `GripType.cs` | 13 | Enum: Jug, Open, Pinch, Crimp, Gaston, Undercling, Sloper |
| `ResourceData.cs` | 31 | ClimbingAction enum, FatigueLevel enum, ResourceSnapshot struct |
| `EnduranceSystem.cs` | 48 | Pure domain: action costs with COG + wall angle modifiers, regen at rest |
| `GripSystem.cs` | 62 | Pure domain: continuous drain, burst loss, chalk restore with cooldown |
| `LocalFatigueSystem.cs` | 86 | Pure domain: per-arm fatigue, grip type multipliers, recovery for inactive arms |

### Day 2: Integration Layer
| File | LOC | Purpose |
|---|---|---|
| `ResourceSystemDriver.cs` | 263 | MonoBehaviour: polls physics snapshot, FSM state, hands → drives 3 systems |
| `ResourceHUD.cs` | 175 | IMGUI overlay: 4 resource bars, chalk indicator, contextual prompts (F3) |
| `TuningConfig.cs` | +27 | 19 new tuning fields: endurance regen/costs, grip adherence, fatigue thresholds, grip type multipliers |
| `EventType.cs` | +5 | New events: EnduranceLow, GripMicroSlip, FatigueWarning, FatigueFailure, ChalkUsed |
| `HoldComponent.cs` | +3 | Added GripType + gripTextureWall to HoldData |
| `InputReader.cs` | +18 | ConsumeChalkPressed() — C key + gamepad west button |
| `PrototypeBootstrapper.cs` | +56 | ResourceModule (wire driver), ShapeToGripType mapping, HoldData defaults, ResourceHUD wiring |

### Day 3: Tests
| File | Tests | Coverage |
|---|---|---|
| `EnduranceSystemTests.cs` | 12 | Initial state, all action costs, COG penalty, wall angle mult, combined modifiers, clamp, regen, reset, ratio |
| `GripSystemTests.cs` | 17 | Drain with weight/mult/texture, burst, chalk use/cooldown/clamp, micro-slip, reset |
| `LocalFatigueSystemTests.cs` | 12 + 9 TestCases | Per-arm gain/recovery, grip type mults, weight/wall angle effects, threshold levels, arm alternation |

## Architecture Decisions

### Resource Systems as Pure Domain (no MonoBehaviour)
Following the pattern established by `ClimbingPhysicsModel` in Sprint 2.0, all three resource systems are pure C# classes with no Unity dependencies beyond `Mathf`. This enables:
- EditMode unit testing without scene setup
- Deterministic behavior (no frame timing issues)
- Reusability across frontends

### Polling over Events for State Detection
`ResourceSystemDriver` polls `PlayerHandsController` each frame to detect grabs (comparing current vs. previous held holds) rather than subscribing to the event bus. This avoids event bus wiring complexity and is more robust during the prototype phase.

### Endurance: Action-Only Drain
Endurance drains only on discrete actions (grab, reposition, dyno), not passively. This makes endurance represent "move energy" while grip and fatigue handle continuous resource pressure. The COG offset penalty and wall angle multiplier affect action costs, not passive drain.

### Fatigue: Inactive Arm Recovery During Climbing
Arms that are not actively holding recover fatigue even during climbing. This creates the core climbing mechanic of "alternating arms" — if you always use the same arm, it exhausts without recovery. This is validated by the `AlternatingArms_LowerMaxFatigue` test.

## Metrics
| Metric | Value |
|---|---|
| New files | 10 source + 14 .meta |
| Modified files | 5 |
| New LOC (source) | ~678 |
| New LOC (tests) | ~607 |
| New tests | 41 (12 + 17 + 12) |
| Total tests (project) | 160 (119 + 41) |
| New TuningConfig fields | 19 |
| New EventTypes | 5 |
| Compilation errors | 0 |

## Tuning Values (from variables_gameplay.md)
All values initialized from the design document. Key defaults:
- Endurance: max=100, grab cost=2, regen=6/s
- Grip: max=100, drain=1.5/s base, burst=5, chalk restore=35, cooldown=20s
- Fatigue: base gain=7/s, recovery=10/s, thresholds at 60/80/95
- Grip type multipliers: Jug(0.6) → Undercling(1.5)

## Friction Points for Sprint 4.0
1. **Reposition/Dyno actions not yet connected** — FSM states don't exist yet; endurance costs for these are defined but not triggered.
2. **Micro-slip has no visual feedback** — event is published but no visual system consumes it.
3. **No grip reset on chalk** — chalk restores grip but doesn't reset micro-slip visual state.
4. **HoldData defaults** — struct fields default to 0; bootstrapper sets 1.0 but manually placed holds need OnValidate protection.
5. **No telemetry** — resource drain rates not logged for tuning analysis.
