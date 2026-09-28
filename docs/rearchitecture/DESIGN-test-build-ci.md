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
`windows-conformance` (windows, the same 6 shards, Release) · `conformance-population` (the shard-sum guard) ·
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
and for the drift gate, exactly as the `guard-fast` regrouping was kept for correctness rather than speed.

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

### 3.13 THE PER-TEST IMPACT MAP — deriving the implementer's gate filter (kb/Work PB1683)

**The defect it answers.** An implementer's gate ran the tests its author NAMED (its goldens, `~Drift|~EditionGate`,
`~CorpusRunner|~Nist` on a "shared seam"). Train 68b dropped two groups on whole-assembly reds those names never
reached — `DiagnosticPositionTests` after a COPY library-search change, and a `CONSTANT AS NULL` crash in
`DataBinder.Constants.cs` — and wave 69 (four agents, 1.17 M tokens) existed only to finish them. The map records
the fact the names were guessing: which tests execute which code.

**Collection — a per-test hit recorder, chosen by measurement.** Per-test coverage from a coverage tool cannot
attribute hits under xunit's parallel collections, and per-shard coverage bisected to tests multiplies the run.
The recorder instead rides the test run itself, in its own RECORDING build, and costs one cold Conformance run:

| piece | what it does |
|---|---|
| `tools/impact/ImpactRecording.targets` | imported ONLY through `-p:CustomAfterMicrosoftCommonTargets=…` (nothing in the tree imports it, so no gated, battery or shipped build carries a probe); compiles `ImpactProbe.cs` into the greenfield compiler, front end, editions, runtime and CLI, the legacy oracle the differential harness loads, and the three test assemblies; compiles `ImpactTestFramework.cs` into the test assemblies and names it in `[assembly: Xunit.TestFramework]` |
| `tools/impact/ImpactInstrumenter` (Mono.Cecil) | rewrites each covered assembly once: every method with sequence points calls `ImpactProbe.Hit(id)` on entry, `id` being its entry in the probe table — its file and the LINE RANGES it owns (its sequence points, merged across the lines between two of them unless another method's point lies between, so braces, `else` lines and comments belong to it and a lambda's body to the lambda); members with no sequence points are charged to a TYPE-LEVEL entry; `implies` names each entry's type-chain static constructors; every `Process.Start` is redirected to the probe's twin |
| `ImpactProbe` | one byte store per hit into the array of the running CONTEXT, an `AsyncLocal` every assembly's copy shares through `AppContext` data; a hit with no context lands in an AMBIENT array. In a child process (a compiled COBOL program loading the instrumented runtime) it records for the process and writes a hits file at exit, which the redirected `Process.Start` queued on the starting test |
| `ImpactTestFramework` | xunit's own framework with the message bus wrapped: `ITestCollectionStarting` / `ITestClassStarting` / `ITestStarting` install a fresh context synchronously on the flow about to run it (the same `TestRunner.RunAsync` then awaits the test, so the context flows into constructor, body and every task it starts, while parallel tests keep their own); `…Finished` saves it with the children folded in. A `BeforeAfterTestAttribute` cannot do this — it is handed only the `MethodInfo`, which cannot tell a theory's rows apart, and the corpus theories are ~3,400 of the ~9,300 Conformance tests |
| `scripts/spec/record_impact_map.py` | a DETACHED worktree at the commit, the recording build, the instrumenter, each test assembly once with `COBOLNET_COMPILE_CACHE=off` (a cache hit skips the compiler and would record nothing for it), and the merge into `<git common dir>/cobol-impact/<sha>.json.gz` — shared by every worktree of the repository. Its watchdog fails the recording when a listed test was never recorded or no test reached the compiler, front end or runtime |
| `scripts/spec/impacted_tests.py` | the lookup: `--base <cut point>` (or explicit files, taken at the file level) → the filter, its last stdout line |

**Measured cost.** At `dbea12428`, `BelowNormal` on the shared 32-core host: 35,578 probe entries; Conformance
9,274 green in 15 m 19 s (cold, instrumented), Unit 29,693 green in 2 m 38 s, Characterization 33 in 2 s; build
23 s, instrumentation 4 s; **18.7 min in all** — inside one cold Conformance leg of the battery, which is where it
runs. The map is ~5 MB compressed.

**How a change selects.** A hunk inside a method's owned lines selects the tests that executed that method (a
static constructor's change, every test that touched its type). A comment-or-blank hunk selects nothing. ADDED
code between members: an ordinary new method runs only when changed code calls it, except through overload
resolution, so it selects the tests that reached any same-named method of the file's types; an override, virtual,
operator, conversion, extension method, field, property, constant, a type with a base list or an attribute is a
DECLARATION change. A declaration change — or any removed or changed line no method owns (a signature, a
constant, an enum value) — selects every test that executed the file, plus every test that executed a file NAMING
one of its outermost types (constants and enum values are compiled INTO their consumers). A theory row is
selected by a `DisplayName~<its name argument>` term, a whole method or class when the term budget (150) requires.

**⛔ Conservative by construction — the WHOLE assembly, and the reason, when it cannot bound the change:** no map
for the base, or one older than it by anything but documentation (the line numbers are the map commit's); a
`.g4`, project, build or source-generator input; a NEW src file; a declarations-only file no executed file names;
non-C# data under `src/`; a changed method reached only outside every test's context in product code (a
finalizer; in a test file that is xunit discovery running a theory's data source, and the file's classes are
selected instead); corpus data no golden names. `ImpactedTestsDriftTests` (Unit) drives every arm through the
script's `--self-test`, proves through the real command line that an unmapped file prints `FullyQualifiedName~.`,
and requires every product project under `src/` to be in both the targets file and the recorder's `PROBED` list.

**Holes it does not close (the lander's whole-assembly gate is their net):** a static cache built by one test
from a method a later test never re-executes; a default-parameter or attribute change consumed by code that does
not name the declaring type; a new type discovered by reflection with no base list.

**What the replay measured — the map is a CORRECTNESS gate, not a narrowing one, for this compiler.** Every
golden runs the whole pipeline, so a change to the preprocessor, binder or emitter reaches almost every test.
FILE granularity selected the whole Conformance assembly for 20 of 22 branches of trains 65–69 and the two
dropped 68b branches; METHOD granularity (the design above) still reaches 5,504–9,268 of 9,274 tests per train
cluster (each cluster bundles three to five notes and 15–86 files), so every replayed cluster's filter is the whole
assembly. For the two dropped 68b branches the map, recorded at their own base `4b0f3e22a`, selects the tests that
went red. The derived filter is therefore NARROWER than the whole assembly only for a small, local change; its
value is that the implementer's gate now runs every test the change can reach, which the name-guessed filter did
not. The implementer gate cost that follows (a whole cold Conformance leg per implementer, which MANDATORY-PRACTICES
I2 forbade for lander contention) is an owner decision recorded in kb/Work PB1683.

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
