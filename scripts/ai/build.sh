#!/usr/bin/env bash
# Optional Unix helper — prefers UNITY_EDITOR_PATH, then common Hub paths.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJ="${ROOT}/UnityProject/ra2-analog"
ART="${ROOT}/artifacts/ai"
mkdir -p "${ART}"
STAMP="$(date +%Y%m%d_%H%M%S)"
LOG="${ART}/build_${STAMP}.log"

UNITY="${UNITY_EDITOR_PATH:-}"
if [[ -z "${UNITY}" || ! -x "${UNITY}" ]]; then
  for c in \
    "/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity" \
    "${HOME}/Unity/Hub/Editor/6000.5.9f1/Editor/Unity"
  do
    if [[ -x "${c}" ]]; then UNITY="${c}"; break; fi
  done
fi

if [[ -z "${UNITY}" || ! -x "${UNITY}" ]]; then
  echo "ERROR: Unity Editor not found. Set UNITY_EDITOR_PATH." >&2
  exit 3
fi

if [[ ! -f "${PROJ}/ProjectSettings/ProjectVersion.txt" ]]; then
  echo "ERROR: project missing at ${PROJ}" >&2
  exit 2
fi

echo "Unity=${UNITY}" | tee "${LOG}"
echo "Project=${PROJ}" | tee -a "${LOG}"
"${UNITY}" -batchmode -nographics -projectPath "${PROJ}" -logFile "${LOG}" -quit
echo "Log: ${LOG}"
