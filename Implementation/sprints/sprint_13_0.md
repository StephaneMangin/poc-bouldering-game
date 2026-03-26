# Sprint 13.0 Checklist (5 days)

## Sprint Objective
Final balancing, gym environment completion, camera polish, and performance optimization.

## Definition of Done
1. Resources, scoring, and difficulty balanced across all routes and modes.
2. Gym environment complete (decorative elements, background).
3. Camera polish (smooth transitions, optional replay angle).
4. 60 FPS stable on MVP scene with all effects active.

## Day 1
1. Final balancing pass on all resources (endurance, grip, fatigue).
2. Score tuning: verify technique bonuses and penalties are balanced.
3. Difficulty multiplier verification across Easy/Medium/Hard.

## Day 2
1. Gym environment: add benches, chalk buckets, background wall panels.
2. Add background elements: gym entrance, water fountain (low-poly).
3. Polish gym lighting: adjust spots, shadows, ambient color.

## Day 3
1. Camera polish: smooth state transitions, avoid wall clipping.
2. Optional replay angle (post-run camera playback).
3. Beta view (quick pre-run camera sweep of the route).

## Day 4
1. Performance profiling: particle budget, shader cost, audio channels.
2. Optimize: reduce draw calls, LOD on background elements.
3. Verify 60 FPS floor on target hardware.

## Day 5
1. Integration test: 60 min continuous session.
2. Fix top remaining bugs.
3. Pre-freeze stabilization.

## Validation Criteria
1. Gym feels like a real indoor climbing space.
2. Camera never clips through walls during climbs.
3. 60 FPS sustained for 30+ minute sessions.
4. Balancing feels fair across all route/mode/difficulty combos.
