## PB3176 — the conformance suite's in-process runner (slice S1c): WiseOwl COBOL runs the suite on itself, and both runners agree

kb/Work PB3176, design `docs/rearchitecture/DESIGN-conformance-suite.md` §5 ("In WiseOwl's own pipeline"), §9 and
§13 S1c (approved by the owner 2026-10-14). Wave 1104 C5, on PB3175's branch, so PB3172 through PB3175 land with it.

**Re-probe.** Greenfield slice: no test of `tests/Cobol.Net.Tests.Conformance` read a generator manifest, and
`run.py` was the suite's only runner. The build was 0 warnings, 0 errors before any change.

**The runner.** `ConformanceSuiteTests` has one theory row per test and edition of the generator's `wiseowl`
manifest (both trees, portable or not) and one per refusal and edition of `wiseowl/diagnostics.json`.
`ConformanceSuiteRunner` judges a test as `run.py` does through the WiseOwl adapter (PASS, FAIL, NOT APPLICABLE, NO
VERDICT, §5), compiling with `CompilerDriver` in-process through the compiled-program cache: an accepted test
compiles, runs in a fresh working directory with its declared files and stdin closed, and its normalized stdout is
compared with the expected output for the profile's Annex A.1 values; a flagged test's control compiles first, then
the test compiles with the §4.2.10 warning mechanism invoked, and a diagnostic must name the marked line, read with the
adapter's own pattern. A row is red on FAIL or NO VERDICT, as `run.py` is with no baseline. A refusal must be refused
by the front end or the binder (never the backend) with its code. `ConformanceSuite` generates the manifests once per
test process into its own directory (`generate.py --out`, new), so the runner reads no test header and never
rewrites the repository's `suites/`; it reads the adapter's diagnostic pattern and the profile instead of restating
them (`config.py` validates both at the audit).

**The drift tests.** `ConformanceSuiteRunnerDriftTests`: (1) `run.py --compiler wiseowl --set cobol=<this build's
cobol>` and the in-process runner, on one generated suite, run the same tests and give each the same verdict, every
difference named with both reasons; (2) `CutRunner.Normalize` and `run.py`'s `normalize()` (run by Python) both map
the 16 cases of the new shared fixture `conformance-suite/harness/normalization-fixture.json` (every line end .NET
reads, the vertical tab it does not, tabs and no-break spaces against spaces, leading and trailing empty lines) to
their stated results; (3) the WiseOwl adapter's new `environment` pins both default locales to the root locale, as
`CutRunner.RunExit` does (`CutRunner.LocalePin`, now one constant): the defaults are the environment's
(DOC-A.1-118), so without the pin a host's regional setting could reach one runner's verdict and not the other's.
The Conformance project now builds `Cobol.Net.Cli` (a project reference with `ReferenceOutputAssembly="false"`),
because CI's Conformance legs build that project alone.

**Probes.** The 29 new rows pass locally (22 test rows, 4 refusal rows, 3 drift facts; the verdict drift test about
8 s). A mutation probe, one wrong line appended to `compute-numeric-receiver.out`, turned that test's four rows red
("the output differs ... at line 2: expected 'WRONG', got '<end of output>'") while both runners still agreed on
FAIL; reverted.

**Design corrections** (§4, §5, §13): the verdict drift test runs `run.py` itself, so every gate and both CI
operating systems run it, instead of a CI job of its own that no local gate would reproduce; a refusal's manifest
entry carries the refused test's declared files; the adapter's locale pin. Left to the move (S2, PB3177) by design:
partitioning the theories when the corpus arrives, and the verdict drift test's shape at the corpus's size, since
`run.py` starts one `cobol` process per compile.

**Self-review.** Architecture: the runner reads only the generator's manifests and the validated adapter and
profile; the verdict logic is stated once per language and held equal by the drift test. Silent failures: a refused
tree, a missing interpreter, an empty manifest (no sentinel row) and a compiler exception are each a red, never a
pass; NOT APPLICABLE is the profile's verdict, and a flagged test whose control fails is red on the control's own row.
Duplication: `CutRunner` is the one program runner and `EditionHarness.AssertHasDiagnostic` the one diagnostic
assertion; the locale pin became one constant. Performance: compiles go through the cache, so a re-gate recompiles
nothing; the theories are one class each until S2 partitions them.

No inventory row: test infrastructure (`closes_rows: []`, its reason filed). PB3177 (S2) unblocked.

**Gate:** pending.
