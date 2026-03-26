# Controls

## Objective
Provide precise but readable controls, with a quick learning curve and enough depth for mastery.

## Principles
1. One critical action = one clear input.
2. Basic controls must be understood in under 2 minutes.
3. The player must be able to disable assists to play in expert mode.

## Gamepad Mapping (recommended)

### Movement and Camera
1. `Left Stick`: hips / center of gravity movement.
2. `Right Stick`: free camera.
3. `R3`: recenter camera behind the player.

### Hands
1. `LT`: grab/hold left hand.
2. `RT`: grab/hold right hand.
3. Release the corresponding trigger: release the hand.

### Feet and Push
1. `LB`: place left foot (on a valid nearby hold).
2. `RB`: place right foot (on a valid nearby hold).
3. `A`: leg push (vertical/dynamic move).

### Body Techniques
1. `LB + Left Stick down`: heel hook (left foot).
2. `RB + Left Stick down`: heel hook (right foot).
3. `LB + Left Stick up`: toe hook (left foot).
4. `RB + Left Stick up`: toe hook (right foot).
5. `Left Stick lateral + A`: drop knee / twist lock (direction-dependent).
6. `D-Pad Down`: flag (extend leg for balance).

### Tools and Focus
1. `X`: chalk (restores grip, cooldown).
2. `Y`: focus (brief slow-motion, mental calm cost).
3. `B` (hold): cancel attempt / abandon run.

## Keyboard + Mouse Mapping

Note: all keyboard keys must be configurable from the interface.

### Movement and Camera
1. `WASD`: hips / center of gravity movement.
2. `Mouse`: camera orientation.
3. `C`: recenter camera.

### Hands
1. `Q`: grab/hold left hand.
2. `E`: grab/hold right hand.
3. Release `Q`/`E`: release the corresponding hand.

### Feet and Push
1. `Z`: place left foot.
2. `X`: place right foot.
3. `Space`: leg push.

### Body Techniques
1. `Z + S`: heel hook (left foot).
2. `X + S`: heel hook (right foot).
3. `Z + W`: toe hook (left foot).
4. `X + W`: toe hook (right foot).
5. `A/D + Space`: drop knee / twist lock (direction-dependent).
6. `V`: flag (extend leg for balance).

### Tools and Focus
1. `R`: chalk.
2. `F`: focus.
3. `Esc` (hold): abandon run.

## Key Configuration (mandatory)
1. The player can rebind every keyboard action from `Options > Controls`.
2. The game offers a default preset + a custom preset.
3. Rebindings are persistent (saved between sessions).
4. Key conflicts are detected and flagged in real time.
5. A critical action cannot be left without an assigned key.
6. `Restore Defaults` button available.
7. Key changes must not require a restart.

### Rebinding UX Constraints
1. Clearly display the action being remapped.
2. Visually confirm the newly captured key.
3. Block system-reserved keys or warn the user.
4. Allow quick cancellation of remapping (`Esc`).

## Control Variants

### Assisted Mode (default)
1. Wide hold auto-aim (larger angle window).
2. Semi-assisted foot placement on nearby holds.
3. Higher tolerance for minor timing errors.

### Expert Mode
1. Reduced auto-aim.
2. Strict manual foot placement.
3. Fatigue penalty more sensitive to bad posture.

## Input Priority and Resolution
1. One hand cannot grab two holds at the same time.
2. If `push` is active, lock foot changes during the move window.
3. If both hands are released outside a stable zone, trigger `fall` state.
4. Conflicting inputs apply the last valid input (last valid input rule).

## UX Safeguards
1. Immediate feedback for invalid hold (audio + subtle flash).
2. Contextual action icons displayed near accessible holds.
3. Interactive tutorial for the first 90 seconds with short objectives.
4. Shortcut to display the controls layout while paused.

## Validation Criteria
1. 80% of new players perform 3 consecutive grabs without text assistance after the tutorial.
2. 70% of players understand the difference between `Assisted` and `Expert` on first try.
3. Less than 10% of failures in the first 5 minutes are caused by control confusion.
4. 95% of rebinding attempts succeed without a blocking error.
