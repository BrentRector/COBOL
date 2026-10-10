#!/usr/bin/env bash
# build-local.sh — THE GATE, as one command (kb/Work PB1708, PB1721; docs/rearchitecture/DESIGN-test-build-ci.md
# §3.14.1–3.14.6; the bash twin of build-local.ps1). It hands the gate to its driver, scripts/run_gate_legs.py, which
# holds this worktree's gate lock, runs the audits (before any slot, kb/Work PB2524), takes a gate slot (implementer),
# runs the solution build, lists the population, plans the order and runs it — the WHOLE discovered population of
# Conformance, Unit and Characterization for the lander, leg 1 of it for an implementer (batched gating, kb/Work
# PB2515) — then checks that population and prints `=== BUILD-LOCAL GATE: … ===`.
# ⛔ ORDER, DON'T SKIP (owner, 2026-09-28): no gate filters. --mode is REQUIRED and has no default:
#   implementer  the likely-red cases first, FAIL-FAST, a gate slot (the shared cap, 3 by default); LEG 1 ONLY under
#                the shared implementer scope `leg1` (the default, kb/Work PB2515), both legs under `whole`;
#   lander       one leg, every red of every cluster in one run, no slot.
# Usage:  bash scripts/build-local.sh --mode implementer|lander
set -u
MODE=""
case "${1:-}" in
    --mode) MODE="${2:-}" ;;
    --mode=*) MODE="${1#--mode=}" ;;
esac
if [ "$MODE" != implementer ] && [ "$MODE" != lander ]; then
    echo "usage: $0 --mode implementer|lander — the caller names the gate's mode; it is never inferred" >&2
    exit 2
fi
cd "$(dirname "$0")/.."
python scripts/run_gate_legs.py --mode "$MODE"
