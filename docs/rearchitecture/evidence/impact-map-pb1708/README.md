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
| a battery's `conformance.trx` | `b1.py`, `b5.py` | `bash scripts/battery.sh <outdir>` writes it (PHASE 1). The committed outputs were measured on battery #87 (2026-09-27). |
| the host | `b2-scaling/`, `b3-order-probe/` | re-run on the shared 32-logical-core host; results depend on its load, so record the load beside a new result. |
| `dotnet-stack` | `b2-scaling/classify_stacks.py`'s inputs | a NuGet tool: `dotnet tool install dotnet-stack --tool-path <dir>`, then `dotnet-stack report -p <pid>` while the probe runs. |

## The files

| file | question it answers |
|---|---|
| `a1`–`a6`, `a9`, `a10`, `a12` | the rejected SELECTION design's measurements: how much of the assembly each replayed cluster of trains 65–69 selects, and why (kept because PB1708's decisions cite them). |
| `a7`, `a8` | where the Conformance assembly's recorded (instrumented) test-seconds go. |
| `a11` | PB1710: which child processes wrote no hits file. |
| `b1` | where an UNINSTRUMENTED whole-Conformance run spends its wall clock: the critical path is the twelve continuity partitions. |
| `b2-scaling/` | whether ONE process scales the continuity work across threads (it does not: 3.5× at best) and why (stack samples: the ANTLR lexer's start state). `b2.txt`, `stacks-*.txt`, `classify.txt`. |
| `b3-order-probe/` | what xunit 2.9.2's collection and test-case orderers do under parallel collections, and what `StopOnFail` does (it crashes the test host). |
| `b4` | where the reds of the dropped branch 68b X would run under each candidate ORDER. |
| `b5` | how big a cheapest-first first leg is, and its serial lower bound. |
| `b6-continuity-processes/` | the continuity cells as the gate runs them, in one process and split across two and four concurrent processes (1.8× and 2.8× faster: the limit is inside one process). |
