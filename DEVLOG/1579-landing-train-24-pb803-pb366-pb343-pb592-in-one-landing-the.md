## Entry 1579 — 2026-09-09 13:29 PDT — Landing train 24: PB803 + PB366 + PB343 + PB592 in one landing — the witness-owed band closes, GAP 2733 → 2726

**PB803 — the DISPLAY over-punch convention becomes TWO conventions behind one option.** Owner decisions Q28 and
Q29 (2026-09-09, verbatim: *"4 and 5. Keep both behind an option with the default being IBM and Micro Focus
compatibility."*) answer Annex A.1 items 177 (the representation of the operational sign when PICTURE carries `S`
and no SIGN clause is written, §13.18.52.4 GR4) and 178 (the valid sign set when SEPARATE CHARACTER is absent,
GR5 b). Both are REQUIRED and required to be DOCUMENTED, and `docs/CONFORMANCE.md` §7's A.1 table jumped straight
from item 171 to 179 — neither row existed, which is why the golden lane had correctly REFUSED to write a witness
for `12L`: a golden pinning an undocumented choice reads as though the choice were documented. The mechanism was a
rule written down TWICE — the encoder's fixed `{ABCDEFGHI` / `}JKLMNOPQR` table beside `CobolNum`'s DISPLAY
formatter, and a second, independent literal character-range pattern inside `CobolClass.IsNumericZoned`, which IS
GR5 b) restated. Two copies is exactly why a second convention could not be expressed at all: a character-range
pattern can only ever answer for one table. `src/Cobol.Net.Runtime/Values/Numeric/ZonedSign.cs` is now the ONE
place either table is written, and `IsSignCharacter` derives the valid set from the same tables `Punch` encodes
with, so the class condition and the encoder cannot disagree by construction. `NumProfile` gains `SignEncoding` as
an axis ORTHOGONAL to `SignKind` — position and representation are separate obligations inside GR4 — and the
option threads CLI → `CompilerDriver` → `EditionContext` → `EmitContext` → both profile-emission sites;
`PicInfo.ProfileInitializer` became a method with a REQUIRED parameter so that no emission site can silently keep
the old default. A new corpus mechanism came with it: a golden may carry `*> options: key=value` at the START of a
comment line, read by `ConformanceCorpus.ApplySourceOptions` (an unknown key or value throws; a quoted header
mid-comment is ignored, with a regression leg for each). That is what lets a golden exercise the LATITUDE ITSELF
rather than one choice inside it. Goldens: `2023/pb803_sign_default_representation` (position — a no-SIGN-clause
item equals one written `SIGN IS TRAILING` and DIFFERS from `SIGN IS LEADING`; width via a one-byte sentinel in
the same group), `2023/pb803_sign_valid_set` (both ends of both tables — `12{` = +120, `12}` = −120 — four
near-misses refused, and `C23` = +323, which fails for any implementation that treats a punch as a sign-only
marker), `2023/pb803_sign_encoding_ascii` (the same claims under `*> options: sign-encoding=ascii`: `123t`,
`q234`, `12t` NUMERIC and `12C` not), `85/pb803_sign_default_representation_85` (edition independence becomes a
witnessed claim rather than an assumption). ⛔ **THE DRIFT-TEST INVARIANT THE DISPATCH SPECIFIED IS FALSE, and the
implementer refused it rather than writing it:** the dispatch asked for "disjoint from plain digits", but
`ascii`'s POSITIVE table *is* `"0123456789"` — that convention leaves a positive digit alone — so asserting it
would have forced the wrong table. What actually has to hold is the ASYMMETRIC half: no NEGATIVE punch is a plain
digit (a negative value's image can never be mistaken for an unpunched positive one, which is how a sign is
silently lost), plus positive ∩ negative = ∅ WITHIN a convention. `ZonedSignTableDriftTests` asserts those and
says in its own comment why the obvious phrasing must not be written there. The self-review corrected one of the
implementer's own claims: "an ordinary compile's generated C# is byte-identical" is true of the emitted PROFILES
(which state the convention only when non-default) but NOT of the emitted class condition, which states it
always. Rows `GR-13.18.52.4-4`, `GR-13.18.52.4-5`, `DOC-A.1-177`, `DOC-A.1-178` → CONFORMS (−4). No diagnostic
code allocated: COBOLNET1916/1917 were reserved and NOT used, because an unknown `--sign-encoding` value is a CLI
argument error on the `--std` precedent. Both stay free.

**PB366 — `USE AFTER EXCEPTION OBJECT` selected only one of a COBOL class's two emitted hierarchies.**
§14.9.49.4 GR14 a) selects a Format-4 declarative when "the exception object that was raised is a factory object
or instance object of object-class-name-1 or of a subclass of object-class-name-1". The emitted `__EcObjDispatch`
rendered ONE C# type test per declarative, `__obj is FOO` — but a COBOL class is emitted as TWO DISJOINT C#
hierarchies, `FOO` rooted at its base's instance half and `FOO__FACTORY` at its base's FACTORY half, and
`FOO__FACTORY` does not derive from `FOO`. The single test was therefore FALSE for every factory exception
object: a WRONG ANSWER with no diagnostic — the declarative the program wrote simply did not run and control fell
through to §14.6.13.1.5's tail as though no USE statement had named the class. The root shape is one this project
has now hit repeatedly: **a SCALAR column where the rule names a SET.** `BoundDeclarative.EoClassCsName` held a
RENDERED C# name — one string, decided at bind time, with the two-hierarchy fact already lost before the emitter
saw it. It is now `EoClass`, the resolved `OoClassSymbol`, and `OoClassSymbol.FactoryOrInstanceCsTypes` is the ONE
census of "the emitted C# types a COBOL reference to this class selects when the rule names the class WITHOUT
distinguishing the two object kinds"; `EcEmitter.EmitObjDispatchSelector` renders one or-pattern per entry, and
"or of a subclass" then rides C#'s `is` in EACH hierarchy because both mirror INHERITS. The note's proposed cure —
a runtime class-identity test on the COBOL class symbol — was NOT taken, and the note now records why: two C# type
tests ARE GR14 a), with no runtime metadata needed. The sibling narrow is instance-only and CORRECT, and the
census says so with its citation: §13.18.60.4 GR22 b) names ONE kind ("If the FACTORY phrase is not specified, the
object referenced by this data item shall be an instance object of the specified class or of a subclass of the
specified class"), so the next reader can tell the two rules apart instead of "fixing" the narrow to match. Both
raise sites were MEASURED to reach the selector, not assumed. Goldens:
`2002/pb366_use_exception_object_factory` with five GR14 arms — a class's factory object; the factory object of a
class a LATER declarative also matches (source-order first-match); the factory object of a SUBCLASS naming no
declarative of its own; an INSTANCE object; and the factory object of a class no declarative names (the
14.6.13.1.5 tail) — of which arms 1, 2 and 3 printed NO HANDLER AT ALL on the pre-fix tree; plus
`negative/pb366-use-exception-object-85` for the 2002 edition gate (COBOLNET0876). `Format4UseObjectSelectorDrift
Tests` asserts against the GENERATED C# in both directions. Reachability was measured rather than deduced: the
CALL/INVOKE propagation path cannot carry a factory object either, because `GOBACK RAISING` needs a TYPED
reference (COBOLNET0849), so `SET <universal> TO <class-name>` + `RAISE` is the whole reachable surface — and it
is ordinary source. Row `GR-14.9.49.4-14` STAYS DIVERGES (GAP 0) for GR14 b), the interface operand, which is
still `COBOLNET0859` and belongs to PB365 — NOT in this train. Its notes, code-location and test-ref now record
GR14 a)'s factory arm FIXED with its witnesses AND name PB365 for b), so a future PB365 batch must SPLICE rather
than overwrite: PB366's code-location and test-ref are supersets of the pre-fix values. COBOLNET1922/1923 were
reserved and NOT used.

**PB343 — a relative `READ … PREVIOUS` after `OPEN INPUT` owed the first existing record and answered at-end.**
§14.9.30.4 GR21, "When the file is a relative file", rule b): "If the file position indicator was established by a
prior successful OPEN or START statement, the first existing record that is selected is made available, REGARDLESS
of whether NEXT or PREVIOUS is specified." The connector answered '10' — a wrong answer that silently loses the
file's first record for any program walking a relative file backwards from the top. The carve-out it was applying
is the INDEXED sub-rule block's d) 3 — the one Annex E.2 item 22 (INFORMATIVE) introduced — and it reached the
relative connector through `docs/VERSION_CHANGE_REFERENCE.md` row 29, **which named no organization**, so every
reader downstream applied an indexed rule to a relative file. Row 29 is now scoped to INDEXED (its todo kept: the
indexed ≤2014 leg is still ungated). `RelativeConnector.SelectSequentialSlot` now branches on the RULE rather than
on the symptom: while the indicator is INCLUSIVE (rule b) ONE ascending walk serves BOTH directions and `previous`
is not consulted at all; only rule c)'s EXCLUSIVE arm lets the direction select. The dead `_positioner` field —
a second char that distinguished OPEN from START in lockstep with the inclusive flag, and existed ONLY to enforce
the indexed carve-out here — is deleted; the indexed connector keeps its own, because d) 3 IS the indexed rule.
⛔ **The sweep found the same wrong arm under the same wrong citation in the LEGACY oracle**
(`CobolSharp.Runtime/IO/RelativeFileHandler.ReadPrevious`, which cited GR21 d.2), **and it found a GREEN test
PINNING THE WRONG ANSWER**: `SpecFixTests.ReadPrevious_Relative_AfterOpen_RaisesAtEnd` asserted `ATEND 10`. It is
renamed `…_MakesTheFirstRecordAvailable`, now expects `GOT 11`, and its comment records what it used to assert and
why — and because `SpecFixTests` drives the LEGACY compiler, that one green test had been pinning the defect on
BOTH engines at once. The note had named those two sites as *evidence the mis-attribution was spreading* but not
as things to fix; it under-counted the blast radius by two. Goldens `2002|2014|2023/pb343_read_previous_relative`
from one template, whose SPARSE-file phase (lowest existing record at RRN 5, nothing below it) is what makes rule
b) FALSIFIABLE, plus `negative/pb343-read-previous-relative-85` (COBOLNET0900). Rows `GR-14.9.30.4-L4.3` and
`GR-14.9.30.4-21` DIVERGES → CONFORMS (−2): the note expected `-21` to stay open for a second, non-sequential
divergence (PB336's '46' guard ordering), but that was RE-MEASURED on today's tree and found already landed, so
the row closed rather than carrying an unfixed defect through a verdict flip. COBOLNET1918/1919 reserved and NOT
used.

**PB592 — `DOC-A.1-206` was a DETERMINATION contradicted by the compiler's own measured behaviour.** Owner
decision Q30 (2026-09-09). §7's row for A.1 item 206 — USAGE BINARY-CHAR/-SHORT/-LONG/-DOUBLE, "allow wider range
than the minimum", §13.18.60.4 GR12, OPTIONAL — read "Not provided. Each usage holds exactly the GR12 minimum
range", which is FALSE for all four SIGNED usages and, written as `±128`, was not readable as a range at all. NO
COMPILER CHANGE was needed: all eight bands already sit at the native two's-complement edges at `--std 2002` and
`--std 2023`. GR12's minimum bands are ASYMMETRIC and the asymmetry is the whole content of the rule — every
SIGNED row of its printed table is a STRICT `<` on both sides while every UNSIGNED row is `<=` on the left — and
the table was RE-RENDERED FROM THE CANONICAL PDF (page 537 = printed 507) rather than read from the OCR, because a
strict-vs-non-strict inequality is precisely the glyph the transcription loses. The signed exclusion of
−2^(w−1) is deliberate (it lets a sign-magnitude or ones'-complement machine conform) and §15.58.4's NOTE concedes
it in terms, so each SIGNED usage expresses exactly ONE value beyond the GR12 minimum and the four UNSIGNED
usages are exactly the minimum: the optional latitude IS taken, four times. The row now carries all eight bands,
the §14.7.5 disposition (a value outside the band raises the size-error condition — never a silent widening), the
implementing sites and its witness. Golden `2002/pb592_binary_usage_range_bounds` (PROGRAM-ID `L1BRB01`), 42 legs:
16 minimum-band round trips, 6 nesting legs at LOWEST- AND HIGHEST-ALGEBRAIC — the leg the parked draft lacked,
and the one that makes GR12's nesting sentence falsifiable at the WIDER end — and 20 provided-band edge legs. The
parked draft's inherited `§15.43.1 "highest"` citation was WRONG and is not carried in: the standard says the
GREATEST algebraic value. The DERIVED verdicts were re-measured rather than assumed — the
`a1-optional-not-provided` selector now takes 127 and 150 and asserts 206 is OUT — and three "measured" statements
about this register were found STALE and corrected (§7 has 58 rows, not 47; the two "Not provided" rows are 127
and 150, not 127 and 206; 5 optional items carry a §7 row, not 2). The implementer made BOTH new checks fail once
on purpose before trusting their silence. Notes: PB592 → landed; **PB373 → landed** — both its rows are now
witnessed and its `blocked_by` cleared, 206 having been what held it; PB280 gains a dated line, because its Q1
premise no longer holds for item 206. Row `DOC-A.1-206` DOCUMENTED-NON-SUPPORT → CONFORMS (−1).

**The train.** Four clusters, four commits, one landing. Every cluster's patch was applied with
`--exclude=tests/version-matrix/traceability-inventory.json` and its `record_verdicts` batch re-applied on the
merged tree in manifest order — no side of the JSON was ever taken — which is why main's own 2026-09-09 inventory
batches survived alongside all four clusters'. Not one hunk conflicted; the per-cluster conflict-marker check
(`git diff --check` on the tree plus `git grep --cached` for the three marker forms on the index) printed nothing
four times. GAP **2733 → 2726**: PB803 −4, PB366 0 (by design — the row stays DIVERGES for PB365's half), PB343
−2, PB592 −1. `audit_annex_a1.py --check`: no findings, and items 177, 178 and 206 each name their golden.
`scripts/spec/work.py check`: 841 work items, all well-formed. `scripts/semgrep/verify.py`: PASS, no count
increased over the baseline. Union filter over the Conformance assembly:
`~Corpus`, `~ClassCondition`, `~SignedAlphanumericMove`, `~GroupNumericLeaf`, `~RedefinesTierA`, `~MoveEdition`,
`~CorpusManifest`, `~VersionMatrix` — each as its own `FullyQualifiedName~` term — with the FULL
`Cobol.Net.Tests.Unit` assembly unfiltered, the FULL `Cobol.Net.Tests.Characterization` assembly, and the legacy
`CobolSharp.Tests.Integration` assembly, which is load-bearing this train because PB343 changed the legacy engine
and one of its tests. All four legs GREEN on the merged tree, verbatim:
`Passed!  - Failed:     0, Passed:  4149, Skipped:     0, Total:  4149, Duration: 8 m 6 s - Cobol.Net.Tests.Conformance.dll`;
`Passed!  - Failed:     0, Passed: 23075, Skipped:     0, Total: 23075, Duration: 2 m 5 s - Cobol.Net.Tests.Unit.dll`;
`Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33, Duration: 2 s - Cobol.Net.Tests.Characterization.dll`;
`Passed!  - Failed:     0, Passed:   503, Skipped:     1, Total:   504, Duration: 28 s - CobolSharp.Tests.Integration.dll`.
The eight filter terms were put back to vstest before the legs ran and ALL EIGHT ARE LIVE in the Conformance
assembly (1817 / 19 / 8 / 8 / 5 / 48 / 9 / 2244) — none dead, none inert. The GPL GnuCOBOL corpus was fetched
into this fresh worktree by `build-local.ps1` (GnuCOBOL 3.2, sha256 verified, 36 autotest `.at` files), so
`ExternalCorpusPopulationDriftTests` measured a real population and there is no environmental red to name. The
three static audits ran ahead of the build and are all at zero.

