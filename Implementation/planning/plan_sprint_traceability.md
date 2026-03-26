# Plan -> Sprint Traceability

## Objective
Verify that every requirement in the plan is covered by at least one sprint with a testable deliverable.

## Matrix
| Requirement | Plan source | Sprint(s) | Expected deliverable | Validation KPI |
|---|---|---|---|---|
| Stable player FSM | `plan/04_technical/player_fsm.md` | S1.0, S5.0 | Debuggable states and transitions (13 states) | 0 blocking transition |
| COG + weight distribution | `plan/02_systems/physics_model.md` | S2.0 | COG computation + weight ratios + debug overlay | COG visibly affects climbing effort |
| PhysicMaterials | `plan/02_systems/physics_model.md` | S2.0 | 6 materials (wall, holds, crash pad, shoe) | Distinguishable friction behavior |
| Wall angle physics | `plan/02_systems/physics_model.md` | S2.0 | Slab/vertical/overhang/roof multipliers | Overhang feels harder than vertical |
| Endurance | `plan/02_systems/endurance.md` | S3.0 | Gauge + physics-driven costs/regen | Runs without resource inconsistency |
| Grip + hold texture | `plan/02_systems/grip.md` | S3.0 | Chalk + slipping + textures + physics drain | Understanding of chalk utility |
| Local fatigue per arm | `plan/02_systems/local_fatigue.md` | S3.0 | Load/critical/overload thresholds + physics | Explainable fatigue falls |
| Grip type differentiation | `plan/02_systems/body_mechanics.md` | S4.0 | 6 grip types with fatigue multipliers | Jug vs crimp feels different |
| Foot techniques (edging, smearing) | `plan/02_systems/body_mechanics.md` | S4.0 | Weight transfer on foot placement | Smearing works on slab only |
| Flag + torso rotation | `plan/02_systems/body_mechanics.md` | S4.0 | COG balancing + extended reach | Flag prevents barn-door |
| Heel hook + toe hook | `plan/02_systems/body_mechanics.md` | S5.0 | Weight transfer off hands | Arm fatigue reduces on overhang |
| Drop knee + twist lock | `plan/02_systems/body_mechanics.md` | S5.0 | Reach extension by rotation | Reach previously unreachable holds |
| Dyno ballistics | `plan/02_systems/physics_model.md` | S5.0 | Launch, gravity arc, catch window | Readable ballistic arc |
| Pendular physics (barn-door) | `plan/02_systems/physics_model.md` | S5.0 | Swing + recovery window | Recovery window is tight but fair |
| Audio system | `plan/02_systems/audio.md` | S6.0 | 4 audio layers + spatial + mixing | Audio enhances climbing readability |
| Arcade/Simulation modes | `plan/01_gameplay/game_modes.md` | S7.0 | Mode rules applied | Arcade completion > Simulation |
| Detailed score + debrief | `plan/02_systems/score_progression.md` | S7.0 | Debrief with score components | 70% understand score |
| XP progression L1-L10 | `plan/02_systems/score_progression.md` | S7.0 | Active and saved unlocks | L5 reachable in 60-120 min |
| Routes R1-R3 + art direction | `plan/03_levels/routes_mvp.md` | S8.0 | 3 routes + color coding + materials | Route concept identified by color |
| Routes R4-R5 + difficulty curve | `plan/03_levels/routes_mvp.md`, `plan/03_levels/difficulty.md` | S9.0 | 5 routes + 3 difficulties | Runs 5-15 min |
| Keyboard rebinding UI | `plan/01_gameplay/controls.md` | S9.0 | Rebind without restart + conflicts | 95% rebinding success |
| Visual effects (chalk, fingers, muscle) | `plan/05_production/backlog_mvp.md` | S10.0 | Particles + animations + feedback | Effects enhance readability |
| Mantling + knee bar + bicycle | `plan/02_systems/body_mechanics.md` | S11.0 | 3 advanced techniques + route variants | All techniques functional |
| External playtests | `plan/06_tests/plan_playtest.md` | S12.0 | Report + P0 fixes | Go KPIs near threshold |
| Polish + gym environment | `plan/05_production/backlog_mvp.md` | S13.0 | Decorative elements + camera + perf | 60 FPS sustained |
| MVP freeze | `plan/05_production/roadmap_10_weeks.md` | S14.0 | Stable demo build | No blocking crash |

## Maintenance Rule
1. Every new requirement adds a row to this matrix.
2. Any row without an assigned sprint is a scope risk.
