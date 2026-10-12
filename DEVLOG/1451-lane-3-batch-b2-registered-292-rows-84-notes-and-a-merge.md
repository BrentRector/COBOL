## Entry 1451 — 2026-09-04 15:36 PDT — Lane-3 batch b2 registered: 292 rows, 84 notes, and a merge that would have closed two rows on evidence its own refuter had just destroyed

Batch b2 is the second adjudication batch of the burn-down's lane 3 — the procedural statements **EVALUATE ·
EXIT · GO TO · GOBACK · IF · INITIALIZE · MOVE · PERFORM · SEARCH · SET · SUBTRACT**, twenty subject files,
adjudicated and then independently REFUTED against the pinned `5fe593a0` compiler while the fix lane moved main
eight landings ahead of it. This entry is the registrar's: the merge, the clustering, what re-measurement on
today's tree contradicted, and what the register now owes. **GAP 3054 → 3022.**

### The shape of the batch

295 adjudicated records, **292 recorded** (three dropped, below).

| verdict | rows |
|---|---|
| CONFORMS | 78 — **32 close the row**, 46 are CONFORMS-but-untested |
| PARTIAL | 83 |
| NOT-IMPLEMENTED | 72 |
| DIVERGES | 62 |

**The refutation was uniformly downward.** 120 refuter checkpoints, one per adjudicated CONFORMS: **76 upheld,
44 overturned** — 12 to DIVERGES, 30 to PARTIAL, and 2 that kept the verdict CONFORMS while destroying the
evidence under it. **Not one overturn went upward.** A refuter that only ever agrees is measuring nothing; this
one moved 37% of the rows it was pointed at, and every move made the register more honest.

### ⛔ The merge would have CLOSED two rows on evidence the refuter had just shown cannot pin the rule

`merge_batch.py` takes a refuted row's corrected verdict and, where the refuter says so, clears the cited
`test-ref`. It recognised exactly one spelling — the literal word `WITHDRAWN`, in `corrected_verdict` alone.
Batch b2 produced BOTH ways of missing it, in one run:

* **`GR-14.9.37.4-6`** — `corrected_verdict: "CONFORMS (test-ref must be CLEARED — the row does not close)"`.
  The reason is exact: `conformance:2023/ec_range_search` cannot pin GR6's branch, because GR6 applies only
  *"If any condition specified in General rule 5 is not satisfied"* and in that test GR5 IS satisfied — its
  table is `05 WS-KE OCCURS 3 TIMES ASCENDING KEY IS WS-K` holding 1, 3, 5. Its ALL-NM leg is governed by GR9,
  where it is already cited.
* **`SR-14.9.44.3-5`** — `corrected_verdict` is the bare word `CONFORMS`; the instruction ("record … as CONFORMS
  with an EMPTY test-ref") was written only in `why`. The cited golden writes `SUBTRACT CORR` and nothing else,
  and *the entire content of SR5 is that CORR and CORRESPONDING are equivalent* — a golden spelling one word
  cannot distinguish equivalence from "only CORR is implemented". Its own method name says so: `_CorrToken`.

Both would have counted as closed. Fixed at the source, so the next case is automatic: the clearing instruction
is a regex over BOTH `corrected_verdict` AND `why`, matching a clearing verb (WITHDRAWN / CLEAR(ED) / EMPTY)
adjacent to a test-ref mention. CONFORMS-with-test-ref fell 34 → 32; the test-needed band rose 44 → 46.

### ⛔ Three records dropped: the batch would have overwritten an OWNER decision on rows that are already closed

`SR-14.9.39.3-15`, `SR-14.9.39.3-16` and `GR-14.9.39.4-11` — SET **Format 6 ATTRIBUTE**, the Annex A.4.2 screen
module. The adjudicators, on the pinned compiler, measured a bare parse error and reasoned that the shipped
posture is not the documented one; `record_verdicts --dry-run` duly reported them as
**DOCUMENTED-NON-SUPPORT → NOT-IMPLEMENTED**, which is an agent reversing an owner decision. Re-measured on
this tree (`SET SCR-1 ATTRIBUTE HIGHLIGHT ON`, `--std 2023`):

```
PIN  5fe593a0 : COBOL0001: no viable alternative at input 'SETSCR-1ATTRIBUTEHIGHLIGHTON'  (+3 cascades)
HEAD 89044aa0: COBOLNET1707: the SET statement's screen attribute format 6 (ISO 14.9.39; Annex A.4.2 item 24)
               applied to 'SCR-1': … part of the OPTIONAL screen handling module … for which COBOL.NET claims
               no support. … See docs/CONFORMANCE.md 5.
```

Fixed between the pin and this base by **`60bf02d7` (A.4.2 screen handling: refused by name, 163 rows)**. The
three rows keep their `DOCUMENTED-NON-SUPPORT`, state **OK**, `ScreenFacility.cs#ReportSetAttribute` and the
`conformance:negative/a42-set-attribute` witness. The MESSAGE-TAG half of the same cluster WAS re-measured and
still fails as a bare `COBOL0001` cascade, so `PB453` carries that half only — and the same facility's
statement half **does** warn by name (`SEND TO … FROM …` → `warning COBOLNET1578 … (§4.2.6; Annex A.3 item 4)`
and the program runs). One facility, one owner disposition, two postures.

### What ELSE the re-measurement contradicted

* **The grammar's per-statement section banners are already correct.** Two findings (MOVE's `// MOVE (14.9.24)`
  = MERGE, SUBTRACT's `14.9.42` = STOP, UNSTRING's `14.9.44` = SUBTRACT, 14 of 30 wrong) were the second half of
  `PB388`. On this base every banner cites its own statement — MOVE §14.9.25, SUBTRACT §14.9.44, UNSTRING
  §14.9.48, STOP §14.9.42, and the eleven others. Fixed by **`9c08e6fe` (Citation sweep: ~200 phantom sites)**.
  `PB388` was rewritten around what survives: the wrong §/rule numbers **PRINTED TO USERS** by the shared
  EXIT/GOBACK/RAISE diagnostics, the SET format ordinals, and the SEARCH GR numbering shift.
* **One adjudicator probe was ILLEGAL SOURCE and had to be re-cast.** `INITIALIZE`'s pointer probes declare
  `05 P1 USAGE POINTER.` under a plain group, which HEAD now correctly refuses (`COBOLNET1724`, §13.18.60.3
  SR14). Re-cast onto an elementary level-01 pointer and a `TYPEDEF STRONG` group child, the defect reproduces
  on all three modelled categories. ⚠ **Every probe, golden and test carrying that shape is now illegal source**
  — a sweep the fixer owes.
* **`GR-14.9.28.4-4`'s mechanism claim was wrong.** The finding said `PERFORM ZERO TIMES` fails because
  `performTimes` admits only `integerLiteral`; on HEAD the rule has three arms
  (`integerLiteral | functionCall | dataReference`) and the real cause is that **no** arm admits a figurative
  constant. Corrected in `PB432` so no fixer inherits it.
* **Everything else reproduces.** Across all eight note-writing groups, every other finding and every refuter
  record was re-run on `89044aa0` and still fails.

### 500 shape violations, and why the fix was not to weaken a record

The first `--dry-run` refused the whole batch: **472 `code-location` fragments and 28 `editions` fields**. The
inventory schema's `code-location` is deliberately SYMBOL-keyed — *"a line number rots on the next edit … a gate
that cries wolf is a gate nobody reads"* — and the adjudicators wrote `file.cs:120-134 Method (and the prose
explaining why)`. `editions` is the four-word vocabulary `85 / 2002 / 2014 / 2023`; 28 records carried a
paragraph of edition REASONING in it. Both were normalised to the schema shape **and the original text appended
verbatim to `notes`**, so the forensic detail survives in the row instead of being deleted to satisfy a
validator. No record was weakened.

### The clustering: 132 mechanisms + 37 finding-less rows → 84 notes

One note per MECHANISM, never per row and never per file. Nine notes span more than one statement subject, and
those are the ones worth reading first.

| note | severity · harm | rows | mechanism |
|---|---|---|---|
| `PB387` | MINOR · under-reject silent | 2 | tests/CobolSharp.Tests.Unit/Overlenient/ is 45 EMPTY [Fact] bodies across 15 constructs: 45 green tests whose NAMES are conformance claims and whose B |
| `PB388` | MAJOR · wrong-answer false-reject silent | 5 | INHERITED clause citations across the shared-verb diagnostics, the SET format ordinals and the SEARCH GR numbering: wrong §/rule numbers PRINTED TO US |
| `PB389` | MAJOR · false-reject under-reject silent | 17 | the object-reference descriptor is a SCALAR (one nullable class name) where §13.18.60.2 prints a TUPLE (kind × FACTORY × ONLY × name): ACTIVE-CLASS an |
| `PB390` | MAJOR · crash under-reject silent | 4 | a SYNTAX RULE decided at bind time is delivered as BoundUnsupported, so the compile succeeds and the user's running program dies with 'a COBOL feature |
| `PB391` | MAJOR · wrong-answer silent | 2 | CorrespondingBinder.CorrMoveValid is a SECOND, private, partial copy of Table 16 beside MoveTable16.Refusal, and the two have drifted in BOTH directio |
| `PB392` | MAJOR · crash false-reject under-reject silent | 1 | CORRESPONDING's SR6 names a SET of four group KINDS and the screen is the boolean `IsGroup`, so a BIT group is accepted and silently does nothing, whi |
| `PB393` | MAJOR · crash false-reject under-reject silent | 9 | VARIABLE-LENGTH and DYNAMIC-LENGTH group operands have no implementation in MOVE, INITIALIZE or CORRESPONDING: legal COBOL-85/2014 source compiles cle |
| `PB394` | MAJOR · wrong-answer silent | 3 | the sending operand / selection subject is re-rendered ONCE PER RECEIVER (and once per relational use), so a subscripted, reference-modified or functi |
| `PB395` | MAJOR · under-reject silent | 3 | EC-DATA-INCOMPATIBLE is armed by a STATEMENT-KIND test (`node is BoundMove`) where §14.6.13.2 hangs it on a SENDING-OPERAND REFERENCE, so GO TO DEPEND |
| `PB396` | MAJOR · under-reject silent | 2 | `statementBlock*` makes a REQUIRED imperative statement OPTIONAL in IF, EVALUATE and PERFORM, and the same flattening lets WHEN OTHER repeat anywhere; |
| `PB397` | MINOR · under-reject silent | 2 | the binder knows which SECTION and which METHOD a statement is in, but not which SENTENCE or which POSITION in a statement sequence, so every placemen |
| `PB398` | MAJOR · false-reject | 4 | two whole alternatives of EVALUATE's printed selection-object figure have no grammar: partial-expression-1 (`WHEN > 5`) and range-expression's `IN alp |
| `PB399` | MAJOR · under-reject silent | 4 | EVALUATE has no syntax-rule screen at the binder: a subject/object count mismatch is a run-time crash one way and a SILENT partial match the other, an |
| `PB400` | MAJOR · crash false-reject under-reject silent | 6 | EVALUATE's operand classification is wrong in three directions: a boolean SUBJECT crashes at run time, a SIGNED sole numeric literal is classified as  |
| `PB401` | MAJOR · wrong-answer silent | 1 | EC-RANGE-INVALID is raised only when BOTH ends of an EVALUATE range are string literals, so the exception fires exactly where the inversion is visible |
| `PB402` | MAJOR · false-reject | 1 | the one shared `raisingPhrase` hard-codes LAST EXCEPTION, but EXCEPTION is not underlined in the printed format, so the legal terse form `RAISING LAST |
| `PB403` | MAJOR · under-reject silent | 2 | EXIT's enclosing-CONTEXT rules are unchecked at BindExit: EXIT PERFORM outside any PERFORM compiles clean and HANGS forever, and EXIT PROGRAM in a FUN |
| `PB404` | MINOR · under-reject silent | 2 | the declarative-context rule family is wired to RESUME and never to EXIT or GOBACK, so EXIT PROGRAM in a GLOBAL declarative and RAISING LAST outside a |
| `PB405` | MAJOR · wrong-answer silent | 2 | EXIT PARAGRAPH and EXIT SECTION written INSIDE an inline PERFORM emit a bare C# `break` that binds to the PERFORM's loop instead of the paragraph disp |
| `PB406` | MAJOR · wrong-answer silent | 1 | 14.9.14.4 GR3's cross-reference into the GOBACK rules names two rules that cannot apply (a defect in the STANDARD, recorded so nobody 'fixes' the comp |
| `PB407` | MAJOR · false-reject under-reject | 1 | the GOBACK format's CHOICE INDICATORS were read as an ordered at-most-one stack, so four legal spellings are rejected (both phrases together, either o |
| `PB408` | MAJOR · wrong-answer silent | 1 | GOBACK RAISING asks the CALLEE whether checking is enabled where §14.9.18.4 GR1b asks the ACTIVATOR, so a declarative fires for a condition the activa |
| `PB409` | MAJOR · wrong-answer under-reject silent | 2 | the GLOBAL-declarative GOBACK rule PAIR is absent in both halves: nothing tests the enclosing declarative's GLOBAL flag (§14.9.18.3 SR1), and EC-FLOW- |
| `PB410` | MAJOR · false-reject under-reject silent | 1 | GOBACK RAISING LAST's placement rule (§14.9.18.3 SR5) is written in TWO arms and both are wrong in opposite directions: outside a method nothing is en |
| `PB411` | MAJOR · false-reject under-reject silent | 3 | the termination-status operand rules are wrong in BOTH directions: an alphanumeric group, which §8.5.2.1 gives usage display and §14.9.18.3 SR6 theref |
| `PB412` | MINOR · under-reject silent | 1 | ONE goToStatement alternative parses all three GO TO shapes as an optional-everything sequence and nothing narrows it back to the two general formats, |
| `PB210` | MINOR · under-reject silent | 1 | GO TO … DEPENDING ON identifier-1 applies NO operand-class screen: §14.9.17.3 SR1 requires \"a numeric elementary data item that is an integer\", Cont |
| `PB413` | MAJOR · wrong-answer silent | 1 | a format-1 GO TO written inside an INLINE PERFORM does not transfer: the emitter's bare C# `break` leaves the PERFORM loop instead of the __pc dispatc |
| `PB414` | MAJOR · wrong-answer silent | 2 | NEXT SENTENCE inside an inline PERFORM in the LAST sentence of a paragraph resumes in the SAME sentence instead of skipping to the next: a wrong answe |
| `PB415` | MAJOR · false-reject under-reject | 6 | INITIALIZE's category-name is a 5-of-13 SCALAR where the printed format draws a 13-alternative SET, TO is required in TO VALUE / THEN TO DEFAULT thoug |
| `PB416` | MAJOR · wrong-answer crash under-reject silent | 4 | INITIALIZE resolves identifier-1 and its REPLACING operands through NO screen: the class rule is unwritten, Table 16 is asked by MOVE and not by INITI |
| `PB417` | MINOR · under-reject silent | 1 | INITIALIZE's exception-checking-PERFORM ban is implemented as a RAW-PARSE-TEXT duplicate test where the rule is about operand COUNT, so `INITIALIZE G  |
| `PB418` | MAJOR · wrong-answer silent | 6 | INITIALIZE's TO VALUE arm substitutes 'the item has a Format-1 VALUE clause' for general rule 5's three-way qualification test, so a pointer or object |
| `PB419` | MAJOR · wrong-answer silent | 1 | INITIALIZE binds all its identifier-1 targets into ONE flat statement site with no per-target boundary, so a declarative that executes RESUME NEXT STA |
| `PB420` | MAJOR · crash | 2 | a STALE deferral in InitializeEmitter's first switch arm refuses the implicit MOVE into any COMP-1/COMP-2 receiver, so one float leaf makes the whole  |
| `PB421` | MINOR · under-reject | 1 | `moveReceivingPhrase` carries a second alternative that parses `MOVE <sending-operand> CORRESPONDING id-3 TO id-4`, a shape NEITHER general format of  |
| `PB422` | MINOR · under-reject silent | 3 | MOVE's SOURCE-SHAPE syntax rules are skipped, absent or misfiled: SR8 runs BELOW the Table-16 group filter so every group receiver escapes it, SR6 (ZE |
| `PB423` | MAJOR · under-reject silent | 1 | a STALE PREMISE in MoveBinder ('pointer, object-reference and message-tag classes cannot reach a bound MOVE — their usages are compile-gated skeletons |
| `PB424` | MINOR · silent process-only | — | COBOLNET0809 and COBOLNET0819, the two diagnostics that carry every §14.9.25.3 SR1 / SR7 / SR8 / SR10 rejection MOVE emits, are absent from docs/DIAGN |
| `PB425` | MAJOR · wrong-answer silent | 2 | §14.9.25.4 GR2/GR3's zero-length-literal SUBSTITUTION is implemented nowhere (only its dynamic-length EXCLUSION is), so on legal source MOVE \"\" TO P |
| `PB426` | MAJOR · wrong-answer silent | 1 | `CobolNum.FromAlphanumeric` accumulates EVERY digit of the sending operand into an Int128 with no cap and no guard, so §14.9.25.4 GR6 d)3's 31-charact |
| `PB427` | MAJOR · false-reject under-reject silent | 1 | `StrongTypeModel.SameStrongType` decides §14.9.25.3 SR2's 'same type' by type-NAME string plus member-NAME PATH, where §8.5.3.1 makes it declaration E |
| `PB428` | MAJOR · false-reject | 1 | an inline method invocation is a legal identifier-1 (§8.4.3.1.2 Format 4) and therefore a legal MOVE sending operand under §14.9.25.3 SR3, but the `:: |
| `PB429` | MAJOR · false-reject | 1 | PAGE-COUNTER is a legal identifier-2 (§8.4.3.15.3 SR1 admits it anywhere an integer data item may appear, and SR3 bars only LINE-COUNTER from the rece |
| `PB430` | MAJOR · wrong-answer silent | 1 | §14.9.25.4 GR4 diverges twice, silently, at all four editions: `MoveClassifier.IsGroupSender` excludes a level-66 RENAMES alias that §13.18.45.4 GR2 d |
| `PB431` | MAJOR · wrong-answer under-reject silent | 1 | `performInlineHead` is a REPETITION where §14.9.28.2's Format 2 prints ONE bracket over three alternatives, and `BindPerformControl`'s `FirstOrDefault |
| `PB432` | MAJOR · false-reject under-reject silent | 4 | PERFORM VARYING's FROM/BY slots are typed `arithmeticExpression`, simultaneously WIDER than the printed brace group (`FROM A + B` accepted) and NARROW |
| `PB433` | MAJOR · crash under-reject silent | 1 | §14.9.28.3 SR11 (a PERFORM range shall not straddle the declaratives boundary or two declarative sections) is implemented NOWHERE, and the canonical v |
| `PB434` | MINOR · under-reject silent | 2 | `CheckCrossStatementBans` is the ONE home for the Format-3 PERFORM's per-region bans and two obligations are missing from it: §14.9.28.4 GR16's transf |
| `PB435` | MINOR · process-only | — | a GREEN conformance test named for §14.9.28.3 SR15's bare-plus-FILE-paired arm has a VERBATIM copy of a different test's body and asserts a different  |
| `PB436` | MAJOR · wrong-answer silent | 2 | PERFORM VARYING … AFTER with TEST BEFORE performs §14.9.28.4 GR13 e) 2's two operations in the WRONG ORDER (the OUTER induction variable is augmented  |
| `PB437` | MAJOR · false-reject | 1 | `UdfStagePerEvaluationResidue` REJECTS legal source (COBOLNET1509) for a function-bearing PERFORM VARYING BY or AFTER-FROM operand, in a slot the comp |
| `PB438` | MINOR · process-only | 1 | §14.9.28.4 GR18 prints TWO consecutive sentences naming DIFFERENT destinations after imperative-statement-3; the compiler implements the second, the p |
| `PB439` | MAJOR · wrong-answer silent | 1 | the EC-RANGE-PERFORM-VARYING guard is keyed on a BOUND-NODE SHAPE (`lv.From is BoundNumRef`) where §14.9.28.4 GR3 says 'an identifier', so a FUNCTION- |
| `PB440` | MAJOR · wrong-answer silent | 10 | PERFORM of an EMPTY procedure range (a section with zero paragraphs, §14.4.2): `BindPerform` returns `BoundNop` when start > end and DELETES the whole |
| `PB441` | MAJOR · wrong-answer silent | 1 | §14.9.28.4 GR14's implicit TURN OFF ALL window covers imperative-statement-5, but imp-5's EC-SIZE guard is COMPILED IN from the AMBIENT `>>TURN` state |
| `PB442` | MINOR · silent | 3 | three b2 rows are PARTIAL on an EVIDENCE BURDEN rather than a measured wrong answer: §14.9.28.4 GR17's scope word is decided two different ways in one |
| `PB443` | MAJOR · wrong-answer under-reject silent | 3 | SEARCH never resolves identifier-1 as a data reference: one line reduces it to its BASE WORD and looks the name up, so a qualified `SEARCH E IN G2` se |
| `PB444` | MINOR · under-reject silent | 1 | there is NO compile-stage syntax-rule checker for SEARCH: the one all-formats rule the compiler reacts to is delivered as a run-time NotImplementedCob |
| `PB445` | MINOR · under-reject silent | 7 | SEARCH ALL never builds or consults a model of identifier-1's OCCURS KEY phrase and binds the WHEN as one opaque condition, so SR7 through SR13 — seve |
| `PB446` | MINOR · under-reject silent | 1 | the SEARCH grammar admits five constructs that appear in NEITHER general format, including a `KEY IS` phrase that is a DEAD rule no binder reads (acce |
| `PB447` | MAJOR · wrong-answer silent | 1 | ONE scan loop serves both SEARCH formats and its advance-then-test step is licensed for only one of them: an unsuccessful SEARCH ALL parks the search  |
| `PB448` | MINOR · silent process-only | — | the Markdown transcription broke two nested lists in the SEARCH general rules: one sub-item lost its indent and now mis-resolves to its own sibling (t |
| `PB212` | MINOR · under-reject silent | 3 | the SET RECEIVER has no class screen: §14.9.39.3 SR1 requires \"a data item of class index or an integer data item\", SetBinder.SetTargetOf applies no |
| `PB449` | MAJOR · crash under-reject | 2 | the SET format is selected from the FIRST receiving operand alone, so the SAME two operands give a correct SR23 diagnostic in one order and a run-time |
| `PB450` | MAJOR · false-reject under-reject | 4 | SET's printed OUTER REPETITIONS were dropped from the grammar in Format 4 and Format 7 while the identically-shaped Format-3 twin eighteen lines away  |
| `PB451` | MAJOR · crash false-reject under-reject | 10 | SET Format 5's receiver discrimination is implemented on ONE arm of each of two dispatches: the class-name sender never asks interface-vs-class, so th |
| `PB452` | MAJOR · false-reject | 7 | SET Formats 8 (function-pointer) and 15 (numeric-content) are MANDATORY base language with no surface at all, and Format 8's absence is MISFILED in th |
| `PB453` | MAJOR · false-reject | 5 | the SET formats with no surface (15 numeric-content, mandatory; 17 message-tag, A.3-declined) die as raw COBOL0001 parse cascades, so the user is told |
| `PB454` | MINOR · under-reject | 1 | a SPECIAL-NAMES switch clause written WITHOUT a mnemonic enters the switch-NAME itself into the mnemonic table, so `SET SWITCH-1 TO ON` binds and runs |
| `PB455` | MAJOR · crash false-reject under-reject silent | 2 | `SET condition-name TO FALSE` is accepted by BOTH grammars and implemented by NEITHER: the VALUE clause parses `WHEN SET TO FALSE literal` and throws  |
| `PB456` | MAJOR · crash under-reject silent | 2 | SET Format 5's semantic re-route fires only when the sender is EXACTLY one bare dataReference, so a literal or any expression sender drops an object-r |
| `PB457` | MAJOR · false-reject | 1 | the CAPACITY register is reachable only by a bare name whose table has no table ancestor, so a QUALIFIED register reference and EVERY reference to a N |
| `PB458` | MAJOR · wrong-answer crash false-reject under-reject silent | 4 | SET Formats 14 and 16 never screen the LITERAL amount, so a negative literal silently INVERTS UP BY / DOWN BY, and Format 16's bare arm drops a qualif |
| `PB459` | MAJOR · wrong-answer under-reject silent | 7 | every SET amount is narrowed by a bare (long) cast with no integrality test, and an index-name reaches SET as a bare long with no table, so a fraction |
| `PB460` | MINOR · under-reject silent | 3 | five Annex A.1 rows the SET clause depends on are absent from docs/CONFORMANCE.md (all five REQUIRED to be documented), and Annex A.4.4 is marked Clai |
| `PB461` | MAJOR · wrong-answer silent | — | the ALL-literal figurative rule is written down in THREE emitters and each copy covers a different half, so SET condition-name TO TRUE stores the raw  |
| `PB462` | MAJOR · crash false-reject | 1 | SET condition-name TO TRUE over a GROUP conditional variable emits NotImplemented.Value<string> into a record-struct receiver, so legal COBOL fails as |
| `PB463` | MAJOR · wrong-answer crash under-reject silent | 1 | SET [SIZE OF] narrows the new length through two UNCHECKED 32-bit casts, so a length past 2^32 silently wraps to its low bits and a length past int.Ma |
| `PB464` | MINOR · process-only | 1 | GR37's 'truncated to the nearest integer' is the standard's only use of that phrase and reads two ways; the compiler picks toward-zero, nothing docume |
| `PB465` | MAJOR · wrong-answer silent | 1 | CobolPtr.UpByReal folds a MAGNITUDE guard into GR19's integrality raise, so SET P UP BY an integral 1.0E19 sets EC-SIZE-ADDRESS and aborts the run uni |
| `PB211` | MINOR · under-reject silent | 1 | SEARCH … VARYING identifier-2 applies NO operand-class screen: §14.9.37.3 SR5 closes it to \"a data item whose usage is index or a data item that is a |
| `PB466` | MAJOR · under-reject silent | — | an unqualified paragraph-name that §8.4.2.2.1 requires to be qualified is resolved to the GLOBALLY-FIRST definition with no ambiguity check: the proce |
| `PB467` | MAJOR · false-reject under-reject | — | PtrResolveBased answers a SCOPED question from the FLAT unit-wide name map instead of the scope-aware SymbolTable, so inside a method the SR18/§14.9.3 |

**84 notes** — {'defect': 81, 'analysis': 3}; severity {'MINOR': 22, 'MAJOR': 62}; harm {'under-reject': 47, 'silent': 63, 'wrong-answer': 28, 'false-reject': 25, 'crash': 14, 'process-only': 5}; 78 claim at least one row.

### Four cross-note findings the fixer should land as ONE change each

1. **"Leave the dispatcher" is spelled as a raw C# `break` at five emit sites**, while "leave a PERFORM" is
   correctly spelled as a `goto` to a planted label. Inside an inline PERFORM — which lowers to a real C# loop —
   `break` binds to the loop, not the paragraph dispatcher. `PB405` (EXIT PARAGRAPH / EXIT SECTION), `PB413`
   (GO TO out of an inline PERFORM), `PB414` (NEXT SENTENCE) and `PB403`'s fallback are **one defect**; the
   repair is a per-paragraph dispatcher-exit label with the fall-through epilogue moved after it, plus a drift
   test asserting no bare `break;` survives inside a lowered loop.
2. **A syntax-rule verdict delivered as `BoundUnsupported` becomes a run-time abort** — `PB390`, measured on
   MOVE CORRESPONDING, PERFORM's procedure-names, SET's SR5/SR6 and SUBTRACT's SR6. A compile-time obligation
   discharged by generated code is not discharged.
3. **A scalar standing where the standard writes a SET** — `PB389` (`objectReferenceUsage` is 3 alternatives
   where §13.18.60.2 prints a 5-shape tuple, stranding 25+ OO rules as unreachable), `PB392` (CORRESPONDING's
   SR6 group KINDS as a boolean `IsGroup`), `PB415` (INITIALIZE's category-name as a 5-of-13 scalar).
4. **One rule written down in more than one place** — `PB391` (a second private Table 16 inside
   `CorrespondingBinder`), `PB461` (the `ALL literal` rule in three emitters, each covering a different half).

### Citations: two traps worth carrying forward

* **A `--check` OK is evidence about the TEXT, never about which RULE you meant.** §14.9.39.3 SR14 b)1. and b)2.
  are word-for-word identical to SR12 c)3. and c)4., so `cite.py --check` on that sentence returns **12)** and
  never 14). The fragment that does return 14) is SR14's own lead-in. Same shape as PB371's landing.
* **A transcription defect makes three general rules uncitable.** §14.9.13.4 GR4 a)'s sub-items 4., 5. and 6.
  are unindented and unescaped where 1., 2. and 3. are indented, so `--check` returns `4) a) 3.` for all three;
  §14.9.20.4 GR5 step c's four alternatives all resolve to `5) c) 1.`; §14.9.37.4's `1) b) 3.` resolves to its
  sibling. That is `PB385`'s family (105 sub-items at column 0), and `PB448` records the SEARCH instance.

### What the register now owes

`PB370` gains the **46** CONFORMS-but-untested rows as the golden lane's next input set — deliberately NOT
claimed in its `inventory_rows`, because those rows need a WITNESS, not a fix. `PB372` gains batch b2's **124**
`dossier_gaps`: all seven b1 shapes confirmed (the grammar list truncated over the load-bearing rule, 20 times;
`tests.named` counting an EMPTY test file as coverage, 4 times; the test list unable to tell a spec-derived test
from a legacy-oracle pin, 7 times) plus two new ones.

### Gates

```
dotnet build CobolSharp.sln -c Debug                                   0 Warning(s)  0 Error(s)
dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter
  "…SpecTraceabilityInventory|…DefectiveRowCoverage|…DerivedVerdict|…AnnexA1Register"
                                       Failed: 0, Passed: 47, Skipped: 0, Total: 47
python3 scripts/spec/audit_code_citations.py --check   2836 files scanned · 0 finding(s)
python3 scripts/spec/audit_doc_citations.py  --check   240 citations checked · 0 MISFILED
python  scripts/spec/work.py check                     523 work items, all well-formed
```

⛔ **The inventory gate is the one that caught my own repair.** The first `record_verdicts` apply passed the
SHAPE validator and then turned the battery red with **11 unresolved `code-location`s and 4 unresolved
`test-ref`s** — my normaliser had kept a trailing `.` inside a symbol (`BindSet.`), taken an ordinary English
word for one (`whose`, `binds`, `literal`), emitted a symbol that is simply not in its file, and left one
doubled path segment; and the adjudicators had written NAMESPACE-qualified test names where the gate resolves
`Class.Method`. The fix was not a longer stop-word list: the normaliser now RESOLVES every reference the way the
gate does before emitting it, and falls back symbol → bare path → drop. The inventory was reset with
`git checkout` and re-applied from clean so the GAP delta above is the true one.

