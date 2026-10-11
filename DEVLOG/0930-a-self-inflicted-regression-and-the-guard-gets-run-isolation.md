## Entry 930 — 2026-07-19 23:30 PDT — A self-inflicted "regression", and the guard gets run isolation

The guard reported `IX216A: DIFF` / `IX217A: DIFF` after the paired-phrase repair. Chasing it properly took
three steps and produced a real infrastructure fix.

**First, a process mistake of mine.** I ran `dotnet build` while `guard.sh` was in flight — the exact thing
[[feedback_run_guard_script]] forbids, because the NIST leg *executes the binary it just built*. Rebuilding swaps
`cobolsharp.dll`/`CobolSharp.Runtime.dll` mid-loop, so early tests use compiler A and later ones compiler B. I
killed that run rather than trust its verdict, and truncated its output with `tail -30`, losing the failing test
name — so the re-run cost a full 20 minutes to recover information I had already had and thrown away.

**Then the diagnosis, which required not assuming either way:**
1. Stash my changes → rebuild at HEAD → recompile IX216A → **matches the golden**. So NOT pre-existing.
2. Restore my changes → rebuild → recompile → **also matches**. So NOT caused by my edits.
3. Therefore: run conditions. **`TaskStop` kills the wrapper but not its child `dotnet` processes**, which kept
   writing into the shared `tests/nist/output/`. The replacement run's start-clean `rm -f *.txt` raced against
   those stragglers — and IX216A/IX217A are precisely the absent-OPTIONAL-file tests that assert status 05
   ("not present, created"), so *any* stray file fails them. They are named in `guard.sh`'s own comment as the
   canaries for exactly this, and IX is in the §3 guard-flake class.

Both intermediate conclusions would have been wrong if taken alone: the first run's verdict was contaminated,
and "baseline is green" does not by itself acquit a change.

**THE FIX — run isolation in `guard.sh` (~30 lines, no fork).** Forking a second `guard-isolated.sh` was
rejected outright: two mechanisms for one job, in the one script where silent divergence between copies would be
worst. Instead the NIST leg now snapshots the built compiler + the NIST inputs into a per-run temp dir
(`GUARD_WORK`, `GUARD_KEEP=1` to retain for post-mortem) and does all work there. This closes both failure modes:
- the executing binary can no longer be swapped by a concurrent build, so **the "no source edits" window shrinks
  from the whole ~20-minute run to just the build phase**;
- `tests/nist/output/` is no longer shared, so overlapping runs — including stragglers from a killed one —
  cannot corrupt each other.
Cost: a ~40 MB copy, a few seconds. Verified by running it: **=== ALL GREEN ===**, which simultaneously confirms
the paired-phrase repair is safe and that the new mechanism works.

**A performance number worth having** (the owner asked about turnaround): the isolated guard runs
`real 20m6s / user 1m22s / sys 9m38s`. **20 minutes of wall clock for ~11 minutes of CPU** — the NIST loop is
sequential, one program at a time, and it dominates the comprehensive gate far more than the 14-minute
Conformance suite does. Parallelising that loop (it is already partitioned by producer/consumer chains, which is
what makes it non-trivial) is the real lever; `scripts/guard-run-group.sh` suggests the grouping already exists.
Recorded rather than acted on — it is not on the P13 close-line.

Also honest: I over-gated. Plan §3 prescribes wave-local (~2–3 min) per change and comprehensive **once per
batch**; I ran the full comprehensive gate plus full NIST guard for a single change set, twice. Batching the
remaining grammar work under one comprehensive gate is free time back.

