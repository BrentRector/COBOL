## Entry 627 — 2026-07-06 14:16 PDT — CI speedup: ~17 min → ~6 min (parallel jobs + parallel guard + a no-emit batch continuity sweep)

The `Build and Test` workflow's single Linux "guard" job ran five heavy activities BACK-TO-BACK (serial
`guard.sh` → greenfield conformance → greenfield unit → CLI build → the INV-1 sweep), so wall-clock was their
SUM (~17 min, and the guard job was often the last thing to go green by many minutes). Three changes, owner-opted
to the fullest scope:

**1. Split the monolith into 4 parallel jobs (commit f706e16).** `guard` (legacy build + unit + integration +
NIST), `greenfield-tests` (conformance + unit), `inv1-sweep`, and `windows-build-test` now run CONCURRENTLY, so
wall-clock is the SLOWEST single job, not the sum. Coverage is unchanged. Added `actions/cache` on
`~/.nuget/packages` (keyed by the csproj/Directory.Packages.props hash) and build-once/`--no-build`.

**2. Parallelise the NIST regression (f706e16).** The guard job now runs `scripts/guard-fast.sh` (the ~350
CCVS-85 baselines fanned across cores with the per-suite isolation model proven equivalent by
`scripts/guard-verify.sh`) instead of the serial `guard.sh`. Verified green + 353 MATCH (identical to `guard.sh`)
on the current tree in ~2.6 min locally. First-run measurements: guard 4.6 min · greenfield 4.3 min · windows
6.1 min — all concurrent.

**3. The real remaining pole was the INV-1 sweep (~29 min on CI) — a ROOT-CAUSE fix.** It ran ~1400 cold `dotnet`
FULL compiles (~350 programs × 4 editions), each running the whole pipeline INCLUDING the Roslyn C#→IL backend.
But the sweep only asks *"does this 85-green program still COMPILE at edition X"* — a verdict fully settled in the
greenfield parse + edition-validate + bind/emit stage, BEFORE the backend. So:
- **`CompilerDriver.Options.CheckOnly`** — a no-emit compile that returns Success once bind/emit succeeds, skipping
  Phase 3 (Roslyn) and all file writes. It is a strictly LENIENT PREFIX of a full compile (it can never FAIL where
  a full compile succeeds), so it is verdict-equivalent for edition continuity; backend/C#-type errors — not an
  edition-continuity concern — are the only difference and are covered by the greenfield conformance suite.
- **`cobol check-batch <manifest>`** — a new CLI subcommand that parse+bind-checks every `source⇥std⇥nist⇥perm`
  entry of a manifest IN PARALLEL in ONE warm process (CompilerDriver.Compile is proven concurrency-safe,
  ConcurrentCompilationTests). The ~1400 cold full compiles collapse to one in-process pass.
- `version-continuity-sweep.sh` now emits one manifest and runs `check-batch` once, then `awk`-aggregates the
  per-(source,std) verdicts into SKIP85 / OK / BREAKS. **Result locally: ~29 min → ~49 s, 440 OK / 19 SKIP85
  (the CM + DB out-of-scope suites, exactly the bind-failures the old sweep also SKIP85'd) / 0 BREAKS** — same
  verdict, ~35× faster. Portability fix: the script now `cd`s to the repo root and uses RELATIVE source paths (an
  absolute MSYS path like `/e/...` is not a path the .NET runtime resolves on a Windows/git-bash checkout).

New `CheckOnlyCompileTests` (4 facts: a good program succeeds and emits no dll/g.cs; the edition-gating verdict is
identical to a full compile at 85/2014/2023). Restructure: the CLI root action moved into `Program.BuildParser()`
so the root stays invokable now that it has a subcommand (else System.CommandLine errors "Required command was not
provided"). Battery: 216 unit GREEN; the sweep + guard-fast verified locally. Net wall-clock target ~6 min.

