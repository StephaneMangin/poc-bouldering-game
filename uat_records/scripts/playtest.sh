#!/usr/bin/env bash
# UAT Records — scenario runner for Linux.
#
# Usage:
#   ./uat_records/scripts/playtest.sh scenarios/walk_forward.json [artefacts_subdir]
#
# Display strategy:
#   - If $DISPLAY is set and reachable, run on the live X server (best path: real
#     graphics device, screenshots work).
#   - Else, if xvfb-run is on PATH, wrap with a virtual framebuffer.
#   - Else, abort: ScreenCapture needs a graphics device.
#
# Requirements:
#   - Unity Editor: $UNITY_EDITOR pointing to the binary, or 'unity-editor' on PATH.
#   - ffmpeg (optional, for PNG → MP4 post-process).
set -euo pipefail

SCENARIO_PATH="${1:-}"
if [[ -z "$SCENARIO_PATH" ]]; then
  echo "Usage: $0 <scenario.json> [artefacts_subdir]" >&2
  exit 1
fi
if [[ ! -f "$SCENARIO_PATH" ]]; then
  echo "Scenario not found: $SCENARIO_PATH" >&2
  exit 1
fi

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PKG_ROOT="$REPO_ROOT/uat_records"
UNITY_PROJECT="$REPO_ROOT/unity"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
SUBDIR="${2:-run_${TIMESTAMP}}"
ARTEFACTS_DIR="$PKG_ROOT/artefacts/$SUBDIR"
mkdir -p "$ARTEFACTS_DIR"

UNITY_BIN="${UNITY_EDITOR:-$(command -v unity-editor || true)}"
if [[ -z "$UNITY_BIN" ]]; then
  echo "Unity Editor binary not found. Set UNITY_EDITOR=/path/to/Editor/Unity" >&2
  exit 1
fi

SCENARIO_ABS="$(readlink -f "$SCENARIO_PATH")"
LOG_FILE="$ARTEFACTS_DIR/unity.log"

echo "[uat_records] scenario  = $SCENARIO_ABS"
echo "[uat_records] artefacts = $ARTEFACTS_DIR"
echo "[uat_records] log       = $LOG_FILE"

# Resolve display strategy.
USE_XVFB=0
if [[ -n "${DISPLAY:-}" ]] && command -v xdpyinfo >/dev/null 2>&1 && xdpyinfo >/dev/null 2>&1; then
  echo "[uat_records] display   = live ($DISPLAY)"
elif [[ -n "${DISPLAY:-}" ]]; then
  echo "[uat_records] display   = live ($DISPLAY, unchecked)"
elif command -v xvfb-run >/dev/null 2>&1; then
  USE_XVFB=1
  echo "[uat_records] display   = xvfb (virtual)"
else
  echo "[uat_records] ERROR: no DISPLAY and no xvfb-run — graphics device unavailable, screenshots impossible." >&2
  echo "[uat_records] Either start an X server, set DISPLAY, or install xvfb (sudo apt install xvfb)." >&2
  exit 1
fi

# Unity invocation. We deliberately omit '-quit' and '-nographics':
#   - '-quit' would exit right after Run() returns (before play mode starts).
#   - '-nographics' disables the graphics device → ScreenCapture cannot work.
EXIT_CODE=0
RUN_CMD=("$UNITY_BIN"
  -batchmode
  -projectPath "$UNITY_PROJECT"
  -executeMethod UATRecords.Editor.PlaytestCLI.Run
  -scenario "$SCENARIO_ABS"
  -artefacts "$ARTEFACTS_DIR"
  -logFile "$LOG_FILE")

if [[ "$USE_XVFB" -eq 1 ]]; then
  xvfb-run -a -s "-screen 0 1280x720x24" "${RUN_CMD[@]}" || EXIT_CODE=$?
else
  "${RUN_CMD[@]}" || EXIT_CODE=$?
fi

echo "[uat_records] Unity exit code = $EXIT_CODE"

# Optional PNG → MP4
SCREENSHOTS_DIR="$ARTEFACTS_DIR/screenshots"
if command -v ffmpeg >/dev/null 2>&1 && [[ -d "$SCREENSHOTS_DIR" ]]; then
  FRAME_COUNT="$(find "$SCREENSHOTS_DIR" -maxdepth 1 -name 'frame_*.png' | wc -l)"
  if [[ "$FRAME_COUNT" -gt 0 ]]; then
    # Compute framerate so playback matches real gameplay duration.
    # Duration line in report.md looks like: "- Duration: 6.15s (target 6s)"
    REPORT_FILE="$ARTEFACTS_DIR/report.md"
    DURATION_S=""
    if [[ -f "$REPORT_FILE" ]]; then
      DURATION_S="$(grep -oE 'Duration: [0-9]+\.[0-9]+' "$REPORT_FILE" | awk '{print $2}')"
    fi
    if [[ -n "$DURATION_S" ]]; then
      FPS="$(LC_ALL=C awk -v f="$FRAME_COUNT" -v d="$DURATION_S" 'BEGIN{ if (d>0) printf "%.3f", f/d; else print 30 }')"
    else
      FPS=30
    fi
    echo "[uat_records] encoding $FRAME_COUNT frames at ${FPS} fps (duration=${DURATION_S:-?}s) to MP4..."
    ffmpeg -y -loglevel error -framerate "$FPS" \
      -i "$SCREENSHOTS_DIR/frame_%05d.png" \
      -pix_fmt yuv420p \
      "$ARTEFACTS_DIR/recording.mp4"
    echo "[uat_records] video     = $ARTEFACTS_DIR/recording.mp4"
  else
    echo "[uat_records] no PNG frames found — skipping MP4 encode."
  fi
fi

exit "$EXIT_CODE"
