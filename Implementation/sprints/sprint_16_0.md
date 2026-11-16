# Sprint 16.0 — Playable Block'Out Rennes

## Objective

Turn the prototype into a playable game: walk inside a 3D reconstruction of the
Block'Out Rennes gym, pick a boulder problem, climb it with the Point & Place
scheme (`plan/01_gameplay/game_loop.md`, core functional loop), top it or fall,
restart.

## Decisions

1. **Gym built at runtime** from one consolidated layout file
   (`Data/Gameplay/GymScenes/blockout_rennes_layout.json`): floor plan footprints
   from sprint 3.3 + per-sector wall profile and panel colors read from the photos.
   The scene only holds a bootstrap object (ADR-014).
2. **Problems generated procedurally** per wall face, deterministic seed, Block'Out
   color = grade circuit. Every generated problem is validated by a simulated
   climber using the same reach solver as the game, so each one is climbable (ADR-015).
   Real problems are not reproduced: they change every few weeks and photos do not
   allow it.
3. **New climbing core**: pure-domain pose solver (pelvis from limb contacts, reach
   limits from the character's measured bones) + Unity humanoid Animator IK for the
   pose. Replaces the sprint 3.x body solver / custom IK / hands controller (ADR-016).
4. Existing reusable parts kept: generic FSM, event bus, `TuningConfig`,
   `InputReader`/`IInputSource`, resource systems (endurance, grip, local fatigue),
   locomotion animator controller, Mixamo characters, UAT harness.
5. UI in IMGUI, English labels (C# files carry no French text).
6. Target: Linux standalone build (only build module installed).
7. **Level design passes** (user requirement): whitebox (untextured blocks: scale,
   circulation, climber metrics) → greybox (wall profiles, volumes, holds, full
   gameplay loop) → art pass (panel colors and patterns from photos, lighting,
   dressing). The runtime can render any pass, so captures compare passes.
8. **Photo-matched captures** (user requirement): the sprint 3.3 reconstruction was
   unreliable, so the 3D gym is validated by renders taken from the viewpoints of
   reference photos, composed side by side with the photos, at greybox (shapes)
   and art (look) passes.
9. Floor plan rescaled to one px/m (the sprint 3.3 extraction mixed 175 px/m in x
   and 152.6 px/m in z); floor area kept at ~1400 m². Contours repaired (snapped
   vertices, T-junctions, noise vertices < 15 cm).
10. Climber metrics: problems must be climbable by a 0.94x and a 1.04x scaled
    default climber (1.75 m); the in-game climber is clamped to that range.

## Definition of Done

1. `BlockOutRennes` scene is the only build scene; it starts on a title menu.
2. The 4 structures and 32 sectors stand at their floor plan positions with
   sector names displayed, wall angles and panel colors following the photos.
3. Every wall face carries generated problems (color circuit, start holds and top
   hold tagged); a validation test proves each one is climbable by the solver.
4. Climbing loop: walk, target a hold, place limbs, body follows, endurance/grip/
   per-arm fatigue drain, slip and fall when exhausted, match the top hold to win,
   drop down, restart. Attempts, time and best time per problem saved to JSON.
5. HUD (problem, timer, gauges, hints), problem browser + teleport, pause menu,
   controls screen.
6. EditMode tests green; UAT scenario `top_out_first_problem` tops a problem
   from the harness; screenshots reviewed.
6b. Photo-match sheet: renders from at least 6 reference photo viewpoints next
   to the photos, at greybox and art passes; gaps listed and fixed or accepted.
7. Linux player build launches without error in `Player.log`.
8. Superseded prototype code, scenes and tests removed; ADRs, variables and
   README updated.

## Work plan

| # | Stage | Status |
|---|---|---|
| 0 | Baseline: WIP preserved in commit, offline compile check | done |
| 1 | Domain: wall geometry, anatomy + pose solver, problem generator + validator | done (offline tests) |
| 2 | Layout data: floor plan repaired + per-sector appearance from photos | done (user review of edge kinds ongoing) |
| 3 | Whitebox: runtime gym builder (grey faces, caps, floor, building), spawn, character scale, camera | done |
| 4 | Greybox: holds + volumes meshes, climber controller + FSM + Animator IK, fall, session rules | done |
| 5 | Game flow: HUD, menus, problem browser, persistence | done |
| 6 | Editor: scene setup, problem bake, Linux build entry points; preview tool (OBJ + Blender) | done |
| 7 | Unity verification: tests, UAT scenarios, build | done (248 EditMode tests, scripted top-out, build) |
| 7b | Whitebox photo calibration: camera poses from photos (PnP), overlays, profile fixes | in progress |
| 8 | Art pass: panel patterns, lighting, ceiling/beams/ducts, mats; photo-match again | todo |
| 9 | Cleanup prototype + docs | todo |

## Top 3 risks

1. **Unity license unavailable** (Hub session expired). *Mitigation*: offline Roslyn
   compile of all assemblies + offline NUnit run of pure domain tests; Unity runs
   batched once the license is back.
2. **IK pose quality** with Mixamo rig. *Mitigation*: elbow/knee hints derived from
   wall normal; reach limited to 95 % of measured chain length.
3. **Performance on Intel UHD 620**. *Mitigation*: holds and panels combined per
   sector and material, simple shadows, frustum-friendly layout.

## KPIs

- Startup to playable < 10 s on the dev laptop.
- 100 % of generated problems pass the climbability validation.
- ≥ 50 FPS in the gym at 1080p on the dev laptop.

## Progress log

- 2026-11-16: WIP of sprints 4.0 + 15.0 committed on `feat/playable-gym` (7b21e59).
  Unity batchmode refuses to start: license needs a Unity Hub login.
- 2026-11-16: domain done and tested offline (Roslyn compile + NUnit on .NET 9 with
  UnityEngine.CoreModule; avoid native Unity calls such as Quaternion.Euler/LookRotation,
  Debug.Log, UnityEngine.Random in domain code). Real layout: 4 structures, 32 sectors,
  82 faces; 86 problems / 837 holds generated in ~5.6 s, all climbable by both climber
  sizes. Generation will run on a worker thread during the title screen.
- 2026-11-16: user feedback: Dalle climbable on the inside only (south side and east
  end marked blank); sprint 3.3 reconstruction "did not work well" → photo-matched
  captures required; follow whitebox/greybox/art passes.
- 2026-11-16: user feedback on renders: ceiling too low (9 m now), island = the "boat" (mast,
  lifebuoy, pirate flag), Breton flag on Pan Gullish, denser holds, wall grain (image_087);
  "island_reacteur photo match does not match at all" → whitebox must match angles first,
  and be playable. Plan outline is now the wall's outermost point (overhang feet set back).
- 2026-11-16: Unity license back. Scene BlockOutRennes created by `Project/Game/Set Up Scene and
  Assets`; 248 EditMode tests green; scripted climb of `starbloc_gauche-6` tops in 15.3 s
  (scenario generated from the validator route); Linux build 87 MB runs without error.
- Tools: `Implementation/sprints/sprint_16_0.d/build_layout.py` (run with `.venv/bin/python -I`)
  rebuilds `blockout_rennes_layout.json` and `layout_debug.png` (green exposed, red blank/back,
  grey internal).
