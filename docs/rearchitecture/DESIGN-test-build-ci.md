# DESIGN — Test, Build & CI Architecture (+ Migration Safety)

Status: DESIGN (rearchitecture dimension). Author: rearchitecture review. Date: 2026-07-07.
Scope: the harness/build/CI machinery that KEEPS THE BATTERY GREEN through the clean-slate rearchitecture of
`src/Cobol.Net.*`, plus the diagnostic-code registry, the characterization strategy, and the roadmap/DEVLOG
discipline that make each phase resumable and behavior-neutral.

This dimension underpins every other rearchitecture dimension: none of the god-class splits, pass-pipeline
extractions, or storage-form unifications can proceed safely unless the test net can PROVE, per phase, that
observable behavior did not change. Today the net can prove agreement with a *frozen legacy engine that is deleted
at G8* — so the net itself must be rearchitected first, or it evaporates mid-migration.

---

## 1. Current state (as-built, grounded)

### 1.1 Test projects (four, two stacks)
| Project | Files | Role |
|---|---|---|
| `tests/Cobol.Net.Tests.Conformance` | ~80 `.cs` | ACTIVE greenfield net: differential feature tests (`*DifferentialTests.cs`), `NistDifferentialTests` (the corpus goldens, `[MemberData]` over `corpus.tsv` — see §3.2), `VersionMatrixTests`, `CorpusRunnerTests`, `EditionHarness`, `CompilerUnderTest`/`CutRunner`. Those three theory-heavy families are **PARTITIONED across xUnit collections** — §3.11. |
| `tests/Cobol.Net.Tests.Unit` | ~17 `.cs` | Greenfield unit: runtime kernels, `ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, `CheckOnlyCompileTests`, CLI parser, `EditionContextTests`. |
| `tests/CobolSharp.Tests.Integration` | legacy | FROZEN legacy oracle + the post-85 `ConformanceTests` corpus runner with the `GreenfieldOnly` / `LegacyDivergent` skip sets. |
| `tests/CobolSharp.Tests.Unit` | legacy | FROZEN legacy unit tests. |

### 1.2 The differential oracle (the load-bearing risk)
`CompilerUnderTest.cs` defines `ICompilerUnderTest` with two impls: `LegacyCompiler` (drives the frozen
`CobolSharp.Compiler.Compilation`) and `CobolNetCompiler` (drives `CompilerDriver`). ~60 `*DifferentialTests.cs`
compile the SAME source with both and assert byte-identical normalized stdout (`CutRunner.Normalize` = the guard's
`normalize()`: drop CR, per-line trailing-trim). **The legacy engine is deleted at G8** (design SSOT + the csproj
comment on the legacy `ProjectReference`). At that moment every dynamic differential test loses its oracle.

`NistDifferentialTests` is DIFFERENT and safe: it compares WiseOwl COBOL output to committed goldens
(`tests/nist/valid/*.txt`), not to a live legacy run. But the green NIST set is a hand-maintained ~318-row
`[InlineData]` list — a THIRD copy of "which programs are green" (the others: `scripts/guard.sh` `NIST_TESTS`, and
implicitly `tests/nist/chains.tsv`).

### 1.3 Guard scripts (bash, Linux-only NIST loop, over the WiseOwl COBOL CLI)
⛔ **The NIST leg drives `cobol` (`src/Cobol.Net.Cli`), not the legacy CLI** — since kb/Work/PB750, which found
that both guards had hard-coded `src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll`. That binary's project
graph is `CobolSharp.CLI → CobolSharp.Compiler → Cobol.Net.Frontend`; `Cobol.Net.Compiler` — the Roslyn code
generator that IS WiseOwl COBOL — is not in it, so the leg was structurally blind to every greenfield codegen defect
and each battery's headline `guard NIST: 353 MATCH` was a true statement about the ORACLE. Battery #58 is the
demonstration: `NC215A` printed a wrong answer (PB741) that `NistDifferentialTests_P0` caught and the guard's
353-MATCH/audit-CLEAN NIST line could not see.
- `scripts/guard-compiler.sh` — ⭐ **the ONE place that answers "which compiler is this gate measuring?"**,
  sourced by both guards and by `run-suite.sh`. `cobol` by default; the legacy oracle under `GUARD_COMPILER=legacy`
  (NIST leg only) or the project's existing opt-in switch `COBOLSHARP_LEGACY_DIFFERENTIAL=1`, which **also**
  switches `CobolSharp.Tests.Integration`'s `ConformanceTests` into their opt-in legacy-differential corpus (that
  assembly reads the same variable) — a separate and much larger gate, so the banner says so and
  `GUARD_COMPILER=legacy` exists to change ONLY the compiler. `guard_assert_compiler_identity` reads the resolved
  CLI's own `.deps.json` — the build's record of its project graph — and REFUSES to run when the closure does
  not match the compiler the guard claims (present `Cobol.Net.Compiler` for `cobol`, absent for `legacy`), so a
  path typo, a stale bin dir or a project rename can no longer silently re-point the gate. `--self-test` proves
  both directions of that refusal fire; `guard-verify.sh` runs it, so `battery.sh` phase 2a does too.
- `scripts/guard-compile.sh` — the compile invocation, written once: `cobol` takes the test name as
  `--nist NAME` (its parser binds the next token, so `--nist prog.cob` would consume the SOURCE), the legacy
  takes a bare `--nist`. Called by the serial guard, the parallel compile and its serial re-observation retry.
- `scripts/guard.sh` — serial: builds the CLI under test, runs the legacy unit+integration suites (a separate
  leg — they gate `CobolSharp.Compiler` and, through it, the SHARED `Cobol.Net.Frontend`), then compiles+runs
  376 NIST programs THROUGH THE COMPILER UNDER TEST and diffs against `tests/nist/valid/`. Carries
  `LEGACY_DIVERGENT` (11 ISO-rebaselined goldens the LEGACY legitimately differs on — **emptied unless the run
  is the legacy differential**, since under `cobol` those goldens are exactly what the compiler must reproduce)
  and the golden-cleanliness sweep. Every summary line names the compiler: `=== NIST (cobol): … ===`.
- `scripts/guard-fast.sh` — parallel version. Isolation is now the CONNECTED COMPONENTS of `corpus.tsv`'s
  declared `chain-preds` (332 groups over 376 programs, longest 9), replacing the former per-suite heuristic —
  see §3.10's grouping row. Verdicts are checked ABSOLUTELY by `guard-nist-audit.sh` against the manifest, which is a
  stronger check than `guard-verify.sh`'s diff against the serial guard (that one is relative: it cannot see the
  two guards deviating together).
- `scripts/guard-run-group.sh` — one group, serial, in its own scratch dir; owns the per-test EVIDENCE RULES.
- `scripts/guard-nist-audit.sh` — the population/manifest/expectation audit, consumed by BOTH guards.
- `scripts/guard-verify.sh` — the serial↔parallel equivalence proof. ⚠ Its verdict filter had silently omitted
  `LEGACY DIVERGENT`, dropping 11 programs from both sides; the vocabulary is complete now and an unrecognized
  verdict-shaped line is reported rather than discarded.
- `scripts/version-continuity-sweep.sh` — INV-1: one warm `cobol check-batch` over ~350 programs × 4 editions
  (no Roslyn), fails on any `BREAKS`. THIS one drives the greenfield CLI.
- `scripts/compliance.sh`, `nist-batch.sh`, `run-suite.sh` — ad-hoc dashboards. `run-suite.sh` selects its
  compiler through `guard-compiler.sh` like the guards; `nist-batch.sh` drives `cobol` directly and is NOT a
  gate (it duplicates guard-fast without isolation, chains, evidence rules or the audit).

**NIST is measured twice, by two different paths, and they are ONE POPULATION.** `NistDifferentialTests`
(6 partitions) drives `CompilerDriver` IN-PROCESS over the 349 green∪divergent rows on both OSes;
`guard-fast.sh` drives the `cobol` CLI as a separate process over the whole 376-program in-scope corpus from
bash on Linux. Neither subsumes the other — one covers the library API and Windows, the other covers the
shipped exe, the CCVS chain-isolation model and the compile+run health of the golden-less programs — and both
derive their population AND their expected verdict from `tests/nist/corpus.tsv`. `CorpusManifestTests` asserts
that structurally (guard population ⊇ every asserted program, ⊆ the manifest, surplus only `pending` rows, and
neither guard hard-coding a CLI path); `guard-nist-audit.sh` asserts it dynamically, per program, per compiler.

### 1.4 CI (`.github/workflows/build-and-test.yml`) — a gated matrix behind ONE required check
Ten jobs. `changes` decides whether the matrix runs at all; then, concurrently, `guard` (WiseOwl COBOL parallel NIST +
legacy unit/integration, ubuntu) · `greenfield-conformance` (ubuntu, 6 shards) · `greenfield-unit` (ubuntu) ·
`windows-conformance` (windows, the same 6 shards, Release) · `conformance-population` (the shards' union against
each platform's discovered population, `scripts/test_population.py`, §3.14.4) ·
`inv1-strong-2023` · `windows-build-test` (Release warnings-as-errors) · `legacy-oracle` (schedule/dispatch only);
and finally `ci-gate`. NuGet cached; `Generated/` regenerated per checkout (java+pwsh prerequisites).

**⛔ `ci-gate` is a REQUIRED status check on `main`** (owner decision 2026-09-06, question 23; `kb/Work/PB796`),
with `enforce_admins: true` — an administrator exemption would be worthless when every push to this repository is
made with the owner's credentials. It is the terminal job: `if: always()`, `needs:` every other job, and it PASSES
only when each of them is `success` or `skipped` (a whitelist, so `cancelled` and any future result value fail).
It is the only name in the file a protection rule can require — the conformance jobs are matrices whose check
names carry the shard AND its filter text (`Greenfield conformance (Linux, sharded) (corpus-nist, FullyQualified…`,
measured on `5bf5a7f1`), so any shard edit renames the context, and every matrix job is skipped on a docs-only
push, which a required check reads as "never reported".

**The doc-only skip moved one layer down.** `paths-ignore` used to make the workflow decline to START for a
`DEVLOG.md` / `docs/**` / `kb/**` / `PROMPT.md` / `CLAUDE.md` / `README.md` push — incompatible with a per-commit
required check, which has nothing to satisfy when no run exists. The identical list now lives in `changes` (one
copy, no trigger-side twin to drift against), which diffs `github.event.before .. github.sha` and outputs
`run_matrix`; every other job carries `needs: changes` + `if: needs.changes.outputs.run_matrix == 'true'`. When
`before` is unusable — which is EVERY landing, since `ci/<short-sha>` is always a new branch — the base is
`merge-base(origin/main, sha)`. `changes` also short-circuits a sha that already carries a green `ci-gate` check
run, so the fast-forward onto main does not pay for the matrix a second time, and it carries the drift check that
asserts every job id is in `ci-gate`'s `needs` list.

**The landing protocol is `scripts/push-main.sh`, and it is the only way a commit reaches main**: push HEAD to
`ci/<short-sha>` → find the run for that exact sha → `gh run watch --exit-status` → on green fast-forward main and
delete the branch, on red print the failing jobs and exit non-zero with main untouched. A check run is attached to
the COMMIT, not to a branch, which is why the verdict earned on `ci/<sha>` is the one the protection reads on main.
The push trigger is therefore `branches: [ main, 'ci/**' ]`; `cancel-in-progress` is restricted to `pull_request`,
because a cancelled push run leaves its sha permanently unlandable.

### 1.5 Drift discipline (a genuine strength — preserve it)
`ConstructRegistryDriftTests` binds `constructs.json` ↔ in-code `ConstructRegistry` both directions;
`CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap` proves every on-disk `.cob` is manifest-listed;
`ReservedWordsDriftTests` binds `reserved-words.json`. Nothing silently undiscovered.

### 1.6 Diagnostics
161 distinct `COBOLNET####` codes exist as **bare string literals**; only 4 are named consts (`EditionCodes.cs`).
`COBOLNET0899` (catch-all "recognized-not-implemented") appears at ~47 sites; several codes are reused for
unrelated rules (e.g. `1533` ×3). There is no registry, no `docs/DIAGNOSTICS.md`, no per-code metadata, so tests
and `--suppress` cannot target a specific rule and the version matrix cannot enumerate diagnostics.

### 1.7 The fleet build guard (`scripts/hooks/fleet_active_build.py`) — a PreToolUse gate on `dotnet *`
Wired in `.claude/settings.json` for both `Bash(dotnet *)` and `PowerShell(dotnet *)`. It **denies**
`dotnet build|test|clean|publish|run|msbuild` while a live subagent is probing the binaries the build would
replace — the guard that exists because ~6 rebuilds under a running 60-agent fleet made all 60 verdicts unusable
(2026-08-04, PB15). Liveness is transcript MTIME within 120 s, scoped to this session's id.
**The unit of the freeze is the WORKING TREE, not the session (2026-09-01).** It denies iff some *foreign* live
agent works in the caller's own tree, and both trees are derived, never listed: the caller's tree is the nearest
ancestor of the payload `cwd` holding a `.git` entry (a directory in the main checkout, a file in a worktree);
the main checkout is that root, or the `gitdir:` target parsed out of the worktree's `.git` file (parsed, not
shelled out — this runs before every `dotnet` call); and a foreign agent's tree is
`<main>/.claude/worktrees/agent-<agentId>` **when that directory exists**, else the main checkout, because
`Agent(isolation="worktree")` creates exactly that path. So N implementer agents in N worktrees build in
parallel, a main-tree build is still denied by a live main-tree agent, and a main-tree build is *allowed* while
only worktree agents are live. Fail-open on any error; an **unknown** tree (unreadable/unparseable `.git`)
reverts to the old session-wide deny rather than to an allow. `--self-test` fires every branch over real
temporary worktrees and is the only thing that proves the ALLOW arm — three of this hook's four defects were it
failing closed.

---

## 2. Problems this design must fix

1. **The net evaporates at G8.** ~60 differential tests are oracle-coupled to code that gets deleted.
2. **No behavior-neutrality proof for a refactor.** A rearchitecture phase that splits `StatementBinder` or unifies
   `StoreAsImage` has NO characterization gate: it relies on output goldens for the subset of programs that happen
   to be in the corpus, and nothing snapshots the generated C# to catch an unintended emit change.
3. **"Which programs are green" has three sources of truth** (`guard.sh` `NIST_TESTS`, `NistDifferentialTests`
   `[InlineData]`, `chains.tsv`) that can silently disagree.
4. ~~**The authoritative NIST gate tests frozen code.**~~ **CLOSED 2026-09-06 (kb/Work/PB750).** CI's heaviest job
   (`guard`) drove the legacy engine for the whole of the rearchitecture, so the compiler under development was
   gated on NIST only by the in-process differential suite — and battery #58's `NC215A` wrong answer proved the
   cost. Both guards now resolve their compiler through `scripts/guard-compiler.sh` (default `cobol`, the legacy
   only under `COBOLSHARP_LEGACY_DIFFERENTIAL=1`), refuse to start unless the resolved binary's `.deps.json`
   closure matches, and name the compiler in every verdict line.
5. **NIST loop is Linux-only bash**, so the Windows job cannot run it; the authoritative regression has an OS gap.
6. **Diagnostics are unaddressable.** No registry ⇒ the version matrix, `--suppress`, and per-rule tests cannot key
   on a stable id; a renumber/reuse is invisible.
7. **`CheckOnly` runs full Emit** (no Bind phase boundary) ⇒ the fast verdict path is not actually fast, and a
   characterization harness cannot snapshot the bound tree independently of emission.
8. **Roslyn reference set uncached** ⇒ the in-process battery pays ~180 `MetadataReference.CreateFromFile` per
   compile (thousands of times per run).
9. **Roadmap SSOT was a 100 KB `resume-prompt.md`** (since absorbed into `docs/COBOLNET_REARCHITECTURE_PLAN.md` and deleted) — excellent for session resume, but there is no compact,
   phase-structured, exit-criteria-bearing rearchitecture roadmap doc that a future engineer resumes the
   MIGRATION (not the feature drive) from.

---

## 3. Target design

### 3.0 Guiding principle: three neutrality gates, ranked
A rearchitecture phase is "behavior-neutral" iff, in order of authority:
1. **Output goldens** (primary, spec-anchored): NIST `valid/*.txt` + corpus `.out` + negative `.err`. These are the
   ISO-conforming oracle and MUST stay byte-identical across every refactor phase. Non-negotiable.
2. **Diagnostic goldens** (primary): every emitted diagnostic (code + message + location) for a fixed corpus of
   positive and negative programs is snapshotted; a refactor must not change the diagnostic surface.
3. **Emitted-C# snapshots** (secondary, advisory): the generated `.g.cs` for a representative program set is
   snapshotted. A refactor that is a pure reorg SHOULD leave these byte-identical; a refactor that intentionally
   changes emission form (e.g. structured `Place` → different but equivalent C#) RE-BASELINES the snapshot **with
   explicit review**, and gate (1) proves the re-baseline is behavior-preserving.

Gates (1) and (2) are hard (red = stop). Gate (3) is a reviewed diff, never auto-accepted, never silently drifting.

### 3.1 Test project taxonomy (target)
Collapse to a coherent, layered set under one namespace root `CobolNet.Tests.*`:

| Project | Replaces | Contents |
|---|---|---|
| `tests/Cobol.Net.Tests.Unit` | (kept) | Pure unit: runtime kernels, binder/emitter component units, CLI/driver, drift tests. Fast, no Roslyn where avoidable. |
| `tests/Cobol.Net.Tests.Conformance` | (kept, refocused) | End-to-end program behavior: NIST goldens, per-edition corpus, version matrix, negative corpus. Compares to COMMITTED goldens only — no live legacy oracle. |
| `tests/Cobol.Net.Tests.Characterization` | NEW | The behavior-neutrality harness: diagnostic snapshots + emitted-C# snapshots (gates 2 & 3) over a fixed "characterization corpus". Runs on every phase. |
| `tests/CobolSharp.Tests.*` | DELETE at G8 | Frozen legacy. Retired once the oracle is baked (see 3.4). |

Rationale: the legacy `ConformanceTests` corpus runner and its `GreenfieldOnly`/`LegacyDivergent` sets are made
redundant by `CorpusRunnerTests` once the legacy oracle is baked out — one corpus runner, not two.

### 3.2 The green-corpus single source of truth: `tests/nist/corpus.tsv`
Replace the three copies with ONE declarative manifest, consumed everywhere.

```
# tests/nist/corpus.tsv — the ONE list of NIST programs and their status.
# name  suite  status         chain-preds(space-joined, '-' if none)   golden(valid|none)   note
NC101A  NC     green          -                                        valid                first NC program
ST103A  ST     green          ST101A ST102A                            valid                SORT USING/GIVING chain
IX999Z  IX     pending        -                                        none                 not yet compiling
```

- `status ∈ {green, pending, divergent}`. `green` ⇒ a `[Theory]` row asserting golden match. `divergent` ⇒ the
  `LEGACY_DIVERGENT` set (compiled+run, output NOT compared to a legacy run — the golden is already ISO-conforming;
  the note carries the ISO citation). `pending` ⇒ catalogued, not asserted (the mass-red guard).
- `chain-preds` REPLACES `chains.tsv` (folded in — one file).
- `NistDifferentialTests` reads `corpus.tsv` via `[MemberData]`, NOT a hand-maintained `[InlineData]` list.
- The bash guard reads the same file for its `NIST_TESTS` and `LEGACY_DIVERGENT`, and since kb/Work/PB750 its
  EXPECTED verdict is derived per compiler: a `divergent` row expects `LEGACY DIVERGENT` under the legacy
  oracle and `MATCH` under `cobol` (the golden IS the ISO-conforming output WiseOwl COBOL must reproduce).
  ⛔ **`LEGACY_DIVERGENT` is DERIVED, through one reader** — `scripts/guard-population.sh`'s
  `guard_legacy_divergent()`, sourced by `guard.sh` AND `guard-fast.sh` — and an unreadable or divergent-free
  manifest is a LOUD non-zero return, never an empty exemption set. Until kb/Work PB898 this sentence described
  an intent the code did not keep: `guard.sh` held the names as a hand-written string, `guard-fast.sh` `sed`-ed
  that string out of it, and `guard-nist-audit.sh` derived the same fact from the manifest — three writings, no
  comparison. The string had drifted a program behind (THIRTEEN rows, TWELVE names; `SQ212A`), so the runner
  scored a difference the auditor beside it expected.
- A drift test (`CorpusManifestTests`) asserts: every `tests/nist/programs/*.cob` is listed; every `green` row has a
  `valid/<name>.txt`; every `divergent` row has a non-empty note containing a `§` citation; and
  `GuardScripts_CarryNoHandMaintainedNistNameList` fails on ANY NIST-shaped name list written out by hand in any
  `scripts/guard*.sh`, so the next such list cannot be a different variable and go unseen.

This kills smell #3 and makes "add a green program" a one-line manifest edit.

### 3.3 The characterization harness (`Cobol.Net.Tests.Characterization`)
The behavior-neutrality proof the rearchitecture needs. Structure:

```
tests/Cobol.Net.Tests.Characterization/
  CharacterizationCorpus.cs      # discovers tests/characterization/**/*.cob (+ per-file .std directive)
  DiagnosticSnapshotTests.cs     # gate (2)
  EmittedCSharpSnapshotTests.cs  # gate (3)
  Snapshots/                     # committed golden snapshots (.diag.txt, .g.cs.txt)
tests/characterization/
  positive/*.cob                 # programs that compile clean — snapshot their emitted C#
  negative/*.cob                 # programs that must diagnose — snapshot their diagnostic list
  README.md
```

Signatures (concrete):

```csharp
public interface ICompilerProbe {
    // Bind-only: no Roslyn. Returns the diagnostic list (code+message+location) and, optionally, the emitted C#.
    ProbeResult Probe(string source, int edition, bool emit);
}
public sealed record ProbeResult(
    bool Ok,
    IReadOnlyList<Diagnostic> Diagnostics,   // structured — see 3.5
    string? EmittedCSharp);                   // null unless emit:true
```

- `DiagnosticSnapshotTests` compiles each `characterization/**` program, formats its diagnostics deterministically
  (`{code}\t{severity}\t{line}:{col}\t{message}`), and asserts equality with `Snapshots/<name>.diag.txt`.
- `EmittedCSharpSnapshotTests` emits the C# for each `positive/*.cob` and asserts equality with
  `Snapshots/<name>.g.cs.txt` (line-ending normalized). Roslyn is NOT invoked — this snapshots the emitter output
  string, which is cheap and deterministic.
- Re-baselining: `COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test …/Characterization` rewrites the `Snapshots/` files. This
  is a DELIBERATE, reviewed action (the diff shows in the PR). Gate (1) — output goldens — proves any re-baseline
  is behavior-preserving. Never run in CI; CI always compares.

The characterization corpus is seeded ONCE, at the START of the rearchitecture, from the current (pre-refactor)
emitter, so the first snapshot is "how the emitter emits today." Every subsequent phase must match it (or
review-re-baseline). This is the missing "prove I changed nothing" gate.

### 3.4 Oracle bake-out: converting the differential net to goldens (the G8-survival migration)
The ~60 `*DifferentialTests.cs` currently assert `cobolnet == legacy` at RUN time. Before the legacy engine is
deleted, run a ONE-TIME bake that freezes the legacy output as a committed golden, then rewrite each test to assert
`cobolnet == golden`.

Mechanism:
1. Add `CutRunner`-driven `DifferentialBakeTool` (a `[Fact(Skip=…)]`-gated maintenance test, or a small
   console tool in `tools/`) that, for every differential test's source, runs `LegacyCompiler`, captures normalized
   stdout, and writes `tests/differential/<TestClass>/<case>.out`.
2. Rewrite the differential base to a golden-comparison base:
   ```csharp
   protected void AssertMatchesGolden(string source, string goldenName, int edition = 85)
       => Assert.Equal(ReadGolden(goldenName), Cn.CompileAndRun(source).stdout);
   ```
   The legacy `ICompilerUnderTest`/`LegacyCompiler` and the `CobolNet==Legacy` asserts are deleted.
3. A `divergent`-style escape hatch stays for the handful of legacy-non-conforming cases (already tracked in
   `LegacyDivergent`) — those goldens are hand-authored to the ISO value.

After the bake, the entire greenfield net is self-standing: it depends on committed goldens, not on the legacy
`ProjectReference`. The legacy test projects can be deleted at G8 with zero net loss.
⛔ **The `guard.sh` NIST loop cannot** — not any more (kb/Work/PB750). It no longer drives the legacy engine: it
is the CLI-level, separate-process, whole-in-scope-corpus NIST leg over `cobol`, and deleting it would drop the
27 golden-less/pending programs' compile+run health, the CCVS chain-isolation model and the shipped exe's own
path. What retires at G8 is `COBOLSHARP_LEGACY_DIFFERENTIAL=1` (the legacy arm of `guard-compiler.sh`) and the
`LEGACY_DIVERGENT` list, not the leg.
**This bake is a PREREQUISITE gate for G8 and for retiring the legacy CI job.**

### 3.5 Diagnostic-code registry (`Cobol.Net.Diagnostics`)
A first-class subsystem (also required by the Editions dimension). Minimal shape:

```csharp
public sealed record DiagnosticDescriptor(
    string Code,           // "COBOLNET1533" — one code, one rule
    Severity DefaultSeverity,
    string SpecRef,        // "ISO §8.4.3.9.4" — the citation
    string MessageTemplate,// "strong TYPE mismatch: {0} vs {1}"
    string? SuppressKey);  // stable --suppress token

public static class Diag {                    // the catalogue (generated-friendly consts)
    public static readonly DiagnosticDescriptor StrongTypeMoveMismatch = new("COBOLNET1533", …);
    // … one entry per distinct rule …
}
public readonly record struct Diagnostic(
    DiagnosticDescriptor Descriptor, string Message, SourceSpan Location, Severity Severity);
```

- `EditionContext.Error("COBOLNET####", text)` → `sink.Report(Diag.X, args…)`. The 47 `0899` sites split into
  named unimplemented-feature descriptors behind a tracked list; `1533`-style reuse is un-merged into distinct
  codes.
- `DiagnosticRegistryDriftTests` (in `Tests.Unit`): every `Diag.*` descriptor has a unique code; every code emitted
  anywhere in the compiler is a registered descriptor (a source-scan or a runtime-collected set). Mirrors the
  existing `ConstructRegistryDriftTests` discipline.
- A build step (or a `[Fact]` that writes when `COBOLNET_UPDATE_DOCS=1`) generates `docs/DIAGNOSTICS.md` from the
  catalogue — the single human-readable code table understandability #1 asks for.

This makes gate (2) precise (snapshots key on stable codes) and lets the version matrix and `--suppress` target a
rule.

**The wire encoding of a diagnostic is part of the diagnostic** (`kb/Work/PB899`). A message carries its ISO
citation, so it carries non-ASCII — `(ISO §11.10.3 SR5–6)` holds U+00A7 and U+2013. `CobolNet.Runtime.IO.StandardStreams.EnsureUtf8()`
is the ONE place that rule is written down, and BOTH entry points call it: `Program.Main` in the CLI before it
parses an argument, and `ProgramTable.RunMain` for a run unit — which is where it used to live ALONE, so every
citation the compiler printed left the process at the OS code page (on cp437 the section sign is the single byte
`0x15`) in every redirected capture and every CI log. No golden could see it: the corpus runner matches `.err`
files on a pure-ASCII diagnostic CODE substring, and the harness decodes every capture as UTF-8. The witness is
therefore a BYTE assertion through a redirected stream, `CliDiagnosticEncodingTests`, which drives the CLI from a
console forced to cp437 on Windows and FAILS if that code page was not actually in force — a child with no console
inherits .NET's UTF-8 default and would pass on the broken build.

### 3.6 Build & driver seams the harness depends on
Two build-side changes this dimension REQUIRES (owned by the driver/emitter dimensions but gated here):
- **Real Bind phase boundary.** Extract the binder passes — reached today through the `CSharpEmitter.Bind` host
  facade — into a standalone `Binding.BindPipeline.Bind(tree, edition) → BoundCompilation`. Then: `CheckOnly`/`--check-batch` stop after Bind
  (fast verdict, no Emit); `ICompilerProbe.Probe(emit:false)` returns diagnostics from Bind only; the
  characterization emitter snapshot is a pure `Emit(BoundCompilation)`.
- **Cache the Roslyn reference set.** `RoslynBackend.ReferenceAssemblies()` →
  `static readonly Lazy<ImmutableArray<MetadataReference>>`. Single highest-leverage battery-throughput fix.

### 3.7 Guard consolidation (cross-platform, greenfield-first)
Target: the authoritative regression is the in-process `dotnet test` battery (runs on every OS), NOT a Linux bash
loop over frozen code. ⚠ **"over frozen code" was the whole objection and it no longer applies** (PB750): the
bash loop drives `cobol`. It stays a SECOND path — the shipped exe, out of process — deliberately, and the two
are reconciled by `corpus.tsv` + `CorpusManifestTests` + `guard-nist-audit.sh` rather than by one replacing the
other. Scripts collapse to:

- `scripts/guard.ps1` + `scripts/guard.sh` (thin, equivalent wrappers): `dotnet build -warnaserror` →
  `dotnet test Unit Conformance Characterization` → `cobol check-batch` continuity sweep → the CLI-level NIST
  leg. Cross-platform, ~one command.
- KEEP `scripts/version-continuity-sweep.sh` (the check-batch INV-1 sweep — it drives the greenfield CLI and is
  already fast/portable) but add a `.ps1` sibling.
- ⛔ **REVISED by kb/Work/PB750:** `guard-fast.sh`, `guard-run-group.sh`, `guard-verify.sh`, `guard-compiler.sh`,
  `guard-compile.sh`, `guard-verdict.sh` and `guard-nist-audit.sh` no longer "exist to parallelize the legacy
  NIST loop" — they ARE the CLI-level WiseOwl COBOL NIST leg, and they carry the verdict-evidence rules, the chain
  isolation model and the population/expectation audit. They SURVIVE G8; what is deleted there is
  `guard-compiler.sh`'s legacy arm (`COBOLSHARP_LEGACY_DIFFERENTIAL=1`) and `LEGACY_DIVERGENT`. DELETE at G8:
  `compliance.sh`, `nist-batch.sh` (ad-hoc dashboards duplicating the guard). `run-suite.sh` survives as a
  triage helper (it selects its compiler through `guard-compiler.sh`).
- Until G8, KEEP the legacy differential arm as the oracle-agreement check that the bake was faithful.

### 3.8 CI (target `build-and-test.yml`)
An OS matrix, greenfield-authoritative, with the characterization gate:

```yaml
strategy: { matrix: { os: [ubuntu-latest, windows-latest] } }
jobs:
  build-test:      # per-OS: build -warnaserror; dotnet test Unit + Conformance + Characterization --no-build
  version-sweep:   # ubuntu: cobol check-batch INV-1 (permissive continuity), fail on BREAKS
  nist-cli:        # ubuntu: guard-fast.sh — the CLI-level WiseOwl COBOL NIST leg (PB750). Survives G8.
  legacy-oracle:   # TEMPORARY (pre-G8 only): COBOLSHARP_LEGACY_DIFFERENTIAL=1 guard-fast.sh — proves the bake
                   # still matches the legacy oracle; the SWITCH (not the leg) is deleted at G8
```

Post-G8 the `legacy-oracle` job and the two legacy test projects are removed; `build-test` becomes the whole gate,
identical on both OSes (NIST now runs in-process, closing smell #5). `Generated/` continues to regenerate per
checkout (a failed regen fails the build — keep).

### 3.9 Roadmap SSOT + DEVLOG discipline
- **`docs/COBOLNET_REARCHITECTURE_PLAN.md`** (the master plan) — the resumable migration SSOT / ROADMAP: an ordered
  P0–P16 phase index, per-phase exit criteria (universal clause: "gates (1) & (2) green; gate (3) either unchanged or
  reviewed-re-baselined in this change set"), a top STATE banner naming the current phase, and the owner-decisions
  table. This is what a future engineer resumes the REARCH from; the plan §0 banner is the feature-drive live
  state and cross-links to it.
- **DEVLOG.md** — unchanged discipline (descending, real timestamp, one entry per commit) per the existing
  `feedback_devlog*` memories. Each rearch phase commit references its ROADMAP phase id.
- **`docs/DOC_INDEX.md`** — add rows for `COBOLNET_REARCHITECTURE_PLAN.md` (the migration SSOT / ROADMAP),
  `DIAGNOSTICS.md`, this doc, and the sibling `DESIGN-*` / `PHASE-*` docs; keep the "one canonical doc per subsystem" rule.

### 3.10 THE VERDICT-EVIDENCE INVARIANT (the instrument gate — plan §11 A12/A12b/A12c/A12d/A12e)

> **A missing observation is not a negative observation.** Every harness in this repo produces verdicts about
> the compiler. Each of them had the same defect, and each of them had it silently: an outcome the harness
> *failed to observe* was folded into a bucket that means *the compiler did something wrong*. This section is
> the design rule that closes the class, and every gate below implements it.

**The rule.** A verdict about the compiler is produced ONLY from an observation the harness actually made.

| verdict | the evidence it requires | with nothing else |
|---|---|---|
| ACCEPT / compiled | the process exited 0 **and** the artifact it claims to have produced exists | NO-VERDICT |
| REJECT / compile failed | the process exited non-zero **and** emitted at least one diagnostic line | NO-VERDICT |
| RUN, for MATCH / DIFF | the program **ran to completion** — not killed, not timed out, not failed to launch — and a non-zero exit carries a **diagnostic**, exactly as a rejection must | NO-VERDICT |
| COMPARE, for MATCH / DIFF | output that **exists and has bytes**, and a comparison that **completed**: the normalization ran to the end and `diff` reported *same* or *different*, never *could not tell* | NO-VERDICT |
| *(no line at all)* | — | ⛔ NO-VERDICT, and LOUD: a missing verdict is a failure, never a subtraction |

⚠ **All THREE arms, not two.** The compile and run arms were hardened in 2026-08-03's instrument wave; the
COMPARE arm was left with the original rule — every non-zero `diff` exit meant "not this file" — for thirteen
months. `kb/Work/PB473`: battery #43's only red, `IF141A`, produced a report **byte-identical to its golden**
and was scored `DIFF — REGRESSION!`. Two arms fixed and one left is this repository's most reproducible defect
shape; when a rule has arms, count them.

A NO-VERDICT is never MATCH and never REGRESSION. It is an explicit statement that the run learned nothing,
and it fails the gate as UNRESOLVED so it gets read rather than absorbed.

**Five corollaries, each earned by a defect.**

1. **Assert the POPULATION, not just the failure count.** A verdict computed from the results that *arrived*
   cannot see a program that produced none — losing one lowers MATCH and still passes. `guard-fast.sh` printed
   `=== ALL GREEN ===` at 352 MATCH against a 353 baseline exactly this way. Every iterating harness asserts
   that its results are a **partition of its declared population**: one verdict per member, no strays.
2. **Compare against a COMMITTED MANIFEST, never a remembered number.** "353 MATCH" was a fact in a document.
   The expected verdict of every NIST program is *derivable* — `tests/nist/corpus.tsv` status/golden columns
   crossed with the population — so `scripts/guard-nist-audit.sh` compares per-program and self-updates the
   moment a golden lands. It also cross-checks the manifest against what is on disk, so a golden that vanishes
   cannot quietly turn its program into an expected `NO BASELINE`.
3. **Prove the instrument can fail, and prove it fails for the right reason.** `--self-test` on the audit runs
   eleven synthetic runs each built to break exactly one check, and asserts each produces *its own named
   finding* — the first draft had two cases "passing" because an unrelated bug had already reddened the
   control. `gnucobol_differential.py` runs an **evidence control** at startup (one program that must be
   accepted, one that must be rejected *with a reason*) and refuses to score anything if this build cannot be
   told apart, because `has_evidence` would otherwise reclassify every genuine rejection as a lost result.
4. **⛔ THE POPULATION A CLAIM IS ABOUT MUST BE THE POPULATION THE INSTRUMENT OPENED — and two instruments
   answering about "the corpus" must not each define it** (kb/Work PB209). Corollary 1 makes a harness assert
   its OWN declared population; it says nothing about whether that declaration matches the corpus the claim is
   written about. The differential defined its 1,323 cases inline in a `ProcessPoolExecutor` worker, so every
   reachability sweep had to re-invent the same noun and re-invented it as `find … -name '*.cob'` — which
   returns **two files** over a tree whose programs are `AT_DATA` heredocs inside `.at` wrappers. Two waves
   therefore proved a shape absent from a corpus the gate then found it in, twice. The rule: **one executable
   definition of a population, called by every reader**, a per-population count PRINTED on every run so a
   contribution of two files cannot be reported as a corpus, and a drift test binding the readers together.
   And a population that cannot be measured is not a population of zero — `corpus_sweep.py` REFUSES to report
   hit counts when its population check fails, because the clean zero from a reader that opened nothing is
   indistinguishable from evidence of absence.
5. **⛔ THE COMPARISON IS AN OBSERVATION TOO — and the plumbing that carries it must not be able to lose data
   silently** (kb/Work PB473). Corollaries 1–4 are about the SUBJECT (did the program run, was the population
   whole). This one is about the INSTRUMENT'S OWN WIRING. The NIST guards compared with
   `diff <(normalize golden) <(normalize actual)`, so both sides arrived over a `/dev/fd/N` process
   substitution. A short delivery there is **indistinguishable from a difference**: `diff` prints nothing on
   stderr and reports *different* over input that is identical. Measured: the compare alone, hammered 3000×
   at `-P32` with nothing else running, never failed; the same loop under the battery's concurrent `dotnet`
   load failed **1 in 640**, and battery #43 lost that coin flip at 1 in 376. The rule: **a verdict-bearing
   comparison reads REAL FILES whose writes have completed**, checks the normalization's own exit status, and
   distinguishes the comparator's *"different"* (exit 1) from its *"could not tell"* (anything else). Detecting
   the loss is not enough — remove the mechanism that can lose it, and keep a drift test so it cannot come back
   (`guard-verify.sh` fails on any `diff <(…)` / `comm … <(…)` in a verdict path). The same reasoning retired
   the last one in `guard-fast.sh`'s lost-result computation, where a short read would have shrunk the set of
   programs to re-observe.

**Where it is implemented.**

| site | what it does now |
|---|---|
| `tests/_shared/ProcessObservation.cs` | **THE one child-process observer.** Replaced six copies of "start `dotnet`, wait N s, return whatever came back" (`CutRunner.RunExit`, `AcceptDifferentialTests.AcceptRun`, `CobolNetTestBase.CompileAndRun`, three in `EndToEndTestBase`) plus a seventh found by its own drift guard (`BinderDecompositionTests`, which read both streams synchronously and then read `ExitCode` without checking `WaitForExit`'s result). A run that does not complete raises `HarnessNonObservationException` — it never returns partial output for a caller to compare. Retries **once, serialized**, first: that is re-attempting a measurement that did not complete, not re-rolling a failed assertion. Budget `COBOLNET_RUN_TIMEOUT_MS` (default 120 s); every retry and non-observation is appended to `COBOLNET_HARNESS_LOG` so the rate is measurable. |
| `ProcessObservationDriftTests` | Keeps the extraction collapsed (the `TestRepoDriftTests` pattern): no test source may start a process under its own bounded wait. Plus five behavioural facts, including "a process that never finishes RAISES instead of returning empty output" and "`Observe` reports a timeout with an **empty** stdout" — if that ever returns content, the defect is back. |
| `scripts/guard-nist-audit.sh` | The population + manifest + expectation audit, consumed by **both** guards so the rule is written once. The EXPECTED verdict now depends on WHICH compiler ran — the `divergent` rows expect `LEGACY DIVERGENT` from the oracle and `MATCH` from `cobol` (PB750) — and an unknown compiler name is refused rather than silently audited against a default's expectations. `--self-test` proves all sixteen checks can fail, including the four compiler-identity arms. |
| `scripts/guard-compiler.sh` | ⭐ **THE one answer to "which compiler is this gate measuring?"** (kb/Work/PB750). `cobol` by default, the legacy oracle only under `COBOLSHARP_LEGACY_DIFFERENTIAL=1`; `guard_assert_compiler_identity` reads the resolved CLI's own `.deps.json` and REFUSES to run when the closure does not match — `Cobol.Net.Compiler` present for `cobol`, absent for `legacy`. Earned by both guards hard-coding `cobolsharp.dll`, so `guard NIST: 353 MATCH` measured the ORACLE for the whole rearchitecture and battery #58's NC215A wrong answer was invisible to it. `--self-test` (run by `guard-verify.sh`, hence by battery phase 2a) proves both directions of the refusal fire. |
| `scripts/guard-compile.sh` | The compile invocation for one NIST program, written once for both compilers (`cobol` needs `--nist NAME` because its parser binds the next token; the legacy takes a bare `--nist`). Called by the serial guard, the parallel compile, the serial re-observation retry and `run-suite.sh` — four call sites that would otherwise each have had to be kept in step by hand. |
| `scripts/guard-verdict.sh` | ⭐ **THE evidence rules for the NIST guards, written ONCE and sourced by both** (`feedback_one_rule_one_place`). `guard_compile_verdict` (compile arm), `guard_output_verdict` (run + compare arms: normalization, candidate resolution, the FAIL*/footer rules, the verdict), `guard_preserve` (keep a non-MATCH's evidence). It reports through `GUARD_VERDICT` / `GUARD_CLASS` (`match` · `regression` · `no-verdict`) so each caller keeps its own recording and counting, and every function is option-local (`local -`) and returns 0: a scoring routine that can abort `guard.sh`'s `set -e` is not a scoring routine. **The comparison materializes both normalized sides into real files** — corollary 5 — and reads `diff`'s exit status explicitly. |
| `scripts/guard-baselines.sh` | ⭐ **THE baseline-cleanliness audit, written ONCE and called by both guards** (`feedback_one_rule_one_place`). Empty / `FAIL*` / non-zero-footer baselines are refused, unless the program is declared `CCVS-DEFECT` in `tests/nist/corpus.tsv` — the same declaration, read from the same file, that `CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects` states as a set equality. The complement is audited here too (a declaration with nothing to describe is a defect). Exits with the FAILURE COUNT, so each caller keeps its own aggregation. |
| `scripts/guard-run-group.sh`, `scripts/guard.sh` | The two callers: same rules, different plumbing (a per-group `mktemp` dir vs. the run-scoped `$GUARD_WORK`, `echo` vs. the recording `v()`). They used to carry a COPY of the rules each, "kept character-for-character in step" by prose — which had already drifted (one `normalize()` had a `[ -f ]` guard, the other did not) and which kept both copies of the compare-arm hole. Compile diagnostics are captured (`<TEST>.compile.log` + `.compile.rc`) instead of `/dev/null`; the run is bounded by `timeout` and its exit status kept instead of `\|\| true`; **any non-MATCH's report, streams and both normalized sides are copied into a run-scoped forensic directory** before the group's dir dies with it (attributing battery #43 cost hours because that directory was already gone). |
| `scripts/guard-fast.sh` | Group-runner stderr captured instead of discarded (a group could die and take its verdicts with it in silence); the audit gates `ALL GREEN`. ⛔ **FULL FAN-OUT IS KEPT AND THE LOST OBSERVATIONS ARE RE-TAKEN INSTEAD** — capping `-P` would pay for the damage on every run to protect against something the evidence rules now DETECT. Contention can no longer corrupt a verdict, only lose one, so step 3b re-runs just the affected groups serially. See this table's grouping row below. |
| `scripts/guard-fast.sh` grouping | ⭐ **Isolation now comes from the DECLARED chain graph** (`corpus.tsv` `chain-preds`, as connected components: 332 groups over 376 programs) instead of a hand-written "these six suites run serially" list. Longest serial group **40 → 9**. Justified by evidence, not guessed: `NistDifferentialTests` already runs all 349 programs in per-program directories with only their declared predecessors and is green. Isolation is strictly SAFER than ordering — guard.sh's prose anti-dependencies ("no other TF022 writer between them") exist only because it shares ONE directory, and per-component dirs make them unstateable. It also corrected the hand list, which over-grouped `SQ204A` (that program `OPEN OUTPUT`s its own file). ⚠ **AND IT DID NOT MAKE THE LEG FASTER — say so.** Measured on a 32-core Windows box: NIST phase **564 s before, 598 s after**. The leg is THROUGHPUT-bound on `dotnet` cold-start (~150 s of the total is the compile phase alone, and effective concurrency was observed at ~7, not 32), not TAIL-bound, so shortening the longest serial group from 40 to 9 buys nothing here. It should matter on Linux CI where process spawn is far cheaper. **The real lever is a persistent run-host to amortize cold-start** — the change is kept for correctness and for retiring a hand-maintained list, NOT for speed. |
| `scripts/guard-verify.sh` | Two checks now, in order. **(1) The evidence-rule witnesses** — 21 synthetic runs through the REAL `guard-run-group.sh` over a fake repo root with `dotnet`/`diff` shims on `PATH`, one rule per case, seconds and no corpus: absent report → NO-VERDICT, 0-byte report → NO-VERDICT, `diff` forced to exit 2 → NO-VERDICT, `rc≠0` with empty stderr → NO-VERDICT, **a genuinely wrong report → REGRESSION** and `rc≠0` *with* a reason → REGRESSION (the discriminators, without which the fix could turn every regression into a NO-VERDICT and look green), plus the compile arm, the FAIL*/footer rules, the forensics and the two structural drift checks. Six were proved RED against the pre-fix runner first; re-run them there with `GUARD_GROUP_RUNNER=<old copy>`. `--witnesses` runs only these — the wave-local gate for a change to the rules, and `scripts/battery.sh` **phase 2a** runs it before phase 2 so every comprehensive run proves the instrument before believing its output. **(2) The equivalence proof**, skipped when (1) fails: two guards agreeing about a rule that is WRONG is not evidence, which is exactly the state that let PB473 stand. Its verdict filter had also silently omitted `LEGACY DIVERGENT`, dropping 11 programs from **both** sides; the vocabulary is complete (`COMPARE NO-VERDICT` included) and any verdict-shaped line it does not recognize is reported rather than discarded. |
| `scripts/gnucobol_differential.py` | A rejection needs a non-zero exit **and** a diagnostic; an acceptance needs the artifact. Evidence-free compiles are retried once and then bucketed `NO_COMPILER_EVIDENCE`, which counts as a harness failure and is **named for re-run**, never folded into a divergence bucket. Its population is no longer its own: it filters with `gnucobol_extract.differential_cases()` before dispatch, so `len(payload)` IS `cases run` and the corpus has one definition. |
| `scripts/gnucobol_extract.py` | **THE external-corpus population, as an API** — `primary_source` (the one "is this a case" predicate, returning the `(member, compile-check)` pair the caller actually needs so nothing re-derives half of it), `differential_cases` (the 1,323), `iter_programs` (the 1,611 COBOL members, what a sweep must read). Both readers call it; there is no second place to define the corpus. |
| `scripts/corpus_sweep.py` + `ExternalCorpusPopulationDriftTests` | The reachability instrument and its lock. The sweep prints a per-population census on EVERY run and refuses to report hit counts when its population check fails. The drift test asserts the sweep's live external population equals the differential's COMMITTED per-case baseline — two independently produced numbers, so agreement is evidence. It was **proved failing first**, driven against the old `*.cob` reader, where it reported `{"external": 2, "baseline": 1323, "state": "drift"}`; `TheDriftCheck_ActuallyFails_WhenTheExtractionIsEmptied` keeps that red permanently reachable. A missing interpreter or an absent corpus is a LOUD failure, never a skip. |
| `scripts/fetch-gnucobol-tests.ps1` | **THE fetch the population gate depends on, and a gate in its own right.** It broke all three of its own contracts at once (`kb/Work/PB897`): it `Remove-Item`d `tests/external/gnucobol` BEFORE an extraction that then failed, it passed the archive as a drive-letter path that GNU tar reads as `host:path` (`Cannot connect to E: resolve failed`, exit 128) while bsdtar accepts it, and Git-for-Windows’ GNU tar cannot read a `.tar.xz` at all (it execs an `xz` it does not ship). Which `tar` was first on PATH therefore decided whether the corpus existed — so the two `ExternalCorpusPopulationDriftTests` reds became permanent and every implementer brief told agents to ignore them, which is a gate that has stopped gating. Now: extraction lands in a STAGING directory and is swapped in only after tar succeeded **and** the payload was verified (nothing is ever deleted before its replacement exists); the archive is named RELATIVE to its own directory, which neither tar misreads (⛔ `--force-local` is the wrong repair — bsdtar rejects it outright); and the tar is chosen by ATTEMPTING the work down an ordered candidate list rather than by trusting `Get-Command`. Every failure prints one `FETCH FAILED: <cause>` line and exits non-zero, and `build-local.{ps1,sh}` both surface it: the gate goes RED with `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED` on the verdict line. `-SelfTest` drives the REAL `Invoke-CorpusExtraction` over a synthetic `.tar.xz` — including the failure branches — and `ExternalCorpusFetchTests` runs it; it was proved failing first against the old shape (4 of 8 checks red under GNU tar, and 4 of 8 under bsdtar too, since the delete-first fault is tar-independent). |

**What this does not claim.** The invariant makes a false GREEN and a false RED *visible*; it does not by itself
prove the battery is deterministic. That is the measurement A12/A12d asks for, and it is recorded in plan §11
beside the row, not here.

### 3.11 TEST-COLLECTION PARALLELISM — the partitioned-theory mechanism (plan §11 A13)

> **A test CLASS is a scheduling unit, not just a namespace.** xUnit 2.9.2 parallelizes at TEST-COLLECTION
> granularity and by default **each test class is one collection**, so every test in a class — including every
> row of a `[Theory]` — runs SERIALLY ON ONE THREAD. A fat class caps an assembly's whole wall clock, and
> nothing in the normal output says so: `dotnet test` reports totals and a duration, never concurrency.

**The instrument.** `scripts/profile-test-parallelism.py <run.trx>` reads the trx `scripts/battery.sh` already
emits and prints, per class, `tests / sum(s) / span(s)` plus the assembly's average concurrency. Read the two time
columns together: `sum ≈ span` means the class ran serially and is a splitting candidate; `span << sum` means it
was already spread across threads.

**The mechanism** — `tests/_shared/TestPartitioning.cs`, linked into every project under `tests/`.

xUnit v2 offers exactly two levers and only one of them can split: `[Collection]` MERGES classes into a shared
collection, so it can never divide one. That leaves genuine class splits — and the naive class split duplicates
the test bodies, which is how a split rots. So the tests live ONCE in an abstract generic base and each partition
is one line:

```csharp
public abstract class FamilyTestsBase<TSlot> where TSlot : ITestPartitionSlot
{
    public const int Partitions = 12;                     // read by the drift audit

    [PartitionedRowSource(nameof(Rows))]                  // the UNPARTITIONED set
    public static IEnumerable<object[]> AllRows() => …;

    public static IEnumerable<object[]> Rows() => TestPartitioning.SliceRows<TSlot>(AllRows(), Partitions);

    [Theory][MemberData(nameof(Rows))] public void TheTest(…) { … }   // written once
}

public sealed class FamilyTests_P0 : FamilyTestsBase<Slot0>;          // one line per collection
```

Row `i` belongs to slot `i % Partitions` — a STRIDE, not a contiguous block, because theory rows are ordered by
construct or program name and adjacent rows have correlated cost; a block would concentrate the expensive rows in
one partition.

⭐ **Why it works:** static members of a CLOSED generic type are per-type-argument, and xUnit resolves
`[MemberData]` against `testMethod.DeclaringType`, which for a method inherited from `FamilyTestsBase<Slot3>` is
the *closed* type, not the open definition. That was PROVED with a standalone probe on xunit 2.9.2 /
xunit.runner.visualstudio 2.8.2 before any real class was touched — 3 partitions over 9 rows produced 9 tests, 3
per partition, each asserting its own slot; open-type resolution would have produced 27 tests and 18 failures.

**⛔ The invariant, and its gate.** A partitioned family FAILS OPEN: delete one partition class and the rows it
owned are simply never run — no error, no red, just a smaller and entirely plausible test count.
`TestPartitionAudit`, run per assembly by `TestPartitionCoverageDriftTests`, closes that. It is SHAPE-DRIVEN, not
registered — it finds every family by structure (a top-level, author-written, abstract generic base with one type
parameter constrained to `ITestPartitionSlot`), so a NEW family is covered the moment it is written. Four checks,
one per way a family loses rows silently:

| check | what it catches |
|---|---|
| the row source yields NOTHING | every check below would compare an empty union against an empty source and report green |
| slot indices ≠ {0 … `Partitions`−1} | a deleted, duplicated or mis-slotted partition class |
| an EMPTY partition over a non-empty source | more partitions declared than the source can fill — the one waste the union check cannot see, because the surviving slots still cover the whole set |
| union of the partitions ≠ the source as a MULTISET | rows dropped or double-run, each named in the failure |

Plus a ladder check: `Slot9.Index => 8` would silently put two classes in one partition and corrupt EVERY family
at once, so the slot ladder is verified against its own names, once, centrally.

⚠ **The gate was proved failing, not trusted silent** — and it earned that on its FIRST real run, before any
deliberate break: Roslyn emits a nested `<>O` delegate-cache class inside any generic type that caches a lambda,
and a nested type inherits its enclosing type's generic parameter *with its constraints*, so every family produced
a phantom family with no `Partitions` const. Both structural checks were then fired deliberately (a partition
class commented out → the slot-set violation plus a per-source "87 row(s) NEVER RUN" naming the exact dropped
rows; a slice count desynced from the const → `TestPartitioning.Slice`'s own range guard throws first, which is
why that direction cannot reach the audit at all).

#### 3.11.1 What is partitioned, and what it measured

| family | `Partitions` | before (battery #41 trx) | after |
|---|---|---|---|
| `VersionMatrixTests_P0 … _P11` | 12 | 2127 rows, **720.5 s SERIAL** — the whole 721 s leg | 12 × (175…179 rows), 552–601 s each, all spanning the run together |
| `NistDifferentialTests_P0 … _P5` | 6 | 349 rows, 237.9 s SERIAL | 6 × (58–59 rows), 329–354 s each |
| `CorpusRunnerTests_P0 … _P2` | 3 | 1005 rows, 83.9 s SERIAL | 3 × (332–334 rows), 117–145 s each |
| `StorageFormNistEquivalenceTests_P0 … _P7` | 8 | one `[Fact]` looping the corpus, **171.5 s** — *was* the Unit leg's wall clock | 8 slice-`[Fact]`s, 81–138 s each |

Whole-corpus assertions stay OFF the partitioned base and run ONCE (`VersionMatrixTests`' two catalogue facts,
`CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap`) — an inherited `[Fact]` would run N times and inflate
the count N-fold. The StorageForm sweep is the one deliberate count change (6 → 13) and it keeps its POPULATION
assertion: the whole-corpus bar was `parsed >= 50`, and each partition now asserts its proportional share rounded
UP, so a partition that silently stopped binding goes RED instead of green-and-empty (§3.10 corollary 1).

**Measured, same box (24-physical/32-logical i9-13900K), same `Conformance ∥ Unit ∥ Characterization` shape:**

| leg | wall | sum of test time | avg concurrency |
|---|---|---|---|
| Conformance | 721 s → **600 s** | 1948 s → 10469 s | 2.7× → **17.4×** |
| Unit | 171 s → **138 s** | 329 s → 895 s | 1.9× → **6.5×** |

⛔ **THE TAIL IS GONE; THE WALL BARELY MOVED — AND THE SECOND HALF OF THAT SENTENCE IS THE FINDING.** A13
predicted ~783 s → ~80 s. The split did precisely what it was designed to do, yet bought 17%, because
`sum-of-test-time ÷ wall` measures COLLECTION concurrency, **not core utilisation**, and the "idle" cores were
never idle: one WiseOwl COBOL compile is internally parallel (Roslyn `Emit`) and a NIST row spawns a `dotnet` child
that is too. The proof is in the after-profile itself — the SAME work reports **5.4× more test time** because the
new collections found contention, not silicon; at 17.4× + 6.5× ≈ 24 threads on 24 physical cores the box is
saturated. **The class-split lever is therefore EXHAUSTED**, and the remaining lever is to reduce the WORK — the
persistent compile/run host of §3.10's `guard-fast` row and A13(c), not more parallelism. Kept for the balance
and for the drift gate, exactly as the `guard-fast` regrouping was kept for correctness rather than speed. ⚠ The
contention was since NAMED (§3.14.5): for check-only compiles it is one lock in the ANTLR lexer's ATN simulation,
reached on every token because predicated rules keep its DFA from caching (the start state, and every edge through a
predicate),
so the continuity partitions ran at ~3.8 busy cores, not a saturated host; the partition counts are re-measured
once M6 removes it.

#### 3.11.2 ⛔ Filters must key on the METHOD, never on `Class.Method`

A partition class is `VersionMatrixTests_P0`, so `FullyQualifiedName~VersionMatrixTests` still selects it and
`!~VersionMatrixTests` still excludes it — but `~VersionMatrixTests.Cobol85Program_…` can never match
`VersionMatrixTests_P0.Cobol85Program_…`, and **vstest answers a filter that matches nothing with a SILENT
GREEN**. Two CI filters were keyed that way: the three continuity shards, and the **INV-1-STRONG job**
(`~NistDifferentialTests.NistProgram_MatchesGolden`), which unlike the shard matrix has **no population guard**
and would have kept reporting success over ZERO goldens. Both now key on the method name alone — unique in the
tree, and immune to the next re-partition. The shard-population job (§3.8) is what catches this class of error for
the sharded leg, and it was re-run locally against the split tree: 349+349+349+1080+1354+1761 = 5242 = discovered.

### 3.12 THE COMPILED-PROGRAM CACHE — reducing the WORK of a re-gate (kb/Work PB985)

§3.11 ended with the parallelism lever exhausted and "reduce the WORK" as the one left. The largest repeated work
is the lander's whole-Conformance re-gate after a TEST-ONLY fix (a probe template, a stale test-ref, a pinned
expectation — the dominant cause of trains 46–48's first-gate reds): ~7,900 cases recompiled although no compiler
or runtime bit changed. `tests/Cobol.Net.Tests.Conformance/CompiledProgramCache.cs` makes that re-gate reuse every
compiled program. **A hit still RUNS the program** — runtime behaviour is what a case checks; only the compile is
skipped.

**Where a case's time goes (measured first, PB985 obligation 1; Debug build, serial, a loaded host).** A sampled
corpus case: front end + bind ≈ 60 ms, emit + Roslyn + packaging ≈ 40 ms, the program RUN (a `dotnet` child
process) ≈ 176 ms. A sampled NIST case: front end + bind ≈ 634 ms, emit + Roslyn ≈ 98 ms, run ≈ 203 ms. So
**Roslyn emit is NOT the dominant cost — the COBOL front end is**, and the cache is sized to skip the WHOLE compile
(front end, bind, emit), not only the backend. The run is irreducible; it bounds what a cache can recover.

**The key is complete, by construction and by test.** A cache that omits one compiler input returns a stale
program — a green verdict about code that no longer exists — so the key is everything the output depends on:

| axis | how it is keyed | proof (`CompiledProgramCacheDriftTests`) |
|---|---|---|
| the compiler's own bits | SHA-256 of every non-framework assembly in `Cobol.Net.Compiler`'s reference closure (compiler, front end, editions, runtime, Roslyn, ANTLR) + the exact shared-framework build + culture + working directory | a byte flipped in one closure assembly changes the fingerprint; every loaded `Cobol.Net.*` product assembly is in the closure |
| every compile option | **reflection** over `CompilerDriver.Options`, rendered per TYPE; an unknown type THROWS | every property is flipped by type-derived value and must change the key — an option added tomorrow is covered with no edit |
| the source | its full path and its content hash | an edit misses; the same text at another path misses |
| copybooks, probes, environment | the compiler's OWN record, `CompilerDriver.Result.Inputs` (below), re-verified on every lookup | an edited copybook misses; a copybook that newly SHADOWS the one found misses; a `>>DEFINE … PARAMETER` variable set/changed misses |
| the clock | never keyed — a compilation that read it (WHEN-COMPILED) is **never stored** | the WHEN-COMPILED program misses every time; a program without it never reads the clock |
| the output DIRECTORY | deliberately NOT keyed (only the output file name) | the same programs compiled cold into two directories agree byte-for-byte in every output file, diagnostic and warning |

**`CompilationInputs` — the compiler states its own inputs.** The one structural change to the product: every
AMBIENT read a compilation makes — the source and copybook reads, every copybook-search probe (including the ones
that found nothing — an absent answer is an input), the NIST default-library probe, a directive's environment
variable, the WHEN-COMPILED clock — goes through `src/Cobol.Net.Frontend/Pipeline/CompilationInputs.cs`, which
performs the read AND records it; `CompilerDriver.Result.Inputs` carries the record and `Result.OutputFiles` every
file the compile wrote (the backend and the packager report their own writes). That is the dependency list any
incremental build needs (the `gcc -MD` shape), not a test-only affordance; the cache re-derives nothing. The
WHEN-COMPILED stamp is captured LAZILY (still once per compilation), so only a compilation that renders it depends on
the clock. Two source-form drift tests keep "complete" true: an ambient-read API anywhere in
`Cobol.Net.Compiler`/`Frontend`/`Editions` outside `CompilationInputs.cs` is red unless its line says why it is not
a compilation input (the compiler's own deployment, its own output), and the compiler may expose no public mutable
static. A third keeps the SCOPE true: the one direct `CompilerDriver.Compile` in the Conformance assembly is
`CompiledProgramCache.CompileCold`.

**What is not keyed, and why that is safe.** Roslyn compiles a program against the host's trusted-platform set,
which in a test host includes the TEST assemblies — deliberately unkeyed, or a test-only fix could never hit. The
only way one could shape the output is by the program BINDING to it, which would appear as an assembly reference;
the cache reads the produced assembly's reference table and refuses to store a program that references anything
outside the keyed closure or the framework. A backend failure is never stored. Runtime-library code the compiler
calls at compile time is covered by the closure hash (its bits) — its own ambient reads are run-time behaviour by
construction and are not scanned; a compile-time call into one would be a defect the ambient scan does not see.

**Stable source paths.** A harness that wrote its program into a fresh random directory had a different
`SourcePath` — a different key — every run. `CompiledProgramCache.StageSource(plannedPath, text)` places the text
at a CONTENT-addressed path (`<store>/src/<hash>/<name>`), alone in its directory, so the same text compiles to the
same key every run; the run directory stays per-case. With the cache OFF it writes to `plannedPath` exactly as
before, so a cold run keeps the historical layout.

**Store, concurrency, eviction.** Default store `.cache/compiled-programs/` under the worktree root (git-ignored):
parallel worktrees never share one, and a removed worktree takes its store with it (`COBOLNET_COMPILE_CACHE_DIR`
relocates it). Entries (`entries/<key>.json`) name content-addressed, Brotli-compressed blobs (`blobs/<sha>`) — the
1.8 MB runtime every program deploys is stored once. Every write is temp-file-then-rename, so parallel test
collections and parallel test hosts only ever see whole files; a torn or evicted entry is a MISS, never an error —
correctness never depends on the cache. Once per test process, under an exclusive lock file, a store over its cap
(`COBOLNET_COMPILE_CACHE_MAX_MB`, default 4096) drops its least-recently-used half and every blob no survivor
names (blobs younger than ten minutes are spared for a concurrent writer), and staged sources untouched for a week
go. Each process appends its hit/miss tally to `stats.log`.

**The switch.** `COBOLNET_COMPILE_CACHE=off` disables it, `=on` forces it. Unset, it is ON for local gates
(`build-local`, the lander) and OFF under CI (`CI` set — GitHub Actions always sets it); `scripts/battery.sh`
exports `off`, so **the battery and CI run COLD** (PB985 obligation 4 — the owner may decide otherwise).

**Measured (PB985 obligation 5)** — the whole Conformance assembly, 7,951 cases, three back-to-back runs on one
unchanged tree at `BelowNormal` priority on the shared 32-core host (2026-09-22): **cold, cache off: 22 m 53 s**;
**first gate, cache on and empty: 21 m 31 s** (12,886 compilations stored — the store's overhead is inside the
noise); **re-gate, warm, no product change: 1 m 27 s** (13,133 hits, 27 misses — the 6 WHEN-COMPILED programs and
the few harnesses whose sources live in a per-case random directory beside their copybooks — all 7,951 green in
all three). The compile, not the run, was the wall-clock pole under full parallel load: the populate run's
compilations summed to 25,217 thread-seconds (~2 s each under contention, against ~0.1 s serial), while the
program runs, being separate processes, spread across the cores. The whole store was 49 MB.

**⛔ A Debug build must not depend on the commit it was built at.** The three runs above shared one HEAD. The
lander's real re-gate COMMITS the test-only fix first, and the SDK then stamped the new commit into every
assembly (`AssemblyInformationalVersion` "1.0.0+&lt;sha&gt;", and the Source Link map whose PDB checksum the
assembly embeds) — every compiler bit changed, every case missed, and every incremental build recompiled every
project. `Directory.Build.props` therefore sets `EnableSourceControlManagerQueries=false` (and drops the revision
suffix) for **Debug only**; Release, the shipped build, keeps its stamp. `ADebugBuild_DoesNotStampTheCommit`
pins it on the built bits. Re-measured on the real flow — populate at one commit (14 m 0 s, a quieter host), then
a TEST-ONLY commit (that very test), rebuild, whole assembly: **build 5 s + 1 m 18 s, 13,133 hits / 27 misses,
7,952 green.**

### 3.13 THE PER-TEST IMPACT MAP — ORDERING the implementer's gate (kb/Work PB1683, PB1708)

**The defect it answers.** An implementer's gate ran the tests its author NAMED (its goldens, `~Drift|~EditionGate`,
`~CorpusRunner|~Nist` on a "shared seam"). Train 68b dropped two groups on whole-assembly reds those names never
reached — `DiagnosticPositionTests` after a COPY library-search change, and a `CONSTANT AS NULL` crash in
`DataBinder.Constants.cs` — and wave 69 (four agents, 1.17 M tokens) existed only to finish them. The map records
the fact the names were guessing: which tests execute which code.

⛔ **The map ORDERS the gate; it never SELECTS it** (owner, 2026-09-28, kb/Work PB1708). Every implementer gate runs
the whole discovered population (§3.14); the map only decides which tests run FIRST, so a red surfaces in minutes.
A dependency the map cannot see therefore costs a later red, never a missed one — and the single-process,
memoizing compiler hides dependencies in more ways than one recorder can close (values memoized into static
collections, edition state cached in the front end, static FIELD reads — PB1712).

**Collection — a per-test hit recorder, chosen by measurement.** Per-test coverage from a coverage tool cannot
attribute hits under xunit's parallel collections, and per-shard coverage bisected to tests multiplies the run.
The recorder instead rides the test run itself, in its own RECORDING build, and costs one cold Conformance run:

| piece | what it does |
|---|---|
| `tools/impact/ImpactRecording.targets` | imported ONLY through `-p:CustomAfterMicrosoftCommonTargets=…` (nothing in the tree imports it, so no gated, battery or shipped build carries a probe); compiles `ImpactProbe.cs` into the greenfield compiler, front end, editions, runtime and CLI, the legacy oracle the differential harness loads, and the three test assemblies; compiles `ImpactTestFramework.cs` into the test assemblies and names it in `[assembly: Xunit.TestFramework]` |
| `tools/impact/ImpactInstrumenter` (Mono.Cecil) | rewrites each covered assembly once: every method with sequence points calls `ImpactProbe.Hit(id)` on entry, `id` being its entry in the probe table — its file and the LINE RANGES it owns (its sequence points, merged across the lines between two of them unless another method's point lies between, so braces, `else` lines and comments belong to it and a lambda's body to the lambda); members with no sequence points are charged to a TYPE-LEVEL entry; `implies` names each entry's type-chain static constructors; every `Process.Start` is redirected to the probe's twin |
| `ImpactProbe` | one byte store per hit into the array of the running CONTEXT, an `AsyncLocal` every assembly's copy shares through `AppContext` data; a hit with no context lands in an AMBIENT array. In a child process (a compiled COBOL program loading the instrumented runtime) it records for the process and writes a hits file at exit, which the redirected `Process.Start` queued on the starting test |
| `ImpactTestFramework` | xunit's own framework with the message bus wrapped: `ITestCollectionStarting` / `ITestClassStarting` / `ITestStarting` install a fresh context synchronously on the flow about to run it (the same `TestRunner.RunAsync` then awaits the test, so the context flows into constructor, body and every task it starts, while parallel tests keep their own); `…Finished` saves it with the children folded in. A `BeforeAfterTestAttribute` cannot do this — it is handed only the `MethodInfo`, which cannot tell a theory's rows apart, and the corpus theories are ~3,400 of the ~9,300 Conformance tests. It DERIVES from the gate's `GateTestFramework` (§3.14.3), so a recording build still names one framework |
| `scripts/spec/record_impact_map.py` | a DETACHED worktree at the commit, the recording build, the instrumenter, each test assembly once with `COBOLNET_COMPILE_CACHE=off` (a cache hit skips the compiler and would record nothing for it), and the merge into `<git common dir>/cobol-impact/<sha>.json.gz` — shared by every worktree of the repository. Its watchdog fails the recording when a listed test was never recorded or no test reached the compiler, front end or runtime. Recorded ON DEMAND, never per commit (owner, 2026-09-28, kb/Work PB1709) |
| `scripts/spec/impacted_tests.py` | the lookup: `analyse()` → the change's TIERS (below) and its tier-0a facts (the test methods it adds, the corpus goldens it touches), which `scripts/gate_plan.py` computes in-process to assemble the plan (§3.14.2); `--base <cut point> --plan <file>` writes the same analysis as JSON for inspection. M11 deleted its selection (kb/Work PB1717): its last stdout line is ALWAYS the whole-assembly filter `FullyQualifiedName~.`, map or no map, until M13 deletes that line and `--plus` with every caller (§3.14.9) |

**Measured cost.** At `dbea12428`, `BelowNormal` on the shared 32-core host: 35,578 probe entries; Conformance
9,274 green in 15 m 19 s (cold, instrumented), Unit 29,693 green in 2 m 38 s, Characterization 33 in 2 s; build
23 s, instrumentation 4 s; **18.7 min in all**. The map is ~5 MB compressed.

**What the lookup computes: a TIER per test.** A change is analysed exactly as PB1683 built it: a hunk inside a
method's owned lines reaches the tests that executed that method; a comment-or-blank hunk reaches nothing; an ADDED
ordinary method reaches the tests that reached any same-named method of the file's types (overload capture); a
DECLARATION change (an override, virtual, operator, conversion, extension method, field, property, constant, a type
with a base list or an attribute, or any removed or changed line no method owns) reaches every test that executed
the file plus every test that executed a file naming one of its outermost types; a static constructor's change
reaches every test that touched its type. A test's reach is its own recorded context plus its class's and its
collection's (constructors, fixtures). The test assemblies are recorded too, so an edited test body or theory harness
reaches exactly the rows that executed it. From that:

| tier | the tests | runs |
|---|---|---|
| 1 | executed a changed method body (the direct hits) | first, cheapest first |
| 2 | reached only through a widening — a static constructor, a declaration change, the file level | next, cheapest first |
| 3 | reached by nothing the change touched | last |

What used to force the WHOLE-assembly filter — no map for the base, a map older than the base, a `.g4`, project,
build or source-generator input, a NEW src file, non-C# data under `src/`, a method reached only outside every
test's context — now puts EVERY test in tier 1, and the gate still runs all of them. A stale map is harmless for
the same reason: its line numbers can misplace a test by a tier, never drop it. So a map older than the base by
anything but documentation is still USED — for its recorded durations and its test names (tier 0u, §3.14.2) — while
every test is tier 1. `ImpactedTestsDriftTests` (Unit) drives every arm of both `impacted_tests.py --self-test` and
`gate_plan.py --self-test`, proves through the real command line that a mapped change is tiered while the filter
line stays the whole assembly, pins the partition-suffix contract `NameKey` relies on (§3.14.2), and requires every
product project under `src/` to be in both the targets file and the recorder's `PROBED` list.

**What the replays measured — for this compiler the map mostly says "everything", so COST orders inside a tier.**
Every golden runs the whole pipeline. Over the 20 replayed clusters of trains 65–69, method granularity reaches
5,504–9,268 of 9,274 tests, and even a perfectly precise registry and declaration analysis leaves a median of 6,404
(evidence `impact-map-pb1708/a5`, `a6`). On the one dropped branch with named reds, 68b X, 9,144 of 9,190 tests are
tier 1 (`b4`). Ranking tier 1 by SPECIFICITY (the sum of `ln(N / df)` over the changed entries a test executed) put
X's first red after **62 %** of the assembly's recorded test-seconds — worse than the plain class-and-name order
(14 %) — because the change that broke them (the COPY library search) is executed by every test. Ranking by
recorded COST, cheapest first, put it after **0.32 %** (40 of 12,408 test-seconds). So inside a tier the order is
cheapest first, and the map's contribution is the tier boundary. Replayed through the order plan itself
(`gate_plan.py`, `b4`'s row P), both of X's reds are in leg 1 and the first runs after 0.32 % of the work, with leg 1
holding 2,829 cases (248 test-seconds, a 15.0 s floor). One replayed branch is a small sample; M13's
acceptance (§3.14.9) records the time to the first red of every implementer gate of the next train.

### 3.14 THE ORDERED WHOLE-POPULATION GATE, WHOLE-SUITE SPEED AND THE GATE CAP (kb/Work PB1708)

#### 3.14.1 The contract

- **Every implementer gate runs the WHOLE discovered population** of Conformance, Unit and Characterization, every
  time, in two LEGS: the likely-red tests first, everything else second. Ordering changes WHEN a test runs, never
  WHETHER.
- ⛔ **Sound by construction: every discovered test case runs exactly once across the legs.** A case's leg is a TOTAL
  function of its display name and one plan file (§3.14.2), so the legs cannot overlap and cannot leave a case out;
  the gate CHECKS it on every run (§3.14.4) and a drift test proves the check fires.
- ⛔ **Only the gate driver can make a test host filter.** The leg filter acts only on a complete, self-consistent
  environment the driver writes; any partial or stale one turns the run RED (§3.14.3), every other caller scrubs it,
  and every whole-assembly run — the battery and CI as well as the gate — asserts its population with one tool
  (§3.14.4). A filtered run can therefore never pass as a whole one.
- **Fail fast:** the gate stops at the first leg with a red and reports RED and INCOMPLETE. It never reports GREEN
  unless every leg ran. An implementer is done only on GREEN.
- **The verdict line counts the whole population, by discovered case:** `=== BUILD-LOCAL GATE: GREEN — Conformance
  9,317/9,317 · Unit 29,703/29,703 · Characterization 33/33 cases ran (skipped 0) in 2 legs (plan 3f1c…: map dbea124,
  timings run 20260928T2210-7c1e) ===`. A case "ran" when it has a Passed or Failed result; a case whose only result
  is NotExecuted (a `[Fact(Skip)]`) is counted SEPARATELY as skipped, never as ran (`b3`).
- **No plan input, a stale one, or a broken one → ONE leg in the plain order.** Never a skip.
- **The gate has two MODES, named by the caller, never inferred** (`build-local.ps1 -Mode lander|implementer`,
  required, no default; `-Priority` only sets the process priority). `implementer`: a plan, two legs, fail-fast, a gate
  slot (§3.14.6). `lander`: no plan, ONE leg, no fail-fast — every red of every cluster in one run — and no slot. Both
  hold the worktree's gate lock and both run the population check. The battery and CI do not use the driver.

#### 3.14.2 The plan: `scripts/gate_plan.py`

The driver first LISTS each assembly (`dotnet test --list-tests`, 2.0 s for Conformance), so the plan assigns every
discovered case explicitly. Inputs, each optional: the diff against the worktree's cut point; the impact map for the
base (§3.13, any age); TIMINGS — the per-test durations in the trx files of this worktree's most recent COMPLETED
gate (its run directory, §3.14.3), else the map's recorded durations; and that gate's REDS (trx outcome `Failed`).

**The name key.** Every lookup — rank, tier, timing, red — keys on `NameKey(display name)`, never on the raw name,
because the raw name is not stable (`b7`):
- a partitioned family (`TestPartitioning.Slice`) puts row *i* in class `_P{i % Partitions}`, and that class is part
  of every row's name. Appending one golden to the 85 manifest shifts every later corpus row by one position, so
  **1,398 rows (450 test-seconds) change name** — to a raw-keyed plan all unknown, all forced into leg 1, and leg 1's
  serial floor rises from 46 s to **208 s** (one `CorpusRunnerTests` partition). `NameKey` removes the partition suffix;
  keyed that way, the append leaves ONE unknown test (the new golden) and the floor unchanged — re-run through
  `gate_plan.py` itself (`b7`'s last table): one tier-0u case, a 15.3 s floor. Appending to 2002 renames 838 rows, to
  2014 677; to 2023 or the negative manifest, only the sentinel.
- **The contract** (`gate_plan.py#name_key`; the test hosts' leg filter mirrors it exactly, M12): split the display
  name at its FIRST `(`; in the part before it, split on `.`; when there are at least three segments and the
  second-to-last (the CLASS) matches `(.+)_P\d+` in full, replace it with the capture; rejoin, and append the `(` and
  everything after it unchanged. A theory's arguments are never touched. Only a partition class may carry the suffix:
  `ImpactedTestsDriftTests` is red on any other `class …_P<k>` under `tests/` (a partition is exactly
  `class Family_P<k> : FamilyBase<Slot<k>>`).
- a display name must not embed a worktree path. 404 Unit rows (`ParenTokenTwinDriftTests`) carried the absolute
  source path, so the map recorded in a detached worktree and a gate in another never share those names (`b8`:
  `E:\\COBOL-wt\\battery87\\…` against this worktree's root). Substituting a placeholder for the root is NOT a fix:
  xunit truncates a long argument with `···` at a fixed length, so the cut point moves with the root's length. The
  test passes the repository-relative path instead (M12), and `GateLegDriftTests` arm (5) is red on any discovered
  name in a gated assembly that contains the repository root, so the next such test cannot recur.

**The tiers.**
- **Tier 0a** — a case of a test METHOD the diff adds (matched by method name in the assemblies the file is compiled
  into); a corpus row whose manifest entry or golden files the diff changes; a case red at this worktree's previous
  gate. Always leg 1, exempt from the budget and the cap. The implementer's own new tests and goldens are here by
  construction. ⚠ **Corrected by the M11 acceptance (kb/Work PB1717):** this tier first held every case DECLARED IN a
  changed test file. 68b X edits the corpus and continuity harnesses (`CorpusRunnerTests.cs`, `VersionMatrixTests.cs`),
  so that rule put 5,774 cases and 8,037 test-seconds in tier 0a, and its first red ran after **62.8 %** of the work
  (`b4`, measured through `gate_plan.py`). An EDITED test or harness is tier 1 through the map instead (the test
  assemblies are recorded, §3.13), where cheapest-first finds X's first red after **0.32 %**.
- **Tier 0u** — a case no timing source and no map knows (genuinely new, after `NameKey`). Each is charged the
  assembly's median recorded time and admitted to leg 1 within its OWN budget, `LEG_ONE_UNKNOWN_BUDGET` = 2 % of the
  assembly's recorded test-seconds, in declaration order; the overflow leads leg 2. A map far older than the base
  therefore cannot swell leg 1 without bound.
- **Tiers 1–3** — `impacted_tests.py` (§3.13); with no map, every case is tier 1; a case the map never recorded but a
  timing source knows is tier 1. A key several cases share takes the lowest tier among them.
- **Timings** are per case, by `NameKey`: the previous gate's trx duration, else the map's. Cases sharing a truncated
  name are charged their mean; a theory xunit cannot serialize is LISTED as one case (`Class.Method`) but RUN as rows
  (`Class.Method(item: …)`), so that listed case is charged its rows' sum. A case's COLLECTION is the map's collection
  for its raw class (so the `process-globals` classes share one), else the raw class — a partition keeps its suffix,
  being its own collection.
- **Leg 1** = tier 0a + the admitted tier 0u + the cheapest cases of tier 1 while their recorded time stays within
  `LEG_ONE_BUDGET` = 2 % of the assembly's recorded test-seconds AND the case's collection's budgeted leg-1 time stays
  within `LEG_ONE_COLLECTION_CAP` = 15 s (tier 0 is exempt from the cap). **Leg 2** = everything else.
- **Why the collection cap.** xunit runs a collection serially, so leg 1's wall is at least its largest collection's
  part, and on a GREEN gate every other core idles at the leg barrier until that part ends. Uncapped, a 2 % budget
  holds 2,883 of 9,217 Conformance cases and one `CorpusRunnerTests` partition holds 46 s of it; capped at 15 s it
  holds 2,740 and the floor is 15.2 s (`b7`, keyed by `NameKey`; `b5` measured the uncapped budget). The barrier's
  remaining green-path cost — that floor plus one more test-host start per assembly — is measured by M13 and counts
  against the cap's retirement criterion (§3.14.6).
- **Order inside a leg.** Collections by their best-ranked case (tier, then cost), and inside a collection its cases
  by the same key — the assembly-level `TestCollectionOrderer` / `TestCaseOrderer` of §3.14.3. In leg 2 the
  collections go LONGEST FIRST (largest recorded sum first), so a surviving long pole starts at once instead of
  capping the wall; with no timings, xunit's own order.

**The plan file** (`plan.json` in the gate's run directory) holds, per assembly, the leg-1 KEYS, the leg-2 KEYS, the
per-key rank, and the plan's SHA-256. Its format (schema 1), written by `gate_plan.py --out` as canonical JSON (UTF-8,
no BOM, sorted keys, no insignificant whitespace):
`{"schema": 1, "base", "map", "map_state", "timings", "every_tier1": [reasons], "constants": {LEG_ONE_BUDGET, …},
"assemblies": {"Conformance": {"legs": [1, 2] | [1] | [2], "leg1": [key, …], "leg2": [key, …], "stats": {…}}, …},
"sha256": …}`. `leg1` and `leg2` are in RANK order — a key's rank is its position, leg 1's list first — so the rank
needs no second structure; `legs` names the legs the driver invokes the assembly for; `stats` is for people (tier
counts, leg-1 cases, seconds and floor, the one-leg reason). `sha256` is the SHA-256 of the canonical JSON of every
other field: the plan's identity on the verdict line and in the identity records. The handshake digest
`COBOLNET_GATE_PLAN_SHA256` is the SHA-256 of the file's BYTES, which `gate_plan.py` prints as its last stdout line.
**The leg function:** `LegOf(case) = 1` if `NameKey(case.DisplayName)` is a
leg-1 key, `2` if it is a leg-2 key, and `1` if it is neither — total over every name, so a case the plan never
heard of runs in leg 1, never nowhere. Display names are the key's source because they are the one identity the
trx's test definitions, the map and `--list-tests` share (`b8`); they are NOT unique (xunit truncates long theory
arguments with `···`: in Unit one name covers four rows), so the function assigns every case of one name to the same leg, which
keeps it total and disjoint (`b3`: two rows sharing a truncated name both ran in leg 1).

**Degenerate inputs, decided PER ASSEMBLY.** No timings (neither a previous run nor the map knows any of its cases):
leg 1 is tier 0a alone, ordered by tier — nothing can be told new from old, so nothing is tier 0u. When an assembly's
leg 1 would be empty, or would hold its whole population, that assembly runs in ONE leg, and the driver does NOT
invoke it for the leg its plan leaves empty — an executor handed no case makes vstest print "No test is available",
no verdict line, and exit 0 (`b3`), which the leg report would score RED; the population check still covers that
assembly through its other leg. An assembly whose whole recorded time fits `LEG_ONE_COLLECTION_CAP` runs entirely in
leg 1: Characterization (33 cases, ~2 recorded test-seconds), whose 2 % budget alone would admit nothing.

#### 3.14.3 How the legs run on this runner (xunit 2.9.2 · xunit.runner.visualstudio 2.8.2 · Test.Sdk 17.11.1)

Each primitive the runner offers was measured (evidence `b3-order-probe/`: 24 collections × 4 facts, one theory whose
two rows share a truncated name, one skipped fact, on this repository's package versions; and on the Conformance and
Unit assemblies themselves, `b8`):

| primitive | verdict |
|---|---|
| `dotnet test --filter` naming the leg's rows | ⛔ rejected. A leg of ~3,000 theory rows needs one `DisplayName=` term each: about 400 KB at ~130 characters a term, beyond Windows' 32,767-character command line. The complement leg needs the De Morgan complement of the whole union. A value containing a space silently matched NOTHING and exited 0 (`DisplayName~edition: 2023`, measured). Truncated `···` names are not unique keys. |
| a runsettings `TestCaseFilter` | ⛔ rejected: the same expression language and the same defects, minus the length limit. |
| vstest.console `/Tests:` | ⛔ rejected: a SUBSTRING match on names (ambiguous by construction) and not reachable through `dotnet test`. |
| xunit `StopOnFail` (runsettings `xUnit.StopOnFail`) | ⛔ rejected: at the first red the test host CRASHED (`OperationCanceledException` in `XunitTestAssemblyRunner.RunTestCollectionAsync`) and vstest reported "Test Run Aborted". Two runs kept 0 and 6 of 99 results, never the red's — its message was lost, and the second run's summary read `Passed! - Failed: 0`. |
| assembly `[TestCollectionOrderer]` + `[TestCaseOrderer]` | ✅ used. Under 4 parallel threads the planned collections started in the first wave and each ran its planned case first; unplanned ones kept xunit's order behind them. |
| an executor that DROPS the cases outside its leg (`XunitTestFrameworkExecutor.RunTestCases` over a filtered list) | ✅ used. A dropped case leaves no trace in the trx — no result and no test definition — and the two legs' test definitions together equal `--list-tests` as a multiset, both rows of the truncated-name theory included (99 = 99). |
| a framework CONSTRUCTOR that throws, to refuse a bad environment | ⛔ rejected: xunit catches it and silently falls back to its default framework — every case ran, `Passed!`, exit 0. |
| an executor that throws | ⛔ rejected: exit 1, but "Catastrophic failure" with no verdict line and no trx result. |
| an executor that reports every case as xunit's `ExecutionErrorTestCase` carrying the cause | ✅ used to refuse: every case Failed with the message, a `Failed!` verdict line, exit 1. |
| `dotnet test --list-tests` | ✅ used for the population: 9,317 Conformance cases discovered in 2.0 s; the trx's test definitions of a full unfiltered run equal it as a multiset (`b8`). |

So the leg filter lives INSIDE the test assemblies, where the test cases are objects rather than strings:
`tests/_shared/GateLegs.cs` (linked into every project under `tests/` by `tests/Directory.Build.props`'s `_shared`
glob, as `TestPartitioning.cs` is) holds `GateNameKey` (the name key, mirroring `gate_plan.py#name_key`), the plan
reader, `GateTestFramework : XunitTestFramework`, whose executor runs `testCases.Where(c => plan.LegOf(c) == leg)`,
and the two orderers (landed, kb/Work PB1719):
- **The framework attribute is written by `tests/Directory.Build.props`**, for every project with `IsTestProject`,
  as an MSBuild `AssemblyAttribute` (`Xunit.TestFrameworkAttribute("CobolNet.Gate.GateTestFramework",
  "$(AssemblyName)")`): xunit resolves the framework by ASSEMBLY name, which a linked source file cannot spell, and
  on any miss it silently runs its default framework — every case, no filter. `GateLegDriftTests` arm (4) checks it
  in each gated assembly.
- **The plan names an assembly by its key**: the test assembly's name without `Cobol.Net.Tests.` (`Conformance`,
  `Unit`, `Characterization`), as the impact map and `gate_plan.py --list` do.
- **The orderers are installed by the executor's assembly runner**, after xunit reads the assembly's own orderer
  attributes and only under a plan, so a run with no handshake keeps xunit's order exactly (an orderer attribute
  would also need the assembly name, and would act on every run). The collection orderer ranks a collection by its
  best-ranked case; the case orderer ranks a class's cases by their own rank; unplanned ones keep xunit's order after
  them. The executor also hands the runner the leg's cases in rank order, because xunit runs a collection's classes
  in the order their first case arrives. ⛔ Each orderer's output is CHECKED to be a permutation of its input
  (`GateOrder.Permutation`) and throws otherwise: xunit trusts an orderer's result, so a dropped case would silently
  never run, while a thrown orderer is logged and xunit keeps its own complete order.
- **Any failure while choosing the leg's cases** — reading the handshake, the plan, writing the identity record, or a
  defect in that code — refuses the run the same way as a bad handshake below; the executor never throws.

**The environment handshake** — three variables, all written by the driver:
`COBOLNET_GATE_PLAN` (the plan file), `COBOLNET_GATE_LEG` (`1` or `2`) and `COBOLNET_GATE_PLAN_SHA256` (the driver's
digest of that file).
- **None set** (the battery, CI, the lander's single leg, an IDE): every case, xunit's order, exactly as today. An
  EMPTY value counts as unset (Windows cannot hold an empty environment variable at all).
- **All three set and consistent** (the file reads, hashes to the digest — compared case-insensitively — and parses
  as a schema-1 plan naming this assembly; the leg is `1` or `2`): the leg's cases, in plan order.
- **Anything else** — one or two of them set, the file missing, unreadable or unparsable, a digest mismatch, a leg
  other than `1` or `2`, a plan of another schema, one that names no entry for this assembly or puts one key in both
  legs: the executor
  reports EVERY case as an `ExecutionErrorTestCase` naming the cause
  (`GATE ENVIRONMENT INCOMPLETE: COBOLNET_GATE_LEG is set but COBOLNET_GATE_PLAN_SHA256 is not`). The run is RED, never
  a silent whole or partial run (measured on the real hosts, evidence `m12`: each of eight partial or stale
  environments gave `Failed!` and exit 1; vstest keeps one failed result per test METHOD, because the error cases of
  one method share its id, so Characterization's 35 cases show 4 failed results). A developer who exports two of the variables by hand to reproduce a leg therefore
  gets a red battery, not a green one with a third of Conformance missing.
- ⛔ **Every caller of `dotnet test` scrubs its environment** — the three handshake variables, and with them the two
  OTHER environment channels that narrow a run while it exits 0 (M14, measured 2026-09-28 on Characterization's 33
  cases): MSBuild imports every environment variable as a property, so `VSTestTestCaseFilter=FullyQualifiedName~Snapshot`
  ran 31 cases, and `RunSettingsFilePath` (or `VSTestSetting`) naming a runsettings file with a `TestCaseFilter` did
  the same — in the run AND in `--list-tests`, so a population listed in the caller's environment is blind to them.
  The rule is written once, `test_population.py`'s `is_scrubbed`: the prefixes `COBOLNET_GATE_` and `VSTest`, and
  `RunSettingsFilePath`, case-insensitively. A python caller passes `env=scrubbed_env()` (`record_impact_map.py`,
  `filter_population.py`; the driver merges its handshake over the scrubbed environment); every other caller runs
  `python scripts/test_population.py scrubbed dotnet test …` — `battery.sh`, `build-local.ps1`/`.sh`, `gen-vcr.ps1`,
  `gen-diagnostics-doc.ps1`, `guard.sh`, `guard-fast.sh`, `measure-battery-determinism.sh`, and the gate command
  `record_verdicts.py` prints. It runs the command rather than printing names to unset, because a PowerShell script
  run in the operator's session would otherwise delete the operator's own variables. **The CI workflow scrubs by
  construction**: a GitHub-hosted job's environment is exactly what the workflow declares, so it scrubs by setting
  none of those variables. `GateLegDriftTests` arm (6) runs `test_population.py audit-callers`, which is red on a
  `dotnet test` in command position under `scripts/` (the hooks excepted: they receive commands as data), on a
  python `["dotnet", "test", …]` without `scrubbed_env`, on a printed unscrubbed instruction, and on a workflow that
  sets a scrubbed variable. A console marker printed from inside the test host was considered and not adopted: what
  vstest shows of a host's output at `--verbosity quiet` is unmeasured, while the refusal arm and the population
  check below are measured.

**Each leg host writes its IDENTITY RECORD** into the gate's run directory — the plan's directory — as
`leg-<n>-<assembly key>.json`, before it runs a case (written to a temporary name and moved into place): `schema`
1, `assembly`, `leg`, `plan_sha256` (the digest it verified), `plan_content_sha256` (the plan's own `sha256`),
`test_assembly` (file, MVID and SHA-256), `product_assemblies` (the SHA-256 of every `Cobol.Net.*`, `CobolSharp.*` and
`cobol.dll` beside the test assembly: the compiler under test), `received` (how many discovered cases the host was
handed) and `runs` (the name key of every case it runs, in run order). The recording build's `ImpactTestFramework`
derives from `GateTestFramework` — overriding only its one extension point, `WrapMessageBus` — and the recording
targets define `IMPACT_RECORDING`, whose presence in `DefineConstants` drops the `tests/Directory.Build.props`
attribute, so an assembly always names exactly one framework (MSBuild evaluates item conditions after every property,
so the props file sees the targets' constant).

**The driver** is `scripts/run_gate_legs.py`, called by `build-local.ps1` and `build-local.sh` in place of their `Leg`
functions and of `filter_population.py` (a gate without a vstest filter has no term to prove live;
`filter_population.py` stays for CI's shards, the generators and `record_verdicts.py`, which still filter). One gate:
1. **The worktree's gate lock** — an exclusive OS lock on `<worktree git dir>/cobol-gate.lock`, held from before the
   build to the verdict. A second gate in the same worktree REFUSES at once, naming the holder's pid: its build would
   otherwise overwrite the binaries between the first gate's legs.
2. The gate slot (`-Mode implementer` only, §3.14.6) — taken BEFORE the build, so the cap bounds the builds too. The
   lock order is always worktree lock, then slot, so no two gates can wait on each other in a cycle.
3. The audits and the solution build, then the binaries' SHA-256 record.
4. A fresh RUN DIRECTORY `TestResults/build-local/<UTC stamp>-<nonce>/`: the plan, every trx, every identity record
   and the verdict file. Nothing is written to a fixed name, so no earlier gate's file can be read as this one's. The
   next gate's timings and reds come from the newest run directory holding a verdict file; the driver keeps the last
   five.
5. `--list-tests` per assembly, the plan, then per leg the three assemblies CONCURRENTLY (the battery's `Conformance ∥
   Unit ∥ Characterization` shape), each `dotnet test --no-build --logger trx` with the handshake, each printed
   through `test_leg_report.py`; an assembly the plan gives no case in a leg is not invoked for it.
6. The population check (§3.14.4), then the verdict line.

`-Filter` is removed from both scripts and every caller in M13 (§3.14.9).

#### 3.14.4 Soundness: the population check — ONE tool for every whole-assembly run

`scripts/test_population.py` (a module and a CLI) answers one question for any run: did the union of its trx files
execute exactly the discovered population? Its callers are the gate driver (per assembly, over both legs: the module's
`list_population` and `check`), the battery (`battery.sh` PHASE 1, all three assemblies — `check --project … --trx …`;
until M14 it accepted any `Passed!` line with exit 0), and CI's `conformance-population` job (`check --listing … --trx
…` over each platform's shard trx files; the inline `grep -c` count block and the `shard-count-*` artifacts were
deleted in the same change, because a count cannot see a dropped test offset by one that ran twice). Each CI shard
lists its OWN build's population (`test_population.py list`, 2 s) and uploads it beside its trx as
`test-results-<OS>-conformance-<shard>`, so the checking job needs no .NET and no build of its own, and the listings
of one platform must be identical — the shards ran one build — or the check is an error. It also owns the ONE parser
of a `--list-tests` output (`parse_listing`); `filter_population.py` and `record_impact_map.py` import it.
- **The population** is `dotnet test --list-tests` — vstest discovery, which never calls the executor and so cannot see
  the leg filter — listed by the tool itself in a SCRUBBED environment (§3.14.3), because the VSTest environment
  channel narrows discovery exactly as it narrows the run.
- **The executed set** is the multiset union of the trx files' TEST DEFINITIONS (`<UnitTest name>`, one per
  discovered case that executed). Not the RESULT names: xunit lists a theory whose data it cannot serialize as ONE
  case and then reports one result per row, so Unit's results carry 33 names `--list-tests` never printed and miss 2
  it did, while its 29,703 definitions equal the listing exactly (`b8`).
- a name short → `NEVER RAN` (red, each named) — unless the run stopped early, when the remainder is reported as NOT
  RUN and the verdict is already RED/INCOMPLETE;
- a name over → `RAN TWICE` (red, named);
- a definition the listing never printed → `NOT IN THE POPULATION` (red, named: a trx of another build or assembly);
- a definition with no Passed or Failed result → counted as SKIPPED on the verdict line, never as ran;
- an input it cannot read (a missing or non-trx file, a listing without vstest's marker, an EMPTY population,
  listings that disagree) → exit 2, an error, never a finding. Each run prints one line,
  `=== POPULATION <label>: EXACT — 9,317/9,317 cases ran (skipped 0) across 1 trx ===` or `RED — …` followed by the
  named cases;
- (gate only) an IDENTITY MISMATCH → red: two legs read two plans (digest), or a leg ran binaries other than the ones
  the driver built and listed (the identity records' hashes against step 3's), so totality no longer holds.

`GateLegDriftTests` (Unit) proves it fails: (1) `LegOf` is total and deterministic over synthetic cases with
repeated, unknown and truncated names and an empty plan, and `NameKey` strips exactly the partition suffix; (2) each
orderer's output is a permutation of its input, and a planted non-permutation throws; (3) `run_gate_legs.py
--self-test` and `test_population.py --self-test` fire every arm on planted inputs — a dropped case, a duplicated
case, a skipped case, a digest mismatch, a binary changed between legs, a red in leg 1 (stop, remainder named,
RED/INCOMPLETE), no plan (one leg, plain order), an assembly whose leg 1 is empty (not invoked; its population still
whole), a partial handshake on a real leg host (every case an execution error, RED), a second gate in the same
worktree (refused), and `-Mode lander` (one leg, no fail-fast, no slot); (4) every test project under `tests/` that
the gate runs links `GateLegs.cs` and names `GateTestFramework`; (5) no discovered display name in a gated assembly
contains the repository root. Arms (4) and (5) audit the assembly they run in (`tests/_shared/GateLegAudit.cs`: the
attribute, and xunit's own discovery with theories pre-enumerated, checking each name and each string argument,
since a truncated argument can cut the root off; the root is matched as TEXT in its written, `/` and xunit-escaped
forms, never through the host's path rules, and Unit's witness plants a Windows, a POSIX and the host's own root, so
the arm is proven on each OS CI runs), so every gated assembly carries a `GateLegDriftTests` with those
two arms; Unit's also holds (1), (2), (3)'s population half, (6), the handshake arms and (4)'s structural half over
every test `.csproj`; (6) every `dotnet test` caller scrubs `COBOLNET_GATE_*`, `VSTest*` and `RunSettingsFilePath`
(`test_population.py audit-callers`, §3.14.3) — the driver included, which merges its own handshake over the
scrubbed environment. Each arm is one test method named for its number, so the mechanisms that land them separately
merge by union.

#### 3.14.5 Whole-suite speed — measured first

An uninstrumented whole-Conformance run (battery #87, `b1`): 9,054 tests, **571 s wall**, 11,553 test-seconds.

| share of test-seconds | method |
|---|---|
| 54.4 % | `VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition` — the continuity cells, 1,047 rows |
| 18.1 % | `NistDifferentialTests.NistProgram_MatchesGolden`, 349 rows |
| 10.8 % | `CorpusRunnerTests` (negative 6.3 %, positive 4.5 %) |
| 3.3 % | two single tests: the corpus row `pb505-table-value-dynamic-span-levels` (266 s, inside the corpus share above) and `ValueFormat2Tests.DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) |

**The critical path is the twelve continuity partitions:** eleven of them ran 525–557 s each and all twelve ended
in the run's last 26 s, against a 361 s floor for the whole assembly's work on 32 threads. A continuity cell is two check-only compiles of
one NIST program (permissive, strict) at a later edition.

**Why a partition is slow — one lock in the lexer (`b6`, `b2`).** One partition alone: 0.97 s per row. All twelve
in one process: 369–395 s, ~4.5 s per row, with the test host using only ~3.8 of 24 cores. Server GC: 360 s. The
same work as two concurrent processes: 209 s; four: 136 s — the limit is INSIDE one process. An in-process probe of
exactly that work (`b2-scaling`) peaks at 3.5× by 8 threads, with ~1,400 contended `Monitor` acquisitions per
compile. Stack samples name it: **28 of 30 single-thread samples, and all 24 twelve-thread stacks, are in ANTLR's
`LexerATNSimulator.ComputeStartState` / `AddDFAState`** — half of the latter blocked in `Monitor.Enter`. ANTLR 4.13.1
`LexerATNSimulator.MatchATN` caches a mode's start state only when its closure carries no semantic context, and
`CobolLexer.g4`'s default mode has six predicates on the LEFT EDGE of a rule (`FN_SIGNED_FLOATLIT`,
`FN_SIGNED_COMMA_FLOATLIT`, `FN_SIGNED_DECIMALLIT`, `FN_SIGNED_INTEGERLIT`, `DEFINED`, `FNARG_SEPARATOR`). So every
token of every compilation recomputes the closure over all default-mode rules and re-adds it under
`lock (dfa.states)`. That is a PRODUCT defect: the `cobol` CLI pays it serially, and any host compiling in parallel
(the test host, an IDE) pays the lock too.

- **M6 — after warm-up, lexing performs NO ATN simulation.** Moving a predicate off the left edge is NOT enough.
  ANTLR 4.13.1 (decompiled `LexerATNSimulator`) never stores a DFA edge whose target configuration set passed
  through a predicate (`AddDFAEdge(from, t, q)` returns before `AddDFAEdge(from, t, state)` when
  `q.hasSemanticContext`), so every token that REACHES a predicate re-runs `ComputeTargetState` and `AddDFAState`
  under `lock (dfa.states)` — on every occurrence, not once. With `DEFINED`'s predicate one character in, s0's `D`
  edge stays uncached and every D-initial default-mode token (DATA, DIVISION, DISPLAY, DEPENDING, D-initial names)
  still takes the lock; `FNARG_SEPARATOR`'s does the same for every `,` and `;` separator, and the `FN_SIGNED_*`
  rules' for every `+` and `-`. So each predicate LEAVES the lexer's hot paths:
  - `FNARG_SEPARATOR` and `DEFINED` match exactly the text their fallback matches, so an ACTION picks the outcome
    (`[,;] [ \t\r\n]+` always lexes as one token; the action keeps it as `FNARG_SEPARATOR` in function arguments
    and skips it otherwise, exactly as `COMMA_SEP` / `SEMICOLON` and the whitespace rule skipped, and `COMMA_SEP`,
    which it subsumes, is deleted). `DEFINED` lost its rule: it is a virtual token that `IDENTIFIER`'s action
    retypes when the directive expression is primed. ⚠ The first cut — an unconditional `'DEFINED'` rule retyping
    itself to `IDENTIFIER` — lexed identical tokens but published the literal name `'DEFINED'`, which
    `CobolKeywordTokens` reads as "the lexer makes this word a keyword token" (so `>>COBOL-WORDS` would have
    treated DEFINED as one) and `CobolWordsDriftTests` flagged; a token-stream differential cannot see a
    vocabulary consumer, so this is recorded here (DESIGN-frontend-grammar.md §3.3e). Actions do not suppress
    DFA edges.
  - the four `FN_SIGNED_*` rules lex ONE token where the fallback lexes TWO (the sign, then the number), so an action
    cannot choose between them by retyping alone. They stay unconditional DEFAULT-mode rules, and their action
    (`OnSignedLiteral`) keeps the signed literal where `SignedLiteralCanStart()` holds — now asked of the character
    before the TOKEN'S START — and otherwise CUTS the token back to its sign: the type becomes `PLUS`/`MINUS`, the
    input is re-seeked to the character after the sign and the column restored (no numeric body contains a line
    break), so the lexer resumes at the digits and lexes them exactly as the predicate-false path did. The
    alternative, a mode entered by the preceding token's action, was rejected: the context is "a separator
    character immediately before the sign", which a mode would have to re-derive on every token.
  `LexerDfaCacheDriftTests` pins the invariant that matters: after one warm-up pass over the suite's sources, no
  ASCII character of a re-lex leaves the cached DFA, and `s0` is non-null for every mode. ⚠ The instrument is NOT
  a counting `LexerATNSimulator` subclass as first designed: in ANTLR 4.13.1 `MatchATN`, `ComputeTargetState` and
  `ExecATN` are not virtual, so no subclass can count them. It is a probe on the INPUT side — `Lexer.NextToken`
  and `LexerATNSimulator.Match` each `Mark()` the `ICharStream` as a match begins, and the test's stream, at that
  moment, walks the cached DFA of the lexer's current mode over the coming characters exactly as `ExecATN` will and
  records the first place the walk would leave the cache (no start state; a missing ASCII edge; a character above
  127; EOF). The lexer runs over a PRIVATE DFA, so the measurement is independent of what other tests lexed first,
  and the warm-up pass must itself see misses, or the probe is blind. The next predicate on a common prefix is
  therefore red, wherever in the rule it sits — measured: the pre-M6 grammar is red (5,216,645 start-state misses
  on the re-lex, 13 min against the fixed grammar's 3 s), and a predicate planted one character into `DEFINED`
  (`'D' {true}? 'EFINED'`) is red (68,289 mid-token misses — every D-initial token). ⚠ ANTLR's DFA edges cover only
  characters 0–127 (`MAX_DFA_EDGE = 127`), so a non-ASCII character is simulated on every occurrence, and the EOF
  transition is never cached (`GetExistingTargetState` rejects `t < MIN_DFA_EDGE`), so the token that reaches the
  end of an input is simulated once per input; the test REPORTS both, never asserts them. Over the lexer inputs the
  suite actually produces (17,055 distinct preprocessed texts and fragments): 14,556 ASCII inputs re-lex with zero
  misses; the 2,499 non-ASCII ones (38,675 non-ASCII characters in 72.8 M) leave the cache 77,196 times — against
  9,955,488 tokens. The implementing change proved equivalence once: every distinct (lexer input, priming) the
  Conformance, Unit and Characterization assemblies lexed on the pre-M6 grammar (compile cache off) was captured and
  replayed through both lexers, and all 17,055 token streams — type, channel, offsets, line, column, text, lexer
  errors and final mode — are identical (DEVLOG). Measured gain (`b2-scaling/b2-after-m6.txt`): the one-thread
  wall of `b2` fell from 110.1 s to 3.4 s (32×, beyond the 15× Amdahl bound estimated from the stack samples), at
  12 threads it is 2.0 s (55× today's one-thread wall: acceptance MET) with ZERO contended `Monitor` acquisitions;
  CPU utilisation at 12 threads is 21 % (acceptance NOT met) and the reason is no longer the lexer — 1.3 s of the
  2.0 s is workstation-GC pause, and the same run under server GC reaches 87 %. `b1` is re-measured on the next
  battery.
- **Sharing one front end across editions is REJECTED** as unsound: edition state is cached in the front end
  (`Frontend.LexAndParse` primes the lexer with the cell's `EditionInfo`; the reference-format and COPY
  preprocessors and `CobolParserCoreBase` hold per-edition state), so a shared front end would answer the 2014 cell
  from another edition's parse. M6 changes no test's inputs.
- **Process sharding is not built.** It bought 1.8× (two processes) and 2.8× (four) only because each process has
  its own lock; building it would route around the product defect (CLAUDE.md rule 4). After M6, `b6` is re-run: if
  two processes still beat one by more than 15 %, the next in-process serialization is found and fixed at its root.
- **Server GC** bought 3–9 % while the lexer lock bound the process: not adopted then. After M6 it is the next
  in-process serialization — `b2` over the whole green set spends 3.1–4.0 s of each ~5 s round paused in
  workstation GC at 8–24 threads, and reaches 6.1× at 87 % CPU on 12 threads under server GC
  (`b2-scaling/b2-after-m6.txt`) — so it is re-decided as its own mechanism, not inside M6.
- **The compiled-program cache** (§3.12) already stores check-only results, so a TEST-ONLY re-gate hits every
  continuity cell; a gate after a product change misses by design. No change.
- **§3.11's "the class-split lever is exhausted"** was measured under this lock. After M6, the partition counts
  (one `Partitions` constant per family, audited by `TestPartitionAudit`) are re-measured, starting with
  `CorpusRunnerTests` (3), whose `_P1` bounds leg 1.

**M7 — the long poles.** `pb505-table-value-dynamic-span-levels` (266 s in the battery; **29.6 s alone through the
`cobol` CLI** for a 26-line program) and `DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) are one
product defect: all three stack samples of the CLI compile sit in `TableValueOdometer.Resolve`, whose fill loop is
bounded only by its `MaxFillElements` ceiling (64,000,000) — and when the odometer can never carry out of a DYNAMIC
dimension with no TO, the phrase's TO is never reached, which is exactly the §13.18.63.3 SR23 violation ("the values
of subscript-1 and subscript-2 corresponding to all levels higher than that of the OCCURS clause, if applicable,
shall be equal") that `COBOLNET1946` then reports. The fix checks SR23 before any fill and bounds the
fill by the elements the phrase names; the test keeps asserting the diagnostic. **Landed (kb/Work PB1716):**
`TableValueOdometer.ElementCount` computes a phrase's element count as the mixed-radix distance from subscript-1 to
subscript-2 BEFORE the fill and returns none for a phrase SR20/SR21/SR23 reject, and `Resolve` fills exactly that
many elements, so the `MaxFillElements` cap is gone; the SR23 rule's shape ("a level above ANY unbounded
dimension") lives once in `TableValueOdometer.UnboundedBelow`, read by both the fill and the `COBOLNET1946` screen,
which had tested only the OUTERMOST unbounded dimension and so accepted two nested ones spanned at the outer level.
pb505 now rejects in 0.49 s through the CLI (was 25.3 s on the same build). The other poles over 30 s in `b1`
(`CompiledProgramCacheDriftTests.TheOutputDirectory_DoesNotReachTheOutput` 55 s, `OptionalWordSubsetDriftTests`
`special-names-85` 41 s, `St101A` 30 s, continuity rows of IX113A/NC177A/NC253A/RL101A at 40–49 s) are compile-bound
and are re-measured after M6 before any is touched. The battery summary prints the five slowest tests (a REPORT,
never a wall-clock assertion — MANDATORY-PRACTICES forbids those), so the next pole is seen when it appears.

#### 3.14.6 The gate cap (M2)

`scripts/gate_slot.py` is ONE counting semaphore for the whole repository, served FIFO:
- **Slots.** N lock files `<git common dir>/cobol-gate-slots/<k>.lock`, each held by an exclusive OS file lock
  (`LockFile` on Windows, `flock` on Linux), so a crashed holder releases its slot when it dies and no stale-pid
  cleanup exists. The holder writes its label (who, pid, since when) at the start of the file; on Windows the lock
  covers one byte far past the end of the file, so the label stays readable. The label is informational only: a slot
  is free exactly when its lock can be taken.
- **Tickets.** A waiter first takes a TICKET: under an exclusive lock on `cobol-gate-slots/tickets.lock` it reads and
  increments the monotonic counter in `tickets.seq` (rewritten atomically; a counter that is not a number is an error,
  never a silent reset, since a reset could hand a new waiter a number below a live one), creates
  `ticket-<seq>.lock` and holds it with an exclusive OS lock for as long as it waits. A ticket is LIVE while its file
  is locked, so a waiter that dies drops out of the queue by itself; the next scan deletes its file. Scans run under
  `tickets.lock`, so a ticket is never probed between its creation and its lock. A waiter may take a free slot only
  when no live ticket has a lower sequence number, and it drops its ticket only AFTER it holds the slot; so a gate
  that finishes and re-gates at once queues BEHIND everyone already waiting, instead of winning every race by polling
  first. `gate-slot: waiting, k ahead` counts the live tickets below one's own and is printed each time `k` changes;
  `gate-slot: took slot k of N (ticket t, waited s)` is printed on acquisition.
- **What holds it.** A Python caller holds a slot with `GateSlots.for_repo().acquire(label)` (a `Slot`, released by
  its `with` block or by the holder's death); a shell caller wraps a command with `gate_slot.py run [--label TEXT] --
  <command>`, which exits with the command's code. `run_gate_legs.py` takes the slot after the worktree's gate lock
  and BEFORE the build (§3.14.3), and holds it through the population check, so the cap bounds the concurrent solution
  builds as well as the test legs. The slot is held by the gate's whole PROCESS TREE. On Windows, acquisition puts the
  holder ITSELF into a Job object with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` before it spawns anything, so every
  descendant is in the job from birth, with no window in which a grandchild can escape, and if the driver dies the OS
  kills every `dotnet` process with it. On Linux the slot's descriptor is inherited by the children: Python's
  `subprocess` closes descriptors by default, so every spawn passes `**slot.spawn_kwargs()`. `dotnet` hands inherited
  descriptors on to its own children, and an `flock` lives until the last descriptor closes, so an ORPHANED Linux
  tree keeps its slot until it exits. The cap still bounds it; nothing kills it. Acquisition is never nested beyond
  the one fixed order (worktree lock, then ticket, then slot), so no two gates can wait on each other in a cycle.
- **Who takes one.** Every `-Mode implementer` gate, and the impact recorder. The LANDER (`-Mode lander`) and the
  BATTERY never take a slot and never wait. N comes from `COBOLNET_GATE_SLOTS` (a positive integer; anything else
  stops the gate with the reason). Its default is the largest N at which the lander's whole-Conformance leg stays
  within 1.25× of its quiet time with N implementer gates running, builds included, since the slot now covers them
  (M2's acceptance). The default is `DEFAULT_SLOTS = 2` in the script, PROVISIONAL until that measurement is made on
  a quiet host (kb/Work PB1720). The verdict line prints the wait and the slot (`Slot.describe()`).
  `gate_slot.py status` prints each slot's holder and the live tickets in queue order.
- `gate_slot.py --self-test` proves five arms against a throwaway repository with one linked worktree: the FIFO
  order (three waiters queued behind a holder are served in ticket order, and the holder re-gating the moment it
  releases is served LAST), the release on a killed holder, a dead waiter leaving the queue, the orphaned tree (on
  Windows the job kills the orphaned child and grandchild and the slot frees; on Linux the tree keeps the slot until it
  exits) and the sharing across worktrees (both checkouts resolve one slot directory, and a holder in one makes a
  waiter in the other wait). Each arm was seen RED on a planted defect: no ticket check, a ticket file counted live
  unlocked, no Job object, the descriptor withheld from Linux children, and the per-worktree git dir. It passes on
  Windows and under WSL, and `GateSlotDriftTests` (Unit) runs it in every Unit run, including CI's Linux unit jobs,
  so each operating system's arm is proven where it runs. Every helper process it starts exits once its sentinel
  file is deleted, so a red arm leaves nothing running on the host. ⛔ The cap does not span operating systems: a
  Windows lock and a WSL lock on a drvfs mount do not see each other; the repository's gates run on Windows
  (`build-local.ps1`), and the WSL legs of the Linux gate (section 3.15) are not counted against the cap.

**Retirement is measured** (owner, 2026-09-28): the cap goes when, over one full train, the median implementer
selection is under 25 % of the Conformance assembly, or a whole cold Conformance implementer gate at `BelowNormal` —
both legs, the leg barrier included — costs under 6 minutes on the shared host. Under ordering nothing is selected,
so the second criterion is the live one — and M6 and M7 are what can meet it.

#### 3.14.7 PB1712 under ordering

PB1712 found that the recorder cannot see static FIELD reads, so a change to one `DiagnosticCatalog` descriptor
reaches 80 tests in the map where the methods reporting it are reached by up to 9,225. Under selection that was a
silent SKIP of every test the map missed. Under ordering it cannot skip anything: a test the map under-reaches lands
in a later tier and still runs in the same gate, so the harm is a later red, never a missed one. The finding stays
documented in the note as an ORDERING-quality limit, and the gate needs no field probes for soundness. M11 deleted
the selection code (kb/Work PB1717), which closed PB1712: `impacted_tests.py`'s filter line is the whole assembly
whatever map exists, so a map recorded into the shared store can only reorder a gate, never narrow one.

#### 3.14.8 What the rejected SELECTION design's mechanisms become

`M1` (field probes, map schema 3) and `M9` (a map recorded per main commit) are not needed for soundness, and per-
commit recording is retired (owner, 2026-09-28, kb/Work PB1709): maps are recorded ON DEMAND. The gate works with any map or none, and on the one replay with named reds recorded cost
carried the order (§3.13). `M3`, `M4`, `M5`, `M8` and `M10` (the registry restructuring) are shelved as a speed
measure (owner, 2026-09-28) and revisited on engineering merit only. `M2` (the cap), `M6` and `M7` are redesigned
above. The rejected design and both reviews are in the DEVLOG entry that pivoted it and in kb/Work PB1708.

#### 3.14.9 Implementation plan — gateable mechanisms, each ≤ ~220 implementer turns

| id | mechanism | files | acceptance (measured, never asserted) | depends on | size |
|---|---|---|---|---|---|
| M6 | after warm-up the lexer performs no ATN simulation (§3.14.5); kb/Work PB1715 | `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4` and the predicate and action methods it calls; `tests/Cobol.Net.Tests.Unit/LexerDfaCacheDriftTests.cs`; this section | token streams of every suite source identical before/after (one-time differential, recorded in the DEVLOG); `LexerDfaCacheDriftTests` red on a planted predicate at the left edge AND on one placed after a common prefix's first character; zero ATN simulation on re-lexing the suite's ASCII sources (the one EOF transition per input ANTLR never caches is reported, not counted); the non-ASCII residual reported; `b2` at 12 threads ≥ 8× today's 1-thread wall, CPU utilisation ≥ 70 % (measured 55× and 21 %: the utilisation limit is now the workstation GC, §3.14.5); whole battery green | — | 140–200 turns |
| M7 | SR23 before the table-value fill; the fill bounded by the phrase (§3.14.5); defect note: id allocated by the orchestrator | `src/Cobol.Net.Compiler/Binding/Model/TableValuePlan.cs` (`TableValueOdometer.Resolve`), `DataBinder.ResolveTableValues`; battery summary top-5 report in `scripts/battery.sh` | the pb505 program rejects with `COBOLNET1946` in < 1 s through the CLI (was 29.6 s); both tests keep their assertions; every Format 2 VALUE golden unchanged; the battery summary lists the five slowest tests | — | 60–100 turns |
| M11 | the order plan: `NameKey`, tiers 0a/0u/1–3, the budgets and the collection cap (§3.13, §3.14.2); the NARROWING deleted — LANDED (kb/Work PB1717: b4 first red 0.32 %, b7 one tier-0u case and a 15.3 s floor; tier 0a corrected to ADDED test methods) | `scripts/spec/impacted_tests.py` (selection code DELETED — its filter line is always the whole-assembly filter until M13 deletes the line; `--plan` added), `scripts/gate_plan.py`, `tests/Cobol.Net.Tests.Unit/ImpactedTestsDriftTests.cs`, kb/Work PB1712 (closed) | `--self-test` covers every tier arm, `NameKey`, the unknown budget, the collection cap and the no-map / no-timings / stale-map / empty-leg-1 / whole-assembly-in-leg-1 arms; the filter line is the whole-assembly filter for a base WITH a map; `b4` re-run through the new script reproduces cheapest-first on 68b X (first red ≤ 1 % of the work); `b7` re-run through it: a golden appended to the 85 manifest leaves ONE tier-0u case and a leg-1 floor ≤ 16 s | — | 110–160 turns |
| M14 | ONE population check for every whole-assembly run, and the handshake scrub (§3.14.3–4) | `scripts/test_population.py` + `--self-test`; `scripts/battery.sh` (PHASE 1 population check; scrub); `.github/workflows/build-and-test.yml` (`conformance-population` runs the tool on the shard trx files; the inline `grep -c` block DELETED; scrub); `gen-vcr.ps1`, `gen-diagnostics-doc.ps1`, `scripts/spec/record_verdicts.py`, `scripts/spec/record_impact_map.py` (scrub); `GateLegDriftTests` (6); `docs/DRIFT_RULES.md`. As landed, the arm-(6) scan also found `build-local.ps1`/`.sh`, `guard.sh`, `guard-fast.sh`, `measure-battery-determinism.sh` and `filter_population.py` (scrubbed), the scrub grew the VSTest channel, the two other `--list-tests` parsers were folded into the tool's, and `FilterPopulationGuardDriftTests` recognises the new shard shape | the self-test's arms (short, over, skipped, definitions vs results); the battery's PHASE 1 prints each assembly's population line and is red on a planted dropped case; CI's guard red on a planted shard overlap that keeps the count; `b8`'s two inputs pass | — | 60–100 turns |
| M12 | the in-assembly leg filter, the handshake and the identity records (§3.14.3) — LANDED (kb/Work PB1719, evidence `m12`: no handshake, 35 / 29,715 / 9,319 definitions = `--list-tests`; a plan from `gate_plan.py` ran Characterization in one leg and Unit 28,754 + 961 and Conformance 5,069 + 4,250, each leg exactly its plan's cases, union = `--list-tests`, five identity records; eight partial or stale handshakes each `Failed!`, exit 1; a recording at its head recorded 39,100 tests, the gate's 9,319 + 29,746 + 35, with the watchdog clean) | `tests/_shared/GateLegs.cs` + `GateLegAudit.cs` (linked by the existing `_shared` glob, so no `.csproj` changes), `tests/Directory.Build.props` (the framework attribute, by assembly name), `tools/impact/ImpactTestFramework.cs` + `ImpactRecording.targets` (`IMPACT_RECORDING`), `tests/Cobol.Net.Tests.Unit/ParenTokenTwinDriftTests.cs` (repository-relative path argument), `GateLegDriftTests` (1), (2), (4), (5) in Unit and (4), (5) in Conformance and Characterization | with no handshake every assembly's count and verdict are unchanged; with a full one, each leg's trx definitions are exactly its leg's cases and the two legs' union equals `--list-tests`; each partial handshake (one variable, two, missing file, digest mismatch, bad leg) makes every case an execution error and the run RED; each leg writes its identity record; a planted non-permutation throws; no display name carries the repository root; a recording at HEAD still records every test (the recorder's watchdog) | M11 (the plan format) | 130–190 turns |
| M2 | the cross-worktree gate cap, FIFO (§3.14.6) | `scripts/gate_slot.py` + `--self-test`; `tests/Cobol.Net.Tests.Unit/GateSlotDriftTests.cs` | the self-test's five arms (FIFO order included), on Windows and on Linux; N measured: the lander's whole-Conformance leg ≤ 1.25× quiet with N implementer gates, their builds included | — | 90–130 turns |
| M13 | the ordered gate: driver, modes, worktree lock, run directory, fail-fast, and the wiring that deletes the selection interface (§3.14.1, §3.14.3–4) | `scripts/run_gate_legs.py` (takes the slot, §3.14.6), `scripts/spec/record_impact_map.py` (the recorder takes a slot too, and spawns every child through `Slot.spawn_kwargs`), `scripts/build-local.ps1` + `.sh` (`-Filter` removed, `-Mode lander\|implementer` required, `Leg` replaced); `scripts/spec/impacted_tests.py` (the filter line and `--plus` DELETED); `GateLegDriftTests` (3); in the SAME change every caller of either interface: `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` (I1, I2, I7), `implementer-brief.md`, `fix-lane-implementer-brief.md`, `dispatch-spec-implementer.md`, `lander-train-brief.md` and `lander-brief.md` (`-Mode lander`), `.claude/skills/workstream/check_practices.py` (its required `impacted_tests\.py --base` patterns), `.claude/skills/workstream/SKILL.md`, `.claude/agents/cobol-implementer.md`, `.claude/skills/gate/SKILL.md`, `scripts/hooks/test_forbidden_commands.py` (its `-Filter` fixture), `tests/Cobol.Net.Tests.Unit/ImpactedTestsDriftTests.cs`, `docs/DRIFT_RULES.md`, `docs/DOC_INDEX.md`, plan §9, README, CONTRIBUTING, the PR template | the self-test's arms (§3.14.4 (3)); a real gate on a planted red in a leg-1 test stops after leg 1 with the remainder named; a real green gate's population equals `--list-tests` for all three assemblies; a real `-Mode lander` gate runs one leg with no slot; `check_practices.py` green with no `impacted_tests` pattern left; over the next train, each implementer gate's time to first red, whole wall and green-path barrier cost (leg 1's wall beyond its share of the work, plus the extra host starts) are recorded, against the lander's single leg | M11, M12, M14, M2 | 170–220 turns |

M6, M7, M11, M14 and M2 are independent and may run in parallel groups; M12 follows M11; M13 lands last, with M2 in
the same train so no implementer runs a whole-population gate uncapped. ⛔ **Nothing deletes an interface its callers
still read:** M11 only empties the selection behind `impacted_tests.py`'s filter line (every caller keeps working,
now always handed the whole assembly), and the line, `--plus` and `-Filter` go in M13 together with every caller named
above. CLAUDE.md "Testing" already states the ORDER, DON'T SKIP direction (owner, 2026-09-28); M13's landing asks the
owner to update it to the landed gate, since agents do not edit CLAUDE.md.

### 3.15 THE LOCAL LINUX GATE — CI's Linux legs under WSL before a push (kb/Work PB1732)

CI runs most of its test jobs on `ubuntu-latest`, and every local gate runs on the Windows host, so without this a
change's first Linux run was CI's. Train 71b showed the cost: a Windows path literal in a new drift test was green
on Windows and red in CI's Linux unit job. That was a ~30-minute round trip and a dropped cluster, for a failure a
2.5-minute WSL run reproduces.

**`scripts/linux-gate.sh`** runs, from any tree (`wsl -d Ubuntu --cd <tree> -- bash -lc 'bash scripts/linux-gate.sh'`),
the test projects CI's Linux jobs run. `LinuxGateDriftTests` holds that set equal to the workflow's.

**How: a Linux clone of the commit, never the Windows tree.** The tree's COMMITTED HEAD is cloned into
`~/linux-gate/<tree>` on the Linux filesystem. The clone is `--shared`: it borrows the Windows repository's object
store read-only, so nothing is copied. Each leg's project is built there with the Linux SDK and tested there, the
shape of CI's ubuntu jobs. The git-ignored GnuCOBOL corpus is copied in from the tree, or fetched as CI fetches it.
Commit before running: uncommitted changes are counted, reported, and not tested.

Two earlier designs failed, and each failure was measured:
1. **Windows-built binaries run `--no-build`.** The Conformance tests find their goldens through `[CallerFilePath]`,
   which a Windows build bakes in as `E:\…` paths that are relative on Linux: 363 false reds.
2. **The Windows tree, with `GIT_DIR`/`GIT_WORK_TREE` exported** so Linux git could read a worktree whose `.git` file
   names an `E:/` gitdir. The variables reached every test process. `gate_slot.py`'s self-test (git init, commit,
   worktree add in its own temp repositories) then wrote into the REAL repository: `core.worktree` landed in the
   shared `.git/config` and broke git in every checkout until the owner removed it (2026-09-29).

So the script exports nothing. Its only git calls on the Windows repository are two READS (HEAD, and the count of
uncommitted changes), with `safe.directory` and `core.autocrlf` passed inline.

Two guards stay beside the clone (group G, kb/Work PB1719). A self-test that builds its own repositories first drops
every variable `git rev-parse --local-env-vars` names (`gate_slot.py`, `status_guard.py`), so an inherited `GIT_DIR`
can never redirect it into the real repository. And the script snapshots the real repository's `HEAD` and
`core.worktree` before the legs and is RED (`repository-written`) when either changed. (Not the worktree list:
other agents add and remove worktrees of the shared repository while it runs.)

**Line endings.** `.gitattributes` keeps `*.sh` LF. Under `core.autocrlf=true` a fresh Windows worktree otherwise
checks the script out CRLF, and bash dies at its first line (`set: -: invalid option`).

**Verdict.** One `=== LINUX GATE: GREEN|RED|NOT RUN ===` line naming the HEAD it tested. NOT RUN is never green.

**Which legs run (MANDATORY-PRACTICES I8, L10): all three, at every implementer gate and every landing.**
- **Measured at `4fdc6897c`** (train 71b with F), 2026-09-29: 195 s in all.
  - `unit`: 75 s including the build; exactly CI's one red, `Arm5`.
  - `characterization`: 8 s.
  - `conformance`: 102 s, for 9,321/9,321.
- **The shared `.git/config` and the worktree list were unchanged afterwards.**

That is about 3–4 minutes for the whole Linux population. A platform-sensitivity detector, which would have added the
conformance leg only for flagged diffs, was built and deleted the same night. At that cost no selection is worth its
misses (the rule of section 3.14.1, ORDER, DON'T SKIP).

**Limits.**
- WSL is not `ubuntu-latest`, and the gate runs Debug where CI also runs Release. It catches platform assumptions,
  and CI through `push-main.sh` remains the proof.
- The gate cap (section 3.14.6) does not span operating systems, so WSL legs are not counted against it.

---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `tests/Cobol.Net.Tests.Characterization/` (+ `tests/characterization/`, `Snapshots/`) | The behavior-neutrality harness (gates 2 & 3) — the missing "prove I changed nothing" proof for every refactor phase. |
| create | — | `tests/nist/corpus.tsv` | ONE source of truth for the green NIST set + chains; kills the 3-way triplication. |
| create | — | `src/Cobol.Net.Compiler/Diagnostics/` (`DiagnosticDescriptor`, `Diag`, `Diagnostic`, `Severity`) | Registry so codes are addressable, snapshot-keyable, doc-generable, suppress-targetable. |
| (exists) | — | `docs/COBOLNET_REARCHITECTURE_PLAN.md` | Resumable migration SSOT + roadmap with per-phase exit criteria = battery-green gate (the master plan already in the repo). |
| create | — | `docs/DIAGNOSTICS.md` (generated) | The single human-readable code table (understandability #1). |
| create | — | `tools/DifferentialBakeTool` (or a skip-gated maintenance test) | One-time freeze of legacy output → committed goldens (the G8-survival bake). |
| create | — | `scripts/guard.ps1` | Cross-platform authoritative guard (Windows parity for the regression). |
| split | `NistDifferentialTests.cs` (318 `[InlineData]`) | `[MemberData]` over `corpus.tsv` + a thin theory | Remove the hand-maintained green list; single source of truth. |
| merge | `tests/nist/chains.tsv` | into `tests/nist/corpus.tsv` | One manifest, not two. |
| refactor | ~60 `*DifferentialTests.cs` (assert `cobolnet==legacy`) | golden-comparison base (`AssertMatchesGolden`) + `tests/differential/**/*.out` | Sever the oracle coupling so the net survives G8. |
| refactor | `EditionContext.Error(code,msg)` string-concat; 161 bare codes | `sink.Report(Diag.X, args)` | Route all diagnostics through the registry. |
| split | `COBOLNET0899` (~47 sites) | distinct unimplemented-feature descriptors behind a tracked list | One code = one rule; snapshot precision. |
| move | `CompilerUnderTest.LegacyCompiler` / `ICompilerUnderTest` | DELETE after the bake | Legacy oracle gone once goldens are baked. |
| refactor | `RoslynBackend.ReferenceAssemblies()` (uncached) | `static Lazy<ImmutableArray<MetadataReference>>` | Battery throughput (thousands of rebuilds → one). |
| refactor | binder passes behind the `CSharpEmitter.Bind` host facade | `Binding.BindPipeline → BoundCompilation` (standalone) | Bind phase boundary: fast `CheckOnly`, probe-able bound tree, clean Emit snapshot. |
| rewrite | `.github/workflows/build-and-test.yml` (4 jobs, legacy-authoritative) | OS-matrix `build-test` + `version-sweep` + temporary `legacy-oracle` | Greenfield-authoritative, cross-platform NIST, characterization gated. |
| delete (G8) | `tests/CobolSharp.Tests.Unit`, `tests/CobolSharp.Tests.Integration` | — | Frozen legacy retired after the bake. |
| delete (G8) | `scripts/guard.sh`, `guard-fast.sh`, `guard-run-group.sh`, `guard-verify.sh`, `compliance.sh`, `nist-batch.sh`, `run-suite.sh` | — | All exist to run/parallelize the legacy NIST loop or legacy dashboards — dead once NIST runs in-process. |
| delete | `CobolParserJsonXml.g4`, `CobolExtensionsJsonXml.g4` (dead JSON/XML) | — | Non-ISO (0 spec occurrences) — hard invariant #5; remove from any test/build surface. |
| move | legacy `ConformanceTests` corpus + `GreenfieldOnly`/`LegacyDivergent` sets | folded into `CorpusRunnerTests` | One corpus runner over committed goldens. |

---

## 5. Migration notes — keeping the battery green through the phases

The rearchitecture proceeds so the net is STRENGTHENED before it is relied on, then legacy is severed:

- **Phase R0 — Net-first (do BEFORE any refactor).**
  1. Land `corpus.tsv` + `CorpusManifestTests`; repoint `NistDifferentialTests` at it (behavior identical, coverage
     unchanged — pure de-duplication). 2. Stand up `Cobol.Net.Tests.Characterization` and SEED `Snapshots/` from the
     CURRENT emitter (captures "today's behavior"). 3. Cache the Roslyn reference set (safe, pure speed). 4. Land the
     diagnostic registry + `DiagnosticSnapshotTests` seeded from today's output. After R0 the battery can prove
     behavior-neutrality; no compiler behavior changed.
- **Phase R1 — Bake the oracle.** Run `DifferentialBakeTool`, commit `tests/differential/**/*.out`, rewrite the
  differential base to golden comparison, DELETE the `cobolnet==legacy` asserts. Keep the legacy test projects and
  `guard-fast.sh` in CI as a `legacy-oracle` job proving the goldens still equal a live legacy run. The net is now
  self-standing but still cross-checked.
- **Phases R2…Rn — the actual rearchitecture** (god-class splits, pass pipeline, storage-form unification, per the
  sibling DESIGN-* docs). EACH phase: run the full battery; gates (1)+(2) MUST stay green; gate (3) stays green or
  is reviewed-re-baselined IN THE SAME change set (with a DEVLOG note citing the intended emit change and the
  gate-(1) proof). No phase merges red. Small phases (feedback_tiered_gates) so a snapshot diff is legible.
- **Phase G8 — Sever legacy.** Once R1's `legacy-oracle` job has stayed green across the rearch, delete the legacy
  test projects, the legacy `ProjectReference`s, the legacy guard scripts, and the `legacy-oracle` CI job. The
  `build-test` matrix is now the whole gate, cross-platform.

Throughout: `Generated/` remains a build output (regen per checkout, failed regen fails the build); warnings-as-
errors stays on the Release/CI build; the drift tests (`ConstructRegistry`, `ReservedWords`, `CorpusManifest`,
`DiagnosticRegistry`) are the "nothing silently added/dropped" backstop.

---

## 6. Risks

1. **Snapshot brittleness.** Emitted-C# snapshots (gate 3) will diff on almost every emitter refactor. MITIGATION:
   gate (3) is advisory/reviewed, never a hard CI red on its own IF gates (1)+(2) are green — but a gate-(3) diff
   with NO corresponding source change in the PR IS a red (unexpected drift). Keep the characterization corpus small
   and representative (one program per feature family), not the whole NIST set.
2. **Bake faithfulness.** If the bake captures a legacy output that WiseOwl COBOL already diverges from (an
   intended ISO fix), the golden would wrongly pin the legacy value. MITIGATION: bake only cases where the
   differential test is currently GREEN (cobolnet already == legacy); a currently-red/skip differential case is
   hand-authored to the ISO value and marked `divergent`.
3. **Two runtimes side-by-side until G8.** The conformance project references both runtimes; a program loads only
   its own. Low risk (proven today) but the characterization/probe path must use the greenfield driver ONLY.
4. **`corpus.tsv` chain semantics** must exactly reproduce `chains.tsv` + `guard.sh` ordering. MITIGATION: a
   one-time `guard-verify`-style diff proving the manifest-driven run matches the current verdict list before
   deleting the old sources.
5. **Diagnostic registry churn** touches ~161 call sites. MITIGATION: mechanical, one code family at a time, each
   change set battery-green; the drift test catches any orphaned/duplicate code.
6. **CI time** could rise if characterization emits Roslyn-compile every program. MITIGATION: gate (3) snapshots the
   emitter STRING only (no Roslyn); the reference-set cache offsets the rest.

---

## 7. Open questions for the owner

1. **Snapshot tooling:** hand-rolled `Assert.Equal(File)` + `COBOLNET_UPDATE_SNAPSHOTS` env, or adopt Verify
   (`Verify.Xunit`)? Verify gives received/verified diffing + review workflow for free but adds a dependency. Lean:
   hand-rolled (zero new deps, matches the repo's existing golden-file idiom) unless you want the Verify UX.
2. **Characterization corpus size:** one program per feature family (~120 programs, fast) vs. snapshot the full NIST
   corpus's emitted C# (maximal coverage, slower, noisier diffs). Recommend the curated family set for gate (3), NIST
   goldens already cover gate (1) breadth.
3. **When to sever legacy (G8 timing):** delete the legacy oracle the moment R1's bake lands and `legacy-oracle`
   goes green once, or keep it running through the WHOLE rearch as a live cross-check (costs one CI job)? Recommend
   keeping it through the rearch (cheap insurance), delete at true G8.
4. **`--suppress` granularity:** per-code, per-family, or per-descriptor `SuppressKey`? Affects the registry shape;
   default proposed is per-code with an optional family key.
5. **Migration-SSOT vs resume-prompt.md ownership — RESOLVED (2026-07-07):** the migration roadmap is the standalone
   `docs/COBOLNET_REARCHITECTURE_PLAN.md` (since 2026-07-19 THE ONE consolidated plan incl. the §0 live banner);
   the feature-drive state ALSO lives there now — `resume-prompt.md` was absorbed into it and DELETED (2026-07-19).
6. **Do we snapshot the runtime deploy?** The runtime DLL is copied per emit; characterization ignores it. Confirm
   the runtime is out of the neutrality scope (it is typed-native and separately unit-tested) — assumed yes.
