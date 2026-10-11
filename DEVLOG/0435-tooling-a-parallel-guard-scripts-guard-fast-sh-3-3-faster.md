## Entry 435 — Tooling: a parallel guard (scripts/guard-fast.sh) — ~3.3× faster, PROVEN equivalent to serial

The owner asked why the guard can't run tests in parallel (a full serial `scripts/guard.sh` is ~10–12 min and uses
~1 of 32 cores). The bottleneck is the NIST loop: 364 COBOL programs compiled AND run one-by-one in a single shared
directory, where producer/consumer chains (shared `TF###` data files) depend on run order — so it was serial for
correctness, not necessity.

Built three scripts:
- **`scripts/guard-fast.sh`** — parallel COMPILE of all 364 `.cob`→`.dll` (`xargs -P`, independent, distinct names);
  grouped parallel RUN with per-group ISOLATION; unit + integration assemblies run concurrently (`--no-build`).
- **`scripts/guard-run-group.sh`** — runs ONE group serially in a private `mktemp -d` scratch dir, emitting the
  same `NAME: VERDICT` vocabulary as guard.sh (identical normalize / outfile-print-file-stdout resolution /
  FAIL*/footer rules), so a verdict differs from serial only if behaviour does.
- **`scripts/guard-verify.sh`** — the trust gate: runs both guards and diffs the per-test verdicts; they must be
  byte-identical.

**Grouping (three tiers):** self-contained suites (NC/IF/RW) → one isolated dir PER TEST (fully parallel);
file-I/O suites (IC/RL/ST/SM/IX/OBSQ) → the whole suite serially in one scratch dir (exact serial behaviour);
SQ → singletons + the one verified `SQ202A→203A→204A` chain. Different groups run in parallel.

**Why this is SAFE — the key insight:** with isolated CLEAN dirs, a mis-grouping can only make a consumer LOSE its
producer → that consumer goes RED (caught by the verdict diff), NEVER a false GREEN (an isolated clean dir cannot
make a test pass that should fail). Borne out twice: the diff flagged **SM** (its COPY-FILE-DESCR tests READ a data
file an earlier SM test produces — values 97523/23497/… → reclassified SM whole-suite) and **IX111A/IX119A**
(non-adjacent producers → IX kept whole-suite). Each fix re-verified green.

**Result:** ~10–12 min → **~3.3 min (198s)**, guard-fast ALL GREEN with per-test verdicts **byte-identical** to
`scripts/guard.sh` (376 tests). The canonical serial guard is UNCHANGED and remains the authority; guard-verify.sh
is what lets us trust the parallel one. The remaining floor is per-test `dotnet` cold-start (~4s) × the longest
serial group; documented further wins (carve real IX/ST/RL chains out as small groups; a persistent run-host to
amortize cold-start) each must keep guard-verify green. No compiler change. (See memory `feedback_parallel_guard`.)

