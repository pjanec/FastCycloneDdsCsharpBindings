#!/usr/bin/env bash
set -euo pipefail

# ===================================================================================
# build/test-package.sh
#
# PURPOSE:
#   Smoke-test the CycloneDDS.NET NuGet package (and, optionally, the DdsMonitor
#   global tool) from a local feed — whether the packages were built locally
#   (build/pack.ps1 or dotnet pack) or downloaded from a CI run (the
#   'nuget-packages' artifact). It always picks the NEWEST matching package by
#   modification time, so a cluttered artifacts/nuget with many historical builds
#   is fine.
#
#   It consumes the package as a real PackageReference via examples/PackageSmokeTest:
#   restore -> run the bundled code generator (idlc) -> publish/subscribe round-trip.
#
# USAGE:
#   build/test-package.sh [FEED_DIR]     # default FEED_DIR = artifacts/nuget
#   NO_DDSMON=1 build/test-package.sh    # skip the DdsMonitor tool check
# ===================================================================================

FEED="${1:-artifacts/nuget}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

if [ ! -d "$FEED" ]; then
    echo "ERROR: feed directory not found: $FEED" >&2
    exit 1
fi
FEED_ABS="$(cd "$FEED" && pwd)"

# Newest file matching a glob (by mtime), or empty.
newest() { ls -t "$FEED_ABS"/$1 2>/dev/null | head -n1 || true; }

RT_PKG="$(newest 'CycloneDDS.NET.[0-9]*.nupkg')"
if [ -z "$RT_PKG" ]; then
    echo "ERROR: no CycloneDDS.NET.<version>.nupkg found in $FEED_ABS" >&2
    exit 1
fi
RT_VER="$(basename "$RT_PKG" | sed -E 's/^CycloneDDS\.NET\.(.+)\.nupkg$/\1/')"

echo "============================================================"
echo "  Package smoke test"
echo "  feed:    $FEED_ABS"
echo "  package: $(basename "$RT_PKG")  (version $RT_VER)"
echo "============================================================"

# Force a fresh restore of exactly this build.
rm -rf "$HOME/.nuget/packages/cyclonedds.net" "$HOME/.nuget/packages/cyclonedds.net.ddsmonitor" 2>/dev/null || true
rm -rf "$REPO_ROOT/examples/PackageSmokeTest/bin" "$REPO_ROOT/examples/PackageSmokeTest/obj"

echo ""
echo "[1/2] Running examples/PackageSmokeTest against $RT_VER ..."
dotnet run --project "$REPO_ROOT/examples/PackageSmokeTest" -c Release \
    -p:SmokePkgVersion="$RT_VER" \
    -p:RestoreAdditionalSources="$FEED_ABS"
echo "  [+] runtime package smoke test PASSED"

if [ "${NO_DDSMON:-0}" = "1" ]; then
    echo ""
    echo "Skipping DdsMonitor tool check (NO_DDSMON=1)."
    echo "All good."
    exit 0
fi

MON_PKG="$(newest 'CycloneDDS.NET.DdsMonitor.*.nupkg')"
if [ -z "$MON_PKG" ]; then
    echo ""
    echo "No CycloneDDS.NET.DdsMonitor package in feed — skipping tool check."
    echo "All good."
    exit 0
fi
MON_VER="$(basename "$MON_PKG" | sed -E 's/^CycloneDDS\.NET\.DdsMonitor\.(.+)\.nupkg$/\1/')"

echo ""
echo "[2/2] Testing the ddsmonitor global tool $MON_VER ..."
dotnet tool uninstall --global CycloneDDS.NET.DdsMonitor >/dev/null 2>&1 || true
dotnet tool install --global --add-source "$FEED_ABS" --version "$MON_VER" CycloneDDS.NET.DdsMonitor >/dev/null

TOOL="$HOME/.dotnet/tools/ddsmonitor"
LOG="$(mktemp)"
"$TOOL" --NoBrowser true >"$LOG" 2>&1 &
TOOL_PID=$!

ok=0
for _ in $(seq 1 30); do
    if grep -q "Application started" "$LOG" 2>/dev/null; then ok=1; break; fi
    if ! kill -0 "$TOOL_PID" 2>/dev/null; then break; fi
    sleep 0.5
done

PORT="$(grep -oE 'Now listening on: http://127.0.0.1:[0-9]+' "$LOG" | grep -oE '[0-9]+$' | head -n1 || true)"
code=""
if [ -n "$PORT" ]; then
    code="$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/" 2>/dev/null || true)"
fi

kill "$TOOL_PID" 2>/dev/null || true
wait "$TOOL_PID" 2>/dev/null || true
dotnet tool uninstall --global CycloneDDS.NET.DdsMonitor >/dev/null 2>&1 || true

if [ "$ok" = "1" ] && [ "$code" = "200" ]; then
    echo "  [+] ddsmonitor started (port $PORT) and served HTTP 200 — native loaded OK"
    echo ""
    echo "All good."
    exit 0
fi

echo "  [-] ddsmonitor check FAILED (started=$ok, http=$code). Log:" >&2
tail -n 20 "$LOG" >&2
exit 1
