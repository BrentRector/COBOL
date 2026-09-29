# PB1708 evidence — the impact map, the registry coupling, and the "order, don't skip" pivot

The measurements behind `docs/rearchitecture/DESIGN-test-build-ci.md` §3.13–§3.14 and kb/Work PB1708 / PB1712.
Every script runs from THIS directory; every path it uses is repository-relative. Frozen outputs sit beside their
scripts as `<name>.txt`.

## Inputs that are NOT in the repository

| input | what reads it | how to reproduce it |
|---|---|---|
| the impact maps at `dbea12428` and `4b0f3e22a` | `common.py#load` → `a1`–`a10`, `a12`, `b4` | `python scripts/spec/record_impact_map.py --commit dbea12428` and `--commit 4b0f3e22a` (about 19 min each at BelowNormal). The recorder writes to `<git common dir>/cobol-impact/` unless given `--store DIR`; the evidence scripts read that default, or the directory named by the environment variable `COBOLNET_IMPACT_STORE` (read only by `common.py`). |
| `a1.pkl` | `a2`, `a3`, `a5`, `a6` | an intermediate written by `a1.py`; run `a1.py` first. |
| raw per-test contexts of a recording | `a11.py` | `record_impact_map.py --work DIR --keep` leaves them under `DIR/rec-<sha>-<n>/raw/contexts/`; pass the `rec-*` directories as arguments. |
| a battery's `conformance.trx` (and `unit.trx`) | `b1.py`, `b5.py`, `b7.py` | `bash scripts/battery.sh <outdir>` writes them (PHASE 1). The committed outputs were measured on battery #87 (2026-09-27). |
| one build's `--list-tests` output and full unfiltered trx, per assembly | `b8.py` | on one build: `dotnet test tests/Cobol.Net.Tests.<A> --no-build --list-tests > <A>.list.txt`, then `dotnet test tests/Cobol.Net.Tests.<A> --no-build --logger "trx;LogFileName=<A>.trx"` (the committed b8.txt: Conformance and Unit, 2026-09-28). |
| the host | `b2-scaling/`, `b3-order-probe/` | re-run on the shared 32-logical-core host; results depend on its load, so record the load beside a new result. |
| `dotnet-stack` | `b2-scaling/classify_stacks.py`'s inputs | a NuGet tool: `dotnet tool install dotnet-stack --tool-path <dir>`, then `dotnet-stack report -p <pid>` while the probe runs. |

## The files

| file | question it answers |
|---|---|
| `a1`–`a6`, `a9`, `a10`, `a12` | the rejected SELECTION design's measurements: how much of the assembly each replayed cluster of trains 65–69 selects, and why (kept because PB1708's decisions cite them). |
| `a7`, `a8` | where the Conformance assembly's recorded (instrumented) test-seconds go. |
| `a11` | PB1710: which child processes wrote no hits file. |
| `b1` | where an UNINSTRUMENTED whole-Conformance run spends its wall clock: the critical path is the twelve continuity partitions. |
| `b2-scaling/` | whether ONE process scales the continuity work across threads (it does not: 3.5× at best) and why (stack samples: the ANTLR lexer's start state). `b2.txt`, `stacks-*.txt`, `classify.txt`. After M6 (kb/Work PB1715), `b2-after-m6.txt`: 32× faster serially, no lock contention, and the next limiter is the workstation GC. |
| `b3-order-probe/` | what xunit 2.9.2's collection and test-case orderers do under parallel collections, and what `StopOnFail` does (it crashes the test host); what a leg-filtering EXECUTOR leaves in the trx (nothing for a dropped case), an empty leg (no verdict line, exit 0), a skipped fact (NotExecuted), and the three ways to refuse a bad environment (a throwing framework constructor falls back SILENTLY to running everything; a throwing executor is a catastrophic failure; `ExecutionErrorTestCase` is a clean red). |
| `b4` | where the reds of the dropped branch 68b X would run under each candidate ORDER, the last being the order plan `scripts/gate_plan.py` builds (PB1717: both reds in leg 1, the first after 0.32 % of the work). |
| `b5` | how big a cheapest-first first leg is, and its serial lower bound. |
| `b7` | whether display names are a stable plan key: appending one golden renames up to 1,398 partitioned corpus rows, and what that, a name key without the partition suffix, and a per-collection cap do to leg 1; the last table re-runs it through `scripts/gate_plan.py` (PB1717: one tier-0u case, a 15.3 s floor). |
| `b8` | whether a full unfiltered trx and `--list-tests` name the same cases: the trx's TEST DEFINITIONS do, its RESULT names do not (theories xunit cannot serialize), and 404 Unit names embed the worktree path. |
| `b6-continuity-processes/` | the continuity cells as the gate runs them, in one process and split across two and four concurrent processes (1.8× and 2.8× faster: the limit is inside one process). |
