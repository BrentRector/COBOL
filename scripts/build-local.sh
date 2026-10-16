#!/usr/bin/env bash
# build-local.sh — THE GATE, as one command (kb/Work PB1708, PB1721, PB3022; docs/rearchitecture/DESIGN-test-build-ci.md
# §3.14.1–3.14.6, §3.17.3; the bash twin of build-local.ps1). It hands the gate to its driver, scripts/run_gate_legs.py,
# which holds this worktree's gate lock, runs the audits (before any slot, kb/Work PB2524), takes a gate slot, runs the
# solution build, lists the population, plans the order and runs leg 1 of it — the change's own tests, the previous
# gate's reds, the host-only witnesses (tier 0h), the cheap tree-wide sweeps (tier 0c) and the cheapest cases the
# change reaches — then checks that population and prints `=== BUILD-LOCAL GATE: … ===`.
# ⛔ ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015/PB3022): there is no mode; the verdict line says
# `LEG 1 ONLY (batched gating, PB2515)`, and CI's merge-group run is the whole-population regression net. Only while
# the owner sets the shared scope to `whole` (`gate_slot.py set-implementer-scope whole`) does it run leg 2 too.
# ⛔ ORDER, DON'T SKIP (owner, 2026-09-28): no gate filters.
# Usage:  bash scripts/build-local.sh
set -u
if [ "$#" -ne 0 ]; then
    echo "usage: $0 — the gate takes no arguments: it has one shape (kb/Work PB3022)" >&2
    exit 2
fi
cd "$(dirname "$0")/.."
# THE ONE INTERPRETER (scripts/python-resolve.sh; kb/Work PB1637, PB3197): a bare `python` died `command not found`
# (exit 127, no verdict line) on a host that has only `python3`. Resolved in a subshell, so a host with no runnable
# Python ends on the gate's own verdict line, NOT RUN with exit 2 as the driver's NOT RUN does, the reason above it.
if ! PY="$(. scripts/python-resolve.sh && printf '%s' "$PY")"; then
    echo "=== BUILD-LOCAL GATE: NOT RUN — python-resolve.sh found no runnable Python (its reason is printed above) ==="
    exit 2
fi
"$PY" scripts/run_gate_legs.py
