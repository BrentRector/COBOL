## Entry 1631 — 2026-09-22 05:42 PDT — Landing train 45: the printed format's repeated brace, one sentence at every operand, one float landing, one tape phrase and one definition-name namespace — five clusters, thirteen notes, GAP 2288 → 2276

**Five clusters, thirteen `kb/Work` notes to `landed`, no cluster dropped.** The train was brought in
from the five BRANCHES (never from an implementer's working tree), gated ONCE on this worktree's own
build, and landed through `scripts/push-main.sh`. Three of the five merges were clean; the three
conflicts were all WHOLE-ELEMENT additions to the same list, and each was resolved by UNION and then
MEASURED rather than assumed — the element count, the duplicate scan and the CRLF line count before
and after, plus a per-cluster `git diff --check` and `git grep --cached` read as OUTPUT, never as an
exit code.

**Cluster 1 — the printed format's repeated brace, and a group that had never had a verdict
(PB453 + PB563 + PB594).** This group arrived on its THIRD attempt: the two agents before it each
reported it finished on a gate that had died inside the Conformance leg, and it was held back from
train 44 for exactly that reason. It merged the whole predecessor chain onto train-44 main and
re-probed every claim on its own `cobol.exe`. §14.9.39.2 Format 7 repeats the BRACED receiving
operand, so the grammar now carries ONE production — `setAddressStatement : SET setAddressReceiver+
TO setAddressSender` — `BoundSetAddressOfBased` is deleted, `BoundSetPointer` carries
`IReadOnlyList<BoundPointerReceiver>`, and §14.9.39.4 GR12's "stored in each data item referenced by
identifier-5 in the order specified" and GR13 are ONE loop in `SetEmitter.EmitSetPointer`. All four
arms were checked and all four moved: greenfield `PtrBinder.BindSetAddress`, the legacy oracle
`DataStatementBinder.BindSetAddress`, the storage-forcing scan `PtrScanAddressOfTargets` and the
edition gate `VersionConformancePass.VisitSetAddressStatement`. The arity sweep was re-run over every
`set…Statement`: all carry `dataReference+` except Format 16, whose printed receiving operand IS one
operand. Then three defects were found INSIDE the group's own change set and repaired rather than
carried: batches 01 and 03 had SUBTRACTED inventory witnesses while their prose said they only added
— `record_verdicts.py` REPLACES a row's evidence rather than merging it — leaving a reference to a
conformance case that does not exist; four quotations attributed a DIAGNOSTIC's wording to
§14.9.39.3 SR8 and §8.8.1.1; and the restricted-data-pointer converse rule had been cited as SR20 in
six code sites and two goldens when it is SR19's third, unnumbered sentence (the rendered page, folio
736, shows 20) is Format 8's function-pointer rule). That last repair exposed a screen cell the
landing itself had opened and left unwitnessed — a plain restricted `identifier-6` standing beside an
`ADDRESS OF` receiver, a parse error before the receiving list became the printed repeated brace — so
a negative golden now pins it. All three load-bearing general formats were RE-RENDERED at 300 dpi
rather than inherited (folios 730, 516, 619). PB450 stays OPEN holding `SR-14.9.39.3-18` alone: its
residue is the OO cell/bridge emission, a different mechanism no note owns.

**Cluster 2 — one run law, one carrier, and one sentence at every operand (PB584 + PB664 + PB394).**
PB584: a mixed bit/character group's compile-time image was composed member-wise while its run-time
twin walked §8.5.1.6.3's runs, so declaring a REDEFINES alias over
`05 H1 PIC 1(4) USAGE BIT. 05 H2 PIC 1(4) USAGE BIT. 05 H3 PIC X(1).` silently changed what its
members held — measured `0x40 0x10` where the rule gives `0x41 0x42`. `BitLayout.RunsOf` → `BitRunMap`
is now THE run computation; `PhysicalModel.BuildPhysicals` owned a private copy and the compile-time
seed had NONE, so the answer to "which arm did I fix?" was BOTH, and the seed arm is the one that
never existed. The seed cannot simply walk `PhysicalChildrenOf` — `DataBinder.AssignClassOffsets`
marks every descendant of a redefines class non-canonical, so that walk answers EMPTY for exactly the
group the seed serves. PB664: the MOVE arm of the pointer screen landed in train 43 and the sweep it
asked for had not, so `STRING P DELIMITED BY SIZE INTO A` stored `CobolNet.Runtime.CellPointer` and
`UNSTRING P` stored `Cobo`; §14.9.43.3 SR1 is ONE sentence about five operands and was enforced at one
of them, while §14.9.48.3 SR2 was a BLACKLIST the four pointer categories the model gained later had
never been added to. Both now have the rules' own shapes, and `OperandText.FieldAsString`'s
`_ => Read(p).ToString()` fall-through is DELETED so a missing screen is a loud invariant failure
instead of a .NET type name in a user's output. Enforcing SR1 turned `2023/ec_data_not_finite` red and
THE GOLDEN WAS WRONG — it carried `STRING WS-INF DELIMITED BY SIZE INTO WS-TXT` over a USAGE COMP-2
item, which SR1 refuses and SR8 refuses again — so the leg was REMOVED and the header records why:
rule 4 the right way round, the COBOL was not valid so the golden moved, not the screen. PB394 did not
reproduce at all; what it owed was the re-verification its rows never got.

**Cluster 3 — one float landing decision and one initial evaluation (PB653 + PB654).** A NESTED float
operand was not quantized at all, because the receiver-shape test that decides binary64-or-fixed-scale
was spelled PRIVATELY by two consumers and the nested position asked neither.
`ReceiverContext.FloatLanding` now returns a three-answer `FloatLandingDecision` — `Binary64` or
`At(scale, mode)` — that also absorbs the receiver-shape test both consumers had written for
themselves. PB654: `ArithmeticEmitter.StoreArith` materialises the initial evaluation ONCE (§14.7.7
r4 a)) and the §14.7.4.3 r7 prohibited-value gate is written ONCE for all three fixed-scale receiver
arms — the edited and locale-edited arms had never had it. `ArithmeticOneInitialEvaluationDriftTests`
is new, over the emitted C#, and its first two invariants were PROVED RED on the pre-fix build before
being trusted. Both notes needed no diagnostic code: three were returned.

**Cluster 4 — one tape phrase, and a lock every connector can SEE (PB668 + PB669).** `OPEN … REVERSED`
was parsed and IGNORED at `--std 85` — measured `R1=00 AAAAAAAA R2=00 BBBBBBBB R3=00 CCCCCCCC`, the
FORWARD order, with no diagnostic at compile or run time. It is now ONE enum end to end
(`BoundOpenTapePhrase` → `OpenTapePhrase` → the one effect switch in `FileRegistry.OpenCore`), with the
two syntax rules as NAMED rows behind a switch whose default throws — the first draft indexed those
rows by enum ORDINAL, a hand-order coupling where a structure belongs, and the self-review replaced it.
`MovesBackward(previous) => previous ^ Reversed` is the ONE direction conversion, so REVERSED reuses
the §14.9.30.4 GR21 c) walk PB334 built and adds no retrieval code. PB669 was worse than its note
claimed: a connector with NO LOCK MODE clause could not SEE another connector's record lock, and the
measured damage was that it REWROTE and then DELETED a record another connector held locked.
§9.1.16 is unqualified, so `FileRegistry.ShareOf` is now the ONE reader of `_connectorShares` and all
FIVE governed entries — read, keyed read, write, rewrite, delete — carried the identical early return
and all five are fixed; ACQUISITION stays gated by `LocksEffective` and the CHECK is gated by nothing.
A contract finding did NOT hold on today's tree and was re-probed rather than inherited: PB669's
`SequentialIoEmitter.LockGoverned` no longer exists, PB683 having removed the compile-time predicate,
so the whole surviving defect was the runtime's. ⚠ The implementer's battery is Windows-only, so the
Linux CI leg on this push is the real verdict for PB669's lock semantics (kb/Work PB796).

**Cluster 5 — one crossing vocabulary and one definition-name namespace (PB663 + PB660).** A LINKAGE
formal of class pointer did not compile at all: `error CS1503: cannot convert from 'string' to
'CobolNet.Runtime.ManagedPointer?'`. `CallCrossing` {Native · Text · VarGroup · Managed} is now the ONE
vocabulary both sides speak, deleting the callee's `bool isNum`, two hand-copied adoption dispatches
and three re-derivations of `CallPlaceIsVarGroup`. One correction to the note, re-probed: a FORMAT-1
CALL cannot reach that callee at all (§14.9.4.3 SR10 is COBOLNET1679), so the repro needs `AS NESTED`
— which is precisely why the hole survived the whole pointer increment. PB660 is the two-arm lesson
in its general form: ⛔ the note's claim that the CONTAINED half "appears enforced" is FALSE, and all
five axes reproduced — two outermost `PROGRAM-ID. P3DND.` ran the second, two same-named CONTAINED
programs ran the first, and a program and a function under one name BOTH ran. Every definition
namespace policed only ITSELF, so nothing ever compared a definition of one KIND against another;
`BinderDriver.CheckDefinitionNameUniqueness` now walks one enumerator for all four, with COBOLNET2213
over §8.3.2.2's externalized-definition list and COBOLNET2214 over §8.4.6.3's containment subtree. Two
inherited claims were rejected on evidence: the note's "ordinal comparison, per PB303" (PB303 records
no such determination, and a check stricter than the resolver passes source the run unit then resolves
to the wrong definition), and "§12.3.8.4 GR10 b)" for the prototype/definition pairing — `--check`
says that text is GR10 a), and the clause that legislates the pair is §10.6.2 SR2/SR3.

**The train.** ⛔ The gate was the WHOLE `Cobol.Net.Tests.Conformance` assembly UNFILTERED — the filter
term is the assembly's own root namespace, never a union of the implementers' subject terms — plus
unfiltered Unit, unfiltered Characterization and the legacy `CobolSharp.Tests.Integration` assembly,
which is the only suite that compiles spellings a grammar tightening removes and cluster 1 touched
`CobolParserCore.g4`. The whole assembly answered `Failed: 0, Passed: 7776, Total: 7776` in 9 m 14 s, unfiltered Unit `Failed: 0, Passed: 28643`, Characterization `Failed: 0, Passed: 33` and the legacy assembly `Failed: 0, Passed: 503, Skipped: 1`, with zero `[FAIL]` lines in any leg. The GPL GnuCOBOL corpus FETCHED into this fresh worktree (no `FETCH FAILED` line), so the two `ExternalCorpusPopulationDriftTests` are REAL passes here and not the environmental pair three clusters' own batteries reported.  Five new diagnostic codes were claimed — COBOLNET2205 (cluster 1),
2210 and 2211 (cluster 4), 2213 and 2214 (cluster 5) — with the ranges checked for collision ACROSS
clusters, a hazard a single-cluster landing never has, and the check EARNED ITS KEEP: **COBOLNET2205
had been allocated to TWO clusters**, cluster 1 and cluster 2, and it bit nothing only because
cluster 2 used none of its four and returned them all. Eight distinct codes came back unused
(2203, 2204, 2206–2209, 2212, 2215), so the next free code is COBOLNET2215, taken from
`session-probe.ps1`'s ceiling-of-both-scans rather than from any report. All ELEVEN `record_verdicts` batches were re-applied in landing order on the
merged tree and the result was then compared ROW BY ROW against the text-merged inventory: zero rows
differed, which is the evidence that the JSON merge was faithful rather than the assumption that it
was. One batch is a mid-sequence re-adjudication — `pb664-string-unstring-operand-screen` takes
`SR-14.9.43.3-1` CONFORMS → PARTIAL and the next batch takes it back — which is why order, not
idempotence, is the property that matters here. GAP 2288 → 2276 on 4,348 rows. `docs/DIAGNOSTICS.md`
and the `*.g.cs` registries were REGENERATED from their sources after the merge rather than
hand-resolved, and the regenerated `DIAGNOSTICS.md` came back byte-identical to the union resolution —
the generator confirming the merge rather than the merge being trusted. semgrep did not increase any
count and `cobolnet-raw-diagnostic-code-literal` went DOWN 393 → 391. Fourteen citations, three per
cluster, were re-run through `cite.py --check` and all fourteen passed.

