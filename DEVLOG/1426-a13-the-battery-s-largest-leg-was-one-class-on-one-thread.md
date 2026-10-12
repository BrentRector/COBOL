## Entry 1426 — 2026-09-02 16:21 PDT — A13: the battery's largest leg was ONE class on ONE thread; split twelve ways it bought 17%, not the 10× predicted — plus PB287, a CI filter that would have reported success over ZERO NIST goldens

`kb/Work/A13` has been open since 2026-08-03 with a measurement and a prediction. The measurement was right and
the prediction was wrong, and the second half is the more useful entry.

**The measurement.** xUnit 2.9.2 parallelizes at TEST-COLLECTION granularity, and by default each test CLASS is
one collection — so every test in a class, *including every row of a `[Theory]`*, runs serially on one thread.
On the battery #41 trx the Conformance leg ran 5,241 tests in **721 s wall for 1,948 s of test time (2.7×
average concurrency)**, of which `VersionMatrixTests` alone was **2,127 rows running serially for 720.5 s** —
essentially the whole leg was one class on one thread. The Unit leg was **171 s wall / 329 s test time (1.9×)**,
and one class, `StorageFormEquivalenceTests`, *was* that wall clock: a single `[Fact]` looping the NIST corpus
for 171.5 s.

**The mechanism, designed for the next case rather than these four.** `tests/_shared/TestPartitioning.cs`
(linked into every project under `tests/` by `tests/Directory.Build.props`). xUnit v2 offers exactly two levers
and only one can split — `[Collection]` MERGES classes into a shared collection, so it can never divide one —
which leaves genuine class splits, and the naive class split DUPLICATES THE TEST BODIES, which is how a split
rots. So the tests live ONCE in an abstract generic base `Family<TSlot>`, its `[MemberData]` sources slice the
full row set `index % Partitions` (a STRIDE, not a block: adjacent rows have correlated cost), and each
partition is a one-line `sealed class Family_P3 : FamilyBase<Slot3>;` that xUnit schedules as its own
collection. It rests on static members of a CLOSED generic type being per-type-argument and on xUnit resolving
`[MemberData]` against `testMethod.DeclaringType` — which for a method inherited from `FamilyBase<Slot3>` is the
*closed* type. That was not assumed: it was proved with a standalone probe on xunit 2.9.2 /
xunit.runner.visualstudio 2.8.2 BEFORE any real class was touched (3 partitions over 9 rows → 9 tests, 3 per
partition, each asserting its own slot; open-type resolution would have given 27 tests and 18 failures).
Design: `DESIGN-test-build-ci.md` §3.11.

**The invariant, and a gate that was proved failing.** A partitioned family FAILS OPEN — delete one partition
class and the rows it owned are simply never run, with a green leg and a smaller but entirely plausible test
count. `TestPartitionAudit` (`TestPartitionCoverageDriftTests`, one per assembly) closes that, and it is
SHAPE-DRIVEN rather than registered: it discovers a family by structure (a top-level, author-written, abstract
generic base with one type parameter constrained to `ITestPartitionSlot`), so a new family is covered the moment
it is written. Four checks — an empty row source, slot indices ≠ {0…Partitions−1}, an EMPTY partition over a
non-empty source, and union ≠ source as a MULTISET — plus a central slot-ladder check, because a copy-pasted
`Slot9.Index => 8` would corrupt every family at once. ⭐ It went RED on its FIRST real run before any deliberate
break: Roslyn emits a nested `<>O` delegate-cache class inside any generic type that caches a lambda, and a
nested type inherits its enclosing type's generic parameter *with its constraints*, so every family produced a
phantom family with no `Partitions` const. Both structural checks were then fired deliberately — a partition
class commented out gave the slot-set violation plus `1047 rows … 87 row(s) NEVER RUN. First missing: IC108A␟2023`,
naming the dropped rows; a slice count desynced from the const is caught earlier still, by `Slice`'s own range
guard.

**What was partitioned, and what the landing re-measured rather than inherited.** Version matrix **12**, NIST
differential **6**, corpus runner **3**, StorageForm NIST sweep **8**. Re-counted here by discovery, not taken
from the implementer's report: `VersionMatrixTests_P0…_P11` at 179·179·178·177·177·177·177·177·177·176·176·175
= 2,125 rows, plus the 2 whole-catalogue `[Fact]`s left deliberately off the partitioned base = **2,127**;
`NistDifferentialTests_P0…_P5` at 59+58×5 = **349**; `CorpusRunnerTests_P0…_P2` at 334+334+332 plus the 5
manifest-integrity rows = **1,005**. Assembly discovery totals **5,242** (Conformance) and **5,151** (Unit).
The StorageForm sweep is the one deliberate count change, 6 → 13, and it KEEPS its population assertion: the
whole-corpus bar was `parsed >= 50`, and each partition now asserts its proportional share rounded UP, so a
partition that silently stopped binding goes red rather than green-and-empty.

**⛔ THE TAIL IS GONE AND THE WALL BARELY MOVED — and the second half of that sentence is the finding.** A13
predicted 783 s → ~80 s. Measured: Conformance **721 s → 600 s (−17%)**, concurrency **2.7× → 17.4×**; Unit
**171 s → 138 s (−19%)**, **1.9× → 6.5×**. The split did exactly what it was designed to do; the premise was
wrong. `sum-of-test-time ÷ wall` measures COLLECTION concurrency, **not core utilisation**, and the 31 "idle"
cores were never idle — one COBOL.NET compile is internally parallel (Roslyn `Emit`) and a NIST row spawns a
`dotnet` child that is too. The proof is inside the after-profile: the SAME work now reports **10,469 s of test
time instead of 1,948 s (5.4×)** because the new collections found contention, not silicon. At 17.4× + 6.5× ≈ 24
threads on 24 physical cores the box is saturated, so **the class-split lever is EXHAUSTED** and the remaining
lever is reducing the WORK — a persistent compile/run host (A13(c)①, the same conclusion the guard's 40→9
regrouping reached). `kb/Work/A13` therefore goes to **`half`**, not `landed`: its stated acceptance bar (no
collection above ~90 s) is measurably not met and cannot be met by this lever. That is now twice in this
register that a concurrency change was kept for correctness rather than speed, and it is worth naming as a
pattern rather than re-learning a third time.

**⛔ AND IT FOUND A LIVE CI DEFECT — `kb/Work/PB287`, and it is the reason this could not have been a quiet
refactor.** A partition class is `VersionMatrixTests_P0`, so `~VersionMatrixTests` still selects it and
`!~VersionMatrixTests` still excludes it — but a `Class.Method` filter can never match, and **vstest answers a
filter that matches nothing with a SILENT GREEN**. Four filters were keyed that way: the three continuity shards
(in both platform matrices) and the **INV-1-STRONG** job, `~NistDifferentialTests.NistProgram_MatchesGolden`.
The shards would eventually have been caught by the shard-population job; INV-1-STRONG has **no population guard
at all** and would have gone on printing a green "NIST goldens byte-exact" verdict over **zero** of the 349
goldens. Proved here rather than argued — the old filter, run against the split tree:

```
No test matches the given testcase filter `FullyQualifiedName~NistDifferentialTests.NistProgram_MatchesGolden`
OLD FILTER EXIT CODE = 0
```

…and the new one, `~NistProgram_MatchesGolden`, discovers 349. All four now key on the method name alone, which
is unique in the tree and immune to the next re-partition — the class half is the half that moves. CI's
shard-population arithmetic was re-derived per shard against the split tree, not copied: 349 · 349 · 349 · 1080
· 1354 · 1761 = **5,242 = discovered**. This is `feedback_vstest_filter_terms` in a new dress and
`feedback_green_gates_arent_evidence` in the old one: a green gate proves nothing when it never looked at what
it claims to cover.

**A citation cluster found here and deliberately NOT repaired here — handed to `kb/Work/PB159`.** Rule 1 forces
a `--check` on every citation a change set carries, so the one ISO reference in these files was checked, and it
failed: `cite.py --check 14.9 "MOVE"` → *"§14.9 exists but does NOT contain 'MOVE'"*. §14.9 is the parent
*Statements* clause; the whole-group MOVE rule is **§14.9.25.4 GR4**, which `--check` confirms verbatim. The
rule NUMBER was right and the CLAUSE was never re-derived — and it had propagated to **19 sites** across nine
`src/` files, two test files and four design docs, while **68 files already carry the correct §14.9.25.4**. The
repair belongs to PB159's sweep, which owns this whole class: splitting one citation cluster across two change
sets is how a sweep loses its census. The finding is appended to PB159 with the site list, both spellings, the
`--check` lines, the discriminator for the repair (a bare `§14.9` that genuinely means *Statements* — "§14.9 has
no ALTER statement", "§14.9 I-O" — is CORRECT and must be left alone), and one neighbour citation
(`BinderDriver.cs:544`'s `§14.2.3 GR8`) named as unchecked rather than checked.

**What the landing corrected in the incoming work.** Two things, both small and both real: the new §3.11
appended a SECOND `---` rule before §4 of `DESIGN-test-build-ci.md`; and plan §5b's step-1 row still read "A13
(b) is unmeasured", which this change set makes false — it now records the split and points at §3.11 for why
the note stays `half`. Plan §0's Gates bullet and §3's tiered-testing bullet carry the new durations, checked
against the after-profile rather than the report.

**Gate (this worktree, rebased onto `5fe593a0`; the implementer's tree was cut from `2acbd842` and three commits
had landed since).** Solution build clean, 0 warnings. Conformance filtered
`~TestPartitionCoverage|~VersionMatrixTests_P0|~NistDifferentialTests_P0|~CorpusRunnerTests_P0` →
**573/573 passed**. Unit filtered `~TestPartitionCoverage|~StorageForm|~SpecTraceabilityInventoryDriftTests` →
**28/28 passed** — `SpecTraceabilityInventoryDriftTests` was added to the filter deliberately, because dozens of
inventory rows carry `conformance-test:CorpusRunnerTests.EnabledNegativeCase_…` and
`conformance-test:VersionMatrixTests.Construct_…` test-refs and the class renames could have broken their
resolution; they resolve by FILE name plus method text, so they still hold. Characterization **33/33**.
`profile-test-parallelism.py` on the Unit trx shows all eight `StorageFormNistEquivalenceTests_P*` running
together — 28 tests, 514 s of test time in 91 s wall, 5.7×. ⚠ The two `ExternalCorpusPopulationDriftTests` reds
the implementer reported are outside this filter and are environmental: they need the git-ignored
`tests/external/gnucobol/` corpus, absent from any fresh worktree by the PB277 licensing doctrine.

⚠ **`python scripts/semgrep/verify.py` is RED, and it is not this change.** `cobolnet-no-biginteger` 4 → 36,
`cobolnet-raw-diagnostic-code-literal` 423 → 475 — already registered as `kb/Work/PB175`, whose numbers this run
updates (they were 34 and 472 when it was written). Attributed mechanically, not by assertion: `verify.py` scans
only the five `src/Cobol.Net.*` projects and this change set touches **no `src/` file at all**. Two more waves
have now landed on top of a red check that sits in no gate, which is exactly what PB175's step 2 asks for.

