## Entry 1598 — 2026-09-13 09:33 PDT — Landing train 31 — FIVE clusters in one landing: PB337 (the INTO receiver admissibility rules, and the alphanumeric-group definition that was written twice) + PB398 (EVALUATE's partial expression is a condition with its leftmost operand missing) + PB640 (the CALL/INVOKE argument crossing performs its COMPUTE where the standard puts it) + PB530 (three live PICTURE rules the Table-10 walk structurally cannot carry) + PB344 (the Annex E.2 item 19 edition gate withdrawn, and the behaviour register grows an admission test), GAP 2533 → 2517

**PB337 — the INTO phrase's admissibility rules, and a definition written twice.** ISO §14.9.30.3 SR1/SR2 (READ)
and §14.9.34.3 SR2/SR3 (RETURN) are the same rule stated twice with one difference, so they are now ONE screen:
`StatementValidation.CheckIntoReceiver`, called from `MoveBinder.BindIntoPhrase` — the single site PB348 already
funnels the sequential READ, the keyed READ and the sort RETURN through, so a fourth INTO-bearing verb inherits
the rule instead of re-deriving it. (The note's instruction to call it from three binders is superseded; FILES
design D24.) The per-verb wording rides on `IntoPhraseRules`, whose one real difference is the record COUNT:
READ's arm a) admits a record-less FD (§13.4.5.3 SR3) where RETURN's "only one" does not (§13.4.6.3 SR2). SR2/SR3's
second sentence — the record area shall be a strongly-typed group of the same type as identifier-1 — is the same
predicate over the same pair that §14.9.25.3 SR2 already applies to the implicit move's SENDER, so it stays with
`CheckStrongMove` (COBOLNET1533) and only the COUNT is new (COBOLNET1995); arm b) is COBOLNET1994. PRE-FIX,
MEASURED ON THE IMPLEMENTER'S OWN BUILD: an FD with `01 REC-A / 05 A-1 PIC X(4)` and `01 REC-B / 05 B-1 PIC 9(4)
COMP` read INTO a `PIC 9(4) COMP` item compiled and printed `GOT=0001`; an SD with an alphanumeric group and an
elementary `PIC 9(6)` returned INTO an alphanumeric group compiled AND RAN. The root cause reached further than
the INTO path: §13.18.29.4 GR3 — what an alphanumeric group item IS — was written down twice, incompletely in
`ItemCategory.Admits` (`item.IsGroup`) and completely but privately in `DataBinder.GroupValue`, so ASSIGN … USING
(§12.4.5.2 SR7) and both record-key rules admitted strongly-typed and variable-length groups. One spelling now,
with the sweep witnessed by its own negative golden (`pb337-assign-using-strong-group`). Nine goldens (the
positive in all four edition legs, five negatives, each verified at every listed edition), two drift tests proved
red first. `SR-14.9.30.3-1/-2` and `SR-14.9.34.3-2/-3` NOT-IMPLEMENTED → CONFORMS.

**PB398 — a partial expression is a condition with its leftmost operand missing.** `partial-expression-1` (`WHEN
> 5`, `WHEN NUMERIC`, `WHEN POSITIVE`, and — SR7 d) — `WHEN > 5 AND < 10`) had no grammar at all, so five
§14.9.13.3 rules had no code site and Table 15's Partial-expression row was a lookup no operand could reach. The
repair is the one SR8 describes: the splice IS §8.8.4.12's subject insertion, so `BindPartialExpression` seeds the
`AbbrevCarry` the abbreviated combined relation already uses and the compound form needs no code of its own. The
grammar is a four-rule spine whose tails ARE the condition tiers' own sub-rules — deliberately NOT folded into
`comparisonExpression`, whose widening DEVLOG 621 measured the cost of — pinned by
`PartialExpressionSpineDriftTests`, which EXTRACTS those tails rather than hard-coding them;
`BindClassConditionOn`/`BindSignConditionOn` were extracted so SR5's class and sign shapes bind through the same
bodies the written forms use. The range's `[ IN alphabet-name-1 ]` phrase was likewise absent from EVALUATE and —
worse — PARSED AND DROPPED in the VALUE clause: `88 X VALUE "M" THRU "A" IN AL` answered NO for a value the
alphabet puts inside the range, a SILENT WRONG ANSWER. §14.7.8 governs both clauses in one sentence, so both now
resolve through one `DataBinder.TryResolveRangeAlphabet` (COBOLNET1997–1999; §14.9.13.3 SR3 = §13.18.63.3 SR31),
the phrase OVERRIDES the program collating sequence as rule 2's exclusive arms require, and the carrier is a
per-type field rather than a 256-entry table allocated per evaluation. ⚠ The note's instruction to add the phrase
to `valueClauseRange` was WRONG against the printed general format: §13.18.63.2 puts `IN alphabet-name` OUTSIDE
the repeated group. A dead reading fell out of the sweep — `OperandCategory` answered null for a numeric literal,
which let `WHEN 1 THRU 9 IN AL` slip past SR3 (§8.3.3.3.1). `SR-14.9.13.3-3/-5/-8` NOT-IMPLEMENTED → CONFORMS,
`-6`/`-10` PARTIAL → CONFORMS; `GR-14.9.13.4-4` stays PARTIAL on a residue that CHANGED — a) 2. is realized and
only B-operator boolean expressions remain, which belong to another construct. ⚠ DETERMINATION, owner-overturnable
in one line: NO introduction gate is written. The 2002+ edge is INFERRED rather than stated, and gating on an
inference would reject legal source; the reading is stated in the goldens, the design doc and the note.

**PB640 — the argument crossing performs its COMPUTE where the standard puts it.** ISO §14.2.3 GR9 (second
branch) and GR10 make a BY CONTENT / BY VALUE argument the sending operand of "a COMPUTE statement without the
ROUNDED phrase" into a record *"allocated by the activating runtime element during the process of initiating the
activation"*. COBOL.NET performed that COMPUTE entirely CALLEE-side, so the ACTIVATING element's `>>TURN EC-SIZE
CHECKING` state, its USE declaratives and §14.9.4.4 GR3 g)'s "control is transferred … if a fatal exception
condition has not been raised" were all out of scope by the time it ran: an argument overflowing its formal's
description under checking stored DOC-A.1-70's low-order digits silently where §14.7.5's no-phrase rule 4 sets
EC-SIZE-TRUNCATION to exist. Measured: `CALL "S" AS NESTED USING BY CONTENT BIG` (10³⁰ into `PIC S9(9)V9(9)`)
under a caller `USE AFTER EXCEPTION CONDITION EC-SIZE` printed `S1 LA=+000000000.000000000` and no `CAUGHT=` line.
`CallEmitter.ArgText` is now `LandedForFormal(a, ArgCarrierText(a))` — one wrapper around EVERY carrier shape,
sharing the one `CallAbi.LandScalar` with the callee-side adapters, which become the identity over an
already-landed argument — with the checked kernel (`CobolNum.StoreOrRaise`) selected at COMPILE time from
`EcState.SizeTruncationChecking`. No new raise plumbing was needed: EC-SIZE-TRUNCATION is already a fatal ambient
gate and a CALL is not an `IArithmeticStatement`, so the statement's existing try/catch runs the §14.9.49 F3
selection and honours RESUME; emitting the landing inside the argument expression puts the raise before
`ProgramRegistry.CallProgram` is entered, which is GR3 g)'s ordering, and works in an expression-position UDF
activation too. The INVOKE lane was the sibling arm — already caller-side (§14.8.2.3.3 rule 2 a)) but always
unchecked; all three `OoEmitter` BY CONTENT arms now take the raising store and the numeric-literal arm joined the
shared `StoreExpr` it had never used. ⛔ The guard is `PicInfo.IsClassNumericFixedPoint`, not a bare category test:
`PicInfo.IndexItem` carries category Numeric with ZERO digits, so the first version crossed a `BY CONTENT` USAGE
INDEX of 3 as 0 through `value % 10^0` — GR9 puts class index on its SET bullet, and probing that bullet is what
caught it; the trap is now written down in one place. ⚠ The note's repro was GR9's FIRST branch (no COMPUTE at
all); the COMPUTE branch is the prototyped / NESTED / method / function set, so the goldens use AS NESTED. Nine
goldens across 2002/2014/2023, the drift test extended to 17 facts and proven to fail. `GR-14.2.3-9` and `-10`
stay PARTIAL on a much narrower residue and the residue is now its OWN note: a nested CALL passing a USAGE POINTER
or OBJECT REFERENCE item emits UNCOMPILABLE C# (CS0029 / CS1061 — the callee-side formal carrier for those
categories is the STRING carrier), so the lander allocated **`kb/Work/PB834`**, transferred both rows to it and
flipped PB640 to `landed` — the implementer had left it `half` and asked for exactly that allocation.

**PB530 — three PICTURE rules the Table-10 walk structurally cannot carry.** Three of the note's four rows (SR25
sentence 1, SR26, SR27) were ALREADY enforced by PB528's Table-10 walk; that was re-derived and verdicted with its
witnesses rather than re-implemented. The residue was live. **§13.18.40.3 SR29** — "For floating insertion, at
least one insertion symbol shall be specified to the left of the decimal point position" — is refused by Table 10
only when a DIGIT precedes the floating string, because the right-of-point floating rows admit the decimal
separator; a string with nothing but the point to its left passed the walk, bound, and rendered an image no rule
derives (`PIC .$$` turned `MOVE 0.5` into `.00`; `PIC V$$` into ` $`; `PIC .++` into `.00`; `PIC V--` into two
spaces — four unprobed cases). §13.18.40.5 rule 6 is why the rule exists: its a) leg edits the leading positions
"to the LEFT of the decimal point position" and its b) leg all of them "the same as if the floating insertion
editing were defined only to the left of the decimal point position", so a string wholly to the RIGHT matches
neither and the standard defines nothing for it. The check is one comparison; what it needed was a BOUNDARY that
did not exist as a value, so the decimal point position is hoisted to `PictureComposition.DecimalPointPosition`
('V' or the decimal separator, else a leading 'P' string's implied point per §13.18.40.4 GR14, else past the last
symbol), derived once in `Validate` and PASSED to `Precedence` — that invariant, not the `if`, is the change
(COBOLNET1934). Two more rules were stated over a SET nothing had assembled: SR24's and SR25's SECOND sentences
are properties of the whole EDITING phrase LIST, so neither was askable in the per-phrase loop that carried every
other SR8–SR12 check. `PIC 9L9F9G` with three FOR phrases bound clean and rendered `-12` as `0(1)2]` where SR24
admits "either one or two extended editing sign control symbols" (now COBOLNET1985); `PIC F999.99L` with its two
EDITING phrases written in the reverse order of their symbols rendered `-1.5` as `)001.50(` where the conforming
spelling renders `(001.50)` (now COBOLNET1984) — the order is not cosmetic, because each extended symbol renders
its own literal at its own position. Five negatives, two positives, four unit theories, the D24 design section
extended. `SR-13.18.40.3-25/-26/-27/-29` → CONFORMS. ⚠ DETERMINATION, owner-overturnable: SR25 sentence 2's "the
first occurrence of the EDITING phrase shall be for the leftmost symbol in character-string-1" is read as the
leftmost OF THE TWO EXTENDED SYMBOLS the sentence has just named — the rule constrains PHRASE ORDER, not symbol
placement. The stricter reading (that the two shall also BE the string's first and last symbols, which is how the
same clause uses that phrase elsewhere) rejects strictly more, and Annex D.24's own `PIC IS L9999.99F` would not
survive it. The lander closed the report's item N1 with a one-record batch: `SR-13.18.40.3-24` is PB528's row and
already read CONFORMS for sentence 1, so its verdict is unchanged and the record exists only so the row's evidence
names COBOLNET1985 and `pb530-picture-editing-extended-count` — the check that closed sentence 2.

**PB344 (finisher #2, the cluster train 29 dropped) — an informative annex is not a normative gate.** Train 29
dropped PB344 whole: its wave-local gate was green and CI was red on five NIST CCVS programs — SQ122A, SQ136A,
SQ137A, SQ138A, SQ148A — all sequential-I/O, all disagreeing about which USE declarative runs for a READ
exception. The diagnosis is that the GATE, not the goldens, was wrong. Annex E is INFORMATIVE; §E.1 scopes it to
"the substantive changes between the previous COBOL standard and this Working Draft International Standard" — ONE
prior edition, the 2014 one — so it says nothing at all about the 1985 or 2002 editions the guard was applied to;
item 19's own justification classes the prior state as defective TEXT ("The previous COBOL Standard was not clear
or missing processing of some I-O exceptions"), and a silence is not a prohibition. For 1985 the behaviour is
fixed the other way by that edition's own validation suite — SQ137A fails with the literal remark "INPUT
DECLARATIVE NOT EXECUTED, CORRECT = EXECUTED" against ANSI X3.23-1985 VII-51 4.6.4(5) — and item 19 b) names only
"any declarative that specifies INPUT or I-O", so SQ148A's OUTPUT declarative was never in its scope: the guard
was over-broad on its own terms as well. Both halves are DELETED rather than left dead, along with the
`isRead` / `__modeTier` plumbing that existed only to feed them, and the six 85/2002/2014 `pb344_use_mode_*`
goldens now equal their 2023 twins BY DERIVATION. What survives is the edition seam the note was really about: the
compilation's ISO edition reaches the connectors (`RuntimeApi.FileRegister*` take a required `edition:`,
`FileConnector.Edition`, the `CobolNet.Runtime.DialectBehavior` registry) and gates ONE behaviour,
`IndexedReadPreviousAfterOpenAtEnd` — Annex E.2 item 22, whose justification states the PRIOR RULE outright ("the
rule itself stated that the first record would be retrieved"), so ≤2014 retrieves the first existing record and
2023 is at end per §14.9.30.4 GR21 d) 3. That difference between item 22 and item 19 is now the register's
ADMISSION TEST, written on the enum, in the VERSION_CHANGE_REFERENCE legend and in the files design: a
`DialectBehavior` member is admitted only when the Annex E item says what the PRIOR RULE STATED. VCR rows 25/26
became `ref-only` with the determination, row 29 keeps its `behavior:` anchor. Separately the USE open-mode tiers,
found in THREE places rather than the note's two (`__IoCheck`, `__IoCheckEc` and `__RunGlobalUse`'s GR4 b) outward
GLOBAL walk), are extracted into one `CodeGen/UseTierEmitter`, and `UseTierEditionInvarianceDriftTests` compiles
one program at all four editions and asserts the emitted selector text is BYTE-IDENTICAL — the GR4 b) walk no
golden can reach — with both of its failure branches fired before it was trusted. The merge reconciled
`EmitUseHook` against main's PB367b (`notNormalLabel` kept, `isRead` gone). `GR-14.9.30.4-L2.3`, `-28` and
`GR-14.9.49.4-6` PARTIAL → CONFORMS. NO NIST golden moved.

**The train.** Five clusters, brought in on `a1de916e` in the order PB337 · PB398 · PB640 · PB530 · PB344, one
commit each plus this one. Rows 1–2 were based on `90ec658e` and rows 3–5 on `a1de916e`; every conflict was a
keep-both TAIL — the `DiagnosticCatalog` descriptor block twice and eight conformance-manifest `enabled` lists —
and `tests/version-matrix/traceability-inventory.json` and `docs/DIAGNOSTICS.md` were excluded from all five
patches on purpose: the first is rebuilt by re-applying every cluster's `record_verdicts` batch in manifest order
on the merged tree, the second is regenerated from the catalogue. The per-cluster marker check (`git diff --check`
on the working tree AND `git grep --cached` on the index) printed nothing for all five. ONE build for the train,
then the union of the five gate filters — `~Drift|~EditionGate|~Read|~Return|~Corpus|~Move|~Sort|~Diagnostic|
~Evaluate|~Condition|~Collat|~VersionMatrix|~Call|~ExceptionCondition|~Nist|~DataClause|~Picture|~FileIo|
~Declarative|~Use|~Indexed|~Vcr|~Io` — over Conformance, the FULL `Cobol.Net.Tests.Unit` assembly, Characterization
and the legacy `CobolSharp.Tests.Integration` suite. EVERY LEG GREEN: Conformance `Passed!  - Failed: 0, Passed: 6340, Skipped: 0, Total: 6340, Duration: 10 m 38 s`; the UNFILTERED Unit assembly `Passed!  - Failed: 0, Passed: 23861, Skipped: 0, Total: 23861, Duration: 2 m 13 s`; Characterization `Passed!  - Failed: 0, Passed: 33, Skipped: 0, Total: 33, Duration: 2 s`; and the legacy `CobolSharp.Tests.Integration` assembly `Passed!  - Failed: 0, Passed: 503, Skipped: 1, Total: 504, Duration: 34 s`. All 23 filter terms were probed live before the legs ran, and the three citation audits (code, doc, frozen-evidence supersession) returned 0 findings; the GnuCOBOL corpus fetched cleanly into this fresh worktree, so PB209's loud arm stayed quiet on its own evidence rather than by absence. Semgrep unchanged at
3 / 46 / 2 / 416. Twenty verdict records across six batches; GAP **2533 → 2517** of 4348, which is the figure the
manifest predicted exactly (4 + 5 + 0 + 4 + 3, PB640's two rows staying PARTIAL by design). Diagnostic codes
claimed: 1984, 1985 (PB530), 1994, 1995 (PB337), 1997–1999 (PB398) — checked for collision ACROSS clusters, a
hazard a single-cluster landing never has; next free is 1996, then 2000. NO cluster was dropped. Notes flipped:
PB337, PB398, PB640, PB530, PB344 → `landed`; PB834 NEW and `open`.

**Thirteen unfiled leads carried out of the five reports**, recorded here so none is lost: (1) an FD/SD sharing one
record area across several 01s is diagnosed as a REDEFINES — COBOLNET1697/1532 under §13.18.44.3 SR14 /
§13.18.57.3 SR4, which is the wrong diagnosis for the shape; (2) `ItemCategory` has no unit test of its own; (3)
EVALUATE's `IN alphabet-name-1` is UNREACHABLE when identifier-4 is written (`WHEN WS-LO THRU WS-HI IN AL` →
COBOLNET1639; it needs a `ReferenceResolver` re-resolve without the trailing qualifier); (4) a BARE user
class-name object (`WHEN MY-DIGIT`) binds as identifier-2 and errors, where the `IS`-led spelling works; (5) the
class-pointer / class-object argument carrier — now `kb/Work/PB834`; (6) a signed image-carried numeric FORMAL
loses its operational sign across the CALL boundary (`CobolArgAdapt.Text`/`TextValue` hard-code `Signed = false`;
§14.2.3 GR11 resolves references "in accordance with their description in the linkage section", and that
description is SIGNED) — a silent wrong answer, recorded in PB834's body; (7) PB367b's ambient-checking-flag leak
across CALL is NOT this mechanism (EC-SIZE checking is a compile-time kernel selection) and stays open; (8)
PB530's N1, closed by this landing's one-record batch; (9) the "es" Table-10 precedence for a SINGLE extended
symbol; (10) SR26's character-1 multiplicity; (11) `SortEmitter.EmitInputFile` hooks the USE after §14.9.30.4
GR12 c)'s implicit CLOSE, silently; (12) PROCESS — run the NIST leg UNFILTERED in `build-local.ps1` the way the
Unit assembly is (~1.5 min): it is the leg that caught a determination every COBOL.NET-authored golden agreed
with, and train 29's CI red is what it cost to learn that; (13) the two determinations above (PB530's SR25
sentence-2 reading, PB398's no-introduction-gate judgment) plus PB344's edition-invariance of the incorporated
§9.1.14 contract are recorded as DETERMINATIONS the owner may overturn in one line each.

