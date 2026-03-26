# Sprint 6.0 Checklist (5 days)

## Sprint Objective
Implement the audio system: climbing sounds, breathing, gym ambiance, and gameplay feedback signals.

## Definition of Done
1. Climbing contact sounds (shoe-on-hold, hand-on-hold, chalk).
2. Breathing / effort audio driven by endurance/fatigue state.
3. Gym ambiance (background music, atmosphere sounds).
4. Feedback signals (grip warning, fatigue pulse, fall impact, route complete).
5. Spatial audio positioning on relevant body parts / holds.

## Day 1
1. Create `AudioSystem` with spatial audio support.
2. Implement Layer 2 (climbing sounds): shoe-on-hold, hand-on-hold, hand release.
3. Source/create placeholder audio clips (10-15 clips).

## Day 2
1. Implement chalk application audio + chalk dust sound.
2. Implement Layer 3 (breathing): normal, exertion, fatigue, recovery.
3. Connect breathing state to `endurance_current` thresholds.

## Day 3
1. Implement Layer 1 (gym ambiance): background music + atmosphere emitters.
2. Implement music ducking when endurance < `audio_music_duck_endurance`.
3. Add 2-3 music tracks (placeholder or CC0 sources).

## Day 4
1. Implement Layer 4 (feedback signals): grip warning creak, fatigue heartbeat, fall whoosh + crash pad impact, route complete chime.
2. Connect signals to gameplay events via event bus.
3. Volume mixing: feedback > breathing > climbing > ambiance.

## Day 5
1. Add individual volume sliders (Music, SFX, Ambiance, Feedback).
2. Internal playtest: verify audio enhances climbing readability.
3. List audio asset needs for production quality.

## Validation Criteria
1. Players report climbing feels more immersive with audio.
2. Breathing audio matches visible fatigue state.
3. Feedback signals are noticeable but not annoying.
4. No audio clipping or overlap issues.
