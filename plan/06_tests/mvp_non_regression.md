# MVP Non-Regression

## Objective
Quickly verify that critical flows remain functional after each change.

## Minimal Suite (to run at the end of each sprint)
1. Launch a complete Arcade R1 run without crash.
2. Launch a Simulation run and verify end on fall.
3. Verify left/right hand grab on 3 consecutive holds.
4. Verify endurance/grip/fatigue gauges evolve correctly.
5. Verify chalk action + cooldown.
6. Verify dyno blocked under fatigue overload.
7. Verify end-of-run score displays all components.
8. Verify debrief displays 1 strength + 1 improvement point.
9. Verify XP progression persists after game relaunch.
10. Verify keyboard rebinding of 3 actions without restart.
11. Verify key conflict detection.
12. Verify accessibility feedback options (intensity) applied.
13. Verify route R5 loads without error.
14. Verify `Simulation + Hard` combination does not break the run.
15. Verify build holds 30 min without blocking crash.

## Result Format
For each test: `PASS` | `FAIL` | `BLOCKED` + short note.

## Release Rule
1. No MVP release if a critical test is `FAIL`.
2. A `BLOCKED` test must have a ticket and an owner.
