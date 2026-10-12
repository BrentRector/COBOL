## Entry 1911 — 2026-10-07 03:57 PDT — Train 1028: the solution is Cobol.Net.sln, the docs describe a compiler without the legacy engine, and the grammar's legacy-forced copies are one rule each; PB2112, PB2111, PB2114

Wave 1028's three legacy-retirement items land as one train (lander worktree `wf_f31b7069-6ff-5`, clusters in order
B, A, D, each its own commit, on `db070acb5`, after train 1026). The train changes no behavior, and the proof is
direct: the R0 oracle captured on the merged tree is IDENTICAL, case for case, to the recorded baseline `f620ccf6d43d`
(7,357 cases of emitted C# and diagnostic streams), so cluster D's grammar unification moved no emitted code and no
diagnostic.

**B — PB2112: the solution file is `Cobol.Net.sln`.** `git mv` and a byte-exact token rename in 228 files: the gate
driver (`run_gate_legs.py`), `linux-gate.sh`, `battery.sh`, `record_impact_map.py`, two hooks, the CI Release build,
`TestRepo.cs`'s repo-root marker and its drift test, the Benchmarks and impact-recording projects, the skills and
briefs, CLAUDE.md, README, CONTRIBUTING, the docs and the kb/Work notes. No alias: one `.sln` is tracked.
`LEGACY-ARCHIVE.md` keeps `CobolSharp.sln`, because that is the solution's name AT the archive tag, and so do the
frozen evidence records. The lander found the rename incomplete on the merged tree: train 1026 had landed after the
cluster's base, and its census, performance-baseline and oracle files named the old solution (`scripts/arch/census.py`
reads and builds it, and `ArchCensusDriftTests` opens it through `TestRepo.At`, so that test would have failed on the
first run). They were renamed in this cluster's commit: `census.py`, `perf_baseline.py`, `ArchCensusDriftTests.cs`,
the ArchOracle and ArchCensus project files, `Directory.Packages.props`, `DOC_INDEX.md`'s census row, PB2115 and PB2131.
The cluster's own `DOC_INDEX.md` row for `LEGACY-ARCHIVE.md` had said the tag recipe builds `Cobol.Net.sln`, which is
wrong at the tag; cluster A's wording ("builds the solution at the tag") replaced it. The tag vocabulary and this
log's title `CobolSharp Developer Log` keep their names (the register's and the log's own).

**A — PB2111: the docs, register and comments describe a compiler without the legacy engine.** Plan PHASE-15 reads
DONE for Cuts 1 and 2 with their landing shas; PHASE-14's equivalence proof, ledger and greenfield-guard steps are
marked obsolete; §0's gate commands, §3's execution model and the D1/D10 rows stop naming the legacy suites.
`COBOLNET_DESIGN.md` §17 and G8, the project-org, frontend-grammar, module-topology and test-build-ci designs replace
their migration recipes with present-tense summaries; dated scouts, surveys and ledgers keep their evidence with the
deleted paths relabelled. The skills, the adjudicator agent and the vault notes are corrected; 19 open notes lose
deleted code sites; PB166 and PB717 retire as moot; 81 source and test files' comments are rewritten (every `.g4`
edit is a comment line). PB1918 lands with it: PB2110 had deleted the Frontend's stale `InternalsVisibleTo` and left
none, so `Cobol.Net.Tests.Unit` is granted, as the other four projects grant it. In the merge the cluster's plan edit
had dropped the header line of the next standing caution ("A QUEUE ENTRY'S 'ROOT CAUSE, ALREADY LOCATED' IS A
CLAIM") along with the deleted additive-grammar caution; cluster D's side of the same hunk restored it. Its
`DOC_INDEX.md` row for the architecture review also dropped "the oracle" from the preconditions; the lander kept
main's row (the R0 oracle is PB2116) and applied only the scope change.

**D — PB2114: the grammar's legacy-forced copies are one rule each.** The sending operand `{identifier | literal}`,
written out nine times while the deleted legacy compiler read each copy's accessors by name, is `sendingOperand`, bound
once by `MoveBinder.SendingOperand` (MOVE, the WRITE/REWRITE/RELEASE FROM phrases, INITIALIZE REPLACING after its NULL
arm) and read by every INSPECT operand. ADD TO, SUBTRACT FROM and DIVIDE INTO share `receiversOrSendingOperand`, and
the four GIVING phrases are `arithmeticGivingPhrase`. `ArithmeticOperandRole.MixedRules` is two rules.
`SendingOperandDriftTests` (planted-copy detector rows included) replaces `ArithmeticSendingOperandDriftTests` and
fails on any rule that writes the set out again. INSPECT lost a dead `figurativeConstant` lookup, and
`BindFromPhrase`'s `dref!` became a named refusal. Plan §0's "A SHARED-GRAMMAR CHANGE MUST BE ADDITIVE UNTIL P15" caution
is deleted (four cautions are three) and DESIGN-frontend-grammar §3.15 records the shape rules. The implementer proved
neutrality with a token-stream and parse-tree differential over 20,494 lexer inputs (0 differences), and the lander's
oracle run above confirms it at the emitted-code level. Two copies are kept by measurement: DISPLAY, where naming the
rule changed 83 error-recovery trees (PB2143), and MULTIPLY's `multiplyByOperand+`, whose fold would change what
parses (PB2144). Batch `pb2114-batch.json`: five rows changed (eight code locations added, eight retired witnesses), GAP unchanged. Citations
re-checked by the lander: §8.4.3.2.3 1), §8.4.3.4.3 1), §14.9.20.3 4).

**The train.** Gate `pwsh scripts/build-local.ps1 -Mode lander` (whole population, one leg): the first run
(`20261007T105109Z-5ed38a`) ran every case green (Conformance 10,720/10,720 · Unit 31,653/31,653 · Characterization
35/35) and was RED only on the drift-rules index: the lander's sed rename had shortened one truncated summary in the
generated `docs/DRIFT_RULES.md`, which `drift_rules.py` regenerated into cluster B's commit. The re-gate
(`20261007T110050Z-52e58c`) is GREEN with every population exact, and the Linux gate is GREEN (hooks, unit,
characterization, conformance, guard). Semgrep verify PASS with no count changed; `work.py check` clean;
`audit_code_citations`, `audit_doc_citations`, `audit_evidence_supersession`, `audit_witness_loss` (0 unexcused, 8
retired) and the rule catalog green. GAP 173 → 173. Nothing dropped. Leads filed: PB2143 (DISPLAY's inline set),
PB2144 (ADD/SUBTRACT/DIVIDE give a raw parse error where MULTIPLY names COBOLNET1689), PB2145 (`invokeArgument` writes
the call-by bodies twice), PB2146 (the identifier-only sending shape is written three times, plus a dead
`figurativeConstant` arm in `strUnstrOperand`), PB2147 (a grammar change that removes a spelling is never swept
against the corpus: PB717's root, un-owned since PB717 retired); `CopyProcessor.Process`'s missing product caller is
recorded on PB2119. PB1953 recurred in all three implementer gates (8 `UniversalCrossingShapeDriftTests` rows NEVER
RAN in leg 2), recorded on its note.

