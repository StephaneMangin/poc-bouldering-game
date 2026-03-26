# Audio System

## Objective
Deliver immersive audio feedback that reinforces climbing readability, physical tension, and gym atmosphere.

## Design Approach
Layered audio: ambient bed + dynamic climbing sounds + UI feedback signals. All sounds spatially positioned in 3D.

## Audio Layers

### Layer 1: Gym Ambiance (continuous)
1. Background music (lo-fi / chill): adjustable volume, fades during intense sequences.
2. Other climbers ambient (distant chalk claps, laughter, encouragement): random spatial emitters.
3. Ventilation / HVAC hum: subtle, constant.
4. Distant door sounds, bag zippers: occasional spatial triggers.

### Layer 2: Climbing Sounds (dynamic, player-driven)
1. **Shoe-on-hold**: contact sound varies by hold type (plastic tap, rubber squeak).
2. **Hand-on-hold**: slap / grip sound, intensity varies with grab force.
3. **Chalk application**: powder puff + rubbing sound, 1-2 s duration.
4. **Chalk dust**: subtle particle-associated audio when hands leave holds.
5. **Hand release**: soft release sound, louder if grip was low.
6. **Foot placement**: rubber-on-plastic tap, pitch varies by precision.
7. **Wall brush**: body-to-wall contact (subtle drag/scrape on slab).

### Layer 3: Breathing & Physical Effort (dynamic, state-driven)
1. **Normal breathing**: calm, rhythmic, audible but not dominant.
2. **Exertion breathing**: heavier during dynamic moves, overhangs.
3. **Fatigue breathing**: labored, panting when endurance < 30%.
4. **Recovery sighs**: during rest positions, slower rhythm.
5. **Grunt / effort**: on dyno launch, max-effort reach.

### Layer 4: Feedback Signals (gameplay events)
1. **Grip warning**: subtle creak / tension sound when grip < 40%.
2. **Slip micro-sound**: quick rubber-on-plastic screech on micro-slip.
3. **Fatigue pulse**: low heartbeat-like thump when arm fatigue > 80%.
4. **Fall whoosh**: wind rush during fall, pitch scales with velocity.
5. **Crash pad impact**: deep thud, volume scales with fall height.
6. **Route complete**: satisfying chime / bell ring at top-out.
7. **Checkpoint reached** (Arcade): subtle positive ping.

### Layer 5: UI & Menu Sounds
1. Hold highlight: soft click when a valid hold is detected.
2. Menu navigation: subtle click / hover sound.
3. Score reveal: progressive tick-up sound during debrief.
4. Level up: fanfare / achievement sound.

## Spatial Audio Rules
1. All climbing sounds emitted from the relevant body part or hold position.
2. Gym ambiance uses 3D emitters spread around the gym space.
3. Camera distance affects volume falloff for climbing sounds.
4. Fall sounds use Doppler-like pitch shift for velocity.

## Music System
1. Gym music = diegetic (from speakers in the gym).
2. Volume dips during high-tension sections (endurance < 40%).
3. Music cuts on fall, resumes on respawn.
4. 3-5 tracks minimum, randomized per session.

## Volume Mixing Priority
1. Gameplay feedback signals (highest priority — never masked).
2. Breathing / effort.
3. Climbing contact sounds.
4. Gym ambiance (lowest priority — ducked under gameplay).

## Accessibility
1. Optional visual-only mode (all audio feedback has a visual equivalent).
2. Individual volume sliders: Music, SFX, Ambiance, Feedback.
3. Subtitle-style indicators for critical audio cues (optional).

## Asset Requirements
1. ~30-40 sound clips for climbing interactions.
2. ~10 ambiance loops/one-shots.
3. 3-5 music tracks.
4. ~10 UI sounds.
5. Format: .ogg or .wav, 44.1 kHz, mono for spatial / stereo for music.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.
