## Entry 1769 — 2026-09-28 20:07 PDT — PB1708 pivot designed: the gate runs everything, in order; the continuity wall is one lexer lock

**Why a pivot.** PB1708's first design made the recorded impact map SELECT an implementer's tests, precisely
enough to be worth it: restructured registries, entry-level dependencies (map schema 3, static-field probes),
per-commit maps, a source-generated construct registry. Two adversarial reviews rejected it — seven BLOCKING
findings, then two still open plus three new — and each cycle found another way this single-process, memoizing
compiler hides a dependency from any recorder (values memoized into static collections; edition state cached in the
front end). Under selection every such hole silently skips a failing test. The owner chose "Pivot: order, don't
skip" (kb/Work PB1708, fourth decision). The rejected branches are `worktree-wf_2a880f25-d10-1` and `-d10-3`.

**The design** (`DESIGN-test-build-ci.md` §3.13 rewritten, §3.14 new). Every implementer gate runs the whole
Conformance, Unit and Characterization population in two legs: leg 1 = tier 0 (new tests, tests in changed test
files, last gate's reds) plus the cheapest tier-1 tests within 2 % of the recorded test-seconds; leg 2 = the rest,
longest collection first. A test's leg is a total function of its display name and one plan file, so the legs can
neither overlap nor drop a test, and every gate checks it: the union of the legs' trx results must equal
`dotnet test --list-tests` as a multiset, and every leg must have read the same plan digest. The gate stops at the
first red leg and reports RED/INCOMPLETE; GREEN exists only when every leg ran. No plan input means one leg in the
plain order — never a skip. The lander, the battery and CI are unchanged.

**How the runner decided the mechanism** (evidence `docs/rearchitecture/evidence/impact-map-pb1708/b3-order-probe`).
vstest filters were rejected: a leg of ~3,000 theory rows is ~400 KB of `DisplayName=` terms, beyond the Windows
command line; the complement leg is the De Morgan complement of the union; xunit truncates long arguments with
`···`; and while measuring, `DisplayName~edition: 2023` (a value with a space) matched NOTHING and exited 0 — the
silent green the filter guard exists for. xunit's `StopOnFail` was rejected after it CRASHED the test host
(`OperationCanceledException`) and the trx lost the red. Assembly-level collection and case orderers ARE honoured
under parallel collections, so the leg filter lives inside the test assemblies (`tests/_shared/GateLegs.cs`, a
`GateTestFramework` the recording framework derives from), keyed on objects, not strings.

**What orders a leg: cost, more than the map** (`b4`). On 68b X — the one dropped branch with named reds — 9,144 of
9,190 tests share tier 1. Ordering by map SPECIFICITY put the first red after 62 % of the assembly's work, worse
than the plain order (14 %); cheapest-first put it after 0.32 %. The map keeps the tier boundary; PB1709
(per-commit maps, ~19 min each) becomes an open owner question, and PB1712 (the map's blindness to static field
reads) can no longer skip a test — it now costs ordering only, and the note says so.

**Where the whole suite's time goes** (`b1`, battery #87, uninstrumented): 571 s wall, and the twelve continuity
partitions ARE the critical path (525–557 s each). Measured as the gate runs them (`b6`): one partition alone 0.97 s
per row; all twelve in one process 369–395 s at ~3.8 busy cores; server GC 360 s; two concurrent processes 209 s,
four 136 s — the limit is inside one process. An in-process probe of the exact check-only work (`b2-scaling`)
peaks at 3.5× by 8 threads with ~1,400 contended Monitor acquisitions per compile, and stack samples put 28 of 30
single-thread samples and all 24 twelve-thread stacks in ANTLR's `LexerATNSimulator.ComputeStartState` /
`AddDFAState`: six LEFT-EDGE predicates in `CobolLexer.g4`'s default mode keep ANTLR from ever caching that mode's
start state, so every token of every compilation recomputes the closure over all default-mode rules and re-adds it
under `lock (dfa.states)`. That is a product defect (the CLI pays it serially); M6 fixes it at the grammar with a
drift test that every mode's start state is cached. Sharing one front end across editions stays rejected (edition
state is cached in the front end), and process sharding is not built — it only routes around the lock. §3.11's
"class-split lever exhausted" was measured under this lock and is re-opened after M6.

**A new defect (note-ready; the id is central).** The long poles `pb505-table-value-dynamic-span-levels` (266 s in the
battery) and `ValueFormat2Tests.DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) are one: the 26-line
program takes 29.6 s through the `cobol` CLI, and every stack sample sits in `TableValueOdometer.Resolve`
(`src/Cobol.Net.Compiler/Binding/Model/TableValuePlan.cs`), whose fill loop is bounded only by `MaxFillElements`
(64,000,000) and runs to it when the odometer cannot carry out of a DYNAMIC dimension with no TO — the §13.18.63.3
SR23 violation COBOLNET1946 reports afterwards. Harm: compile time (a legal-looking but rejected program hangs the
compiler for half a minute), no wrong answer. Design M7.

**Evidence reproducibility.** The evidence directory from `-d10-3` is in the tree with a README naming every input
that is not in the repository (the two recorded maps, `a1.pkl`, raw recording contexts, a battery trx, the host,
`dotnet-stack`); `a1.txt` and `a5.txt` were re-generated from the repository and match (a5 minus the two scratchpad
warning lines it used to carry, from a positional `maxsplit` now fixed).

**Implementation plan** (§3.14.9): M6 lexer start state · M7 odometer · M11 order plan (tiers; the filter output
deleted) · M12 in-assembly leg filter + orderers · M2 cross-worktree gate cap (retires when a cold whole-Conformance
leg at BelowNormal is under 6 min over a train) · M13 the ordered gate (driver, population check, fail-fast,
`-Filter` removed from `build-local` and every caller). M6, M7, M11, M2 are independent; M13 lands last with M2.
No production code changed.

