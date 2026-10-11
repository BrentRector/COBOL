## Entry 1608 — 2026-09-13 15:29 PDT — Landing train 35 — FIVE clusters in one landing: PB405+PB414 (leaving a paragraph stops being a bare C# `break`) + PB416 (the four §14.9.20.3 operand screens INITIALIZE never asked, over the one home of MOVE validity) + PB426+PB844 (the alphanumeric sending operand gets its two missing §14.9.25.4 GR6 d) rules) + PB482 (a SUM clause's operands are written references, bound where values are bound) + PB368 (the USE re-entrancy guard IS the EC-FLOW-USE raise site), GAP 2468 → 2456

**PB405 + PB414 — "leave the dispatcher" stops being a bare `break`.** C# binds `break` to the innermost
breakable statement, and this emitter lowers an inline PERFORM to a real loop and GO TO … DEPENDING to a switch —
so every transfer of control OUT of a paragraph written inside one was captured by that container, and the
paragraph's fall-through epilogue then overwrote the pc the statement had set. Four rules were wrong together:
EXIT PARAGRAPH degraded to EXIT PERFORM (§14.9.14.4 GR6), EXIT SECTION's target was discarded so the rest of the
section ran (GR7), and NEXT SENTENCE in a last sentence resumed inside the sentence it was told to leave
(§14.9.19.4 GR4/GR6). GR6/GR7's NOTE is the whole derivation — the return mechanisms are "those associated with …
PERFORM, SORT, and USE", and an INLINE PERFORM is none of them, so it has no standing to intercept the transfer.
The repair is structural and singular: `DispatchState.TransferOut/TransferJump/ResumeTransfer`
(`CodeGen/EmitterState.cs`) is now the ONE renderer of a paragraph-leaving transfer (`__pc = t; goto __xfer;`) and
`DispatchEmitter.EmitDispatchMethod` plants `__xfer: ;` between the `switch (__pc)` and the `__atExit` test —
only when a transfer was rendered, so a transfer-free unit's generated source is byte-unchanged — with the OO
`__MDispatch` arm getting its own `__mxfer` because a `goto` may not leave its member. Twenty-five sites
converted, of which eighteen were hand-copied RESUME-AT landings in the Ec/Call/Ptr/SeqIo emitters, so
§14.9.33.4 GR3's landing is now written once. EXIT SECTION's guard was re-derived rather than moved: set the pc,
then `if (__exitPc == SectionEndPc) return __pc;` (a C# `return` no container can capture), then the jump —
exactly GR7's "preceding any return mechanisms for that section"; the implementer's own note (§4) had predicted
the opposite order and correctness requires this one, which the landed paragraph records. Control-flow design
gains D1a and corrects D2/D3/D4/D6/D8/D9 (D6 still described EXIT PERFORM as `break`/`continue`);
`DispatcherTransferIdiomDriftTests` (Unit, 5 facts) re-measures the idiom over the GENERATED C# and is proved to
flag the pre-fix emission and to cover the OO arm. Goldens: `2002/pb405_exit_paragraph_section_in_inline_perform`
(PB405XFER02) with the negative `pb405-exit-paragraph-in-inline-perform-85` (COBOLNET0900 — EXIT PARAGRAPH is the
COBOL-2002 Format-4 introduction), and `85/pb414_next_sentence_last_sentence_inline` (PB414NEXTSENT85) covering
both the THEN and the ELSE arm — the ELSE arm had no test anywhere in the tree — with no negative beneath it,
NEXT SENTENCE being legal at all four editions. A citation sweep rode along: ten sites citing §14.9.19 GR6 for a
lowering BOTH arms share now cite GR4/GR6 by arm, and GO TO's §14.9.20 (that clause is INITIALIZE) became
§14.9.17 at five sites. Four rows DIVERGES → CONFORMS (GR-14.9.14.4-6/-7, GR-14.9.19.4-4/-6). Deliberately NOT
closed: GR-14.9.17.4-1 (PB413) and GR-14.9.37.4-1 (PB388) are fixed by this same repair — GO TO and GO TO …
DEPENDING out of an inline PERFORM now transfer — but they are other leads' rows and their owners re-measure.

**PB416 — the four §14.9.20.3 operand screens INITIALIZE never asked, and the MOVE-validity home that had to grow
to answer one of them.** INITIALIZE resolved identifier-1 with the plain reference resolver and its REPLACING
operands with none, so four syntax rules went unenforced at once: SR1's class list (an INITIALIZE of a
`USAGE INDEX` item was a silent no-op), SR4's "the implied MOVE shall be valid" (a `PIC A(4)` item ended up
holding `0000` where the explicit MOVE is COBOLNET0819), SR5's RENAMES prohibition (the check existed, cited the
rule, and its only call site sat on the resolve-FAILURE arm a level-66 entry never reaches — the violation
shipped as an unhandled run-time exception), and SR7's consequence that identifier-1 IS the receiving operand (a
CONSTANT RECORD was destroyed at run time, past a COBOLNET1548 whose own doc comment claimed INITIALIZE as a
caller it never had). All four were re-reproduced on today's tree before anything was written; the note's §1 line
numbers had gone stale behind PB415 and PB418 but every claim held, and two cases the note did not list
reproduced as well and are fixed (REPLACING NUMERIC BY an index item; REPLACING ALPHANUMERIC BY a `PIC N` item).
Three of the four repairs are ROUTINGS rather than checks: identifier-1 now resolves through
`ExpressionBinder.ResolveReceiving`, so all six receiving-operand prohibitions reach it (COBOLNET1548 among them)
and the run-time `InitializeErrorAction` staging is deleted; SR5's check moved onto the resolved item, which also
fixed qualification; and SR4 asks `MoveTable16`. That last one forced the estimate up (CLAUDE.md rule 5) —
"shall be a valid MOVE statement" is the whole statement, not the table — so **`MoveTable16` is now the one home
of the category-keyed MOVE-validity question**: `SenderPosition` moved out of `MoveBinder` (the duplicate
deleted), `ShapeRefusal` holds §14.9.25.3 SR8 + SR7 + SR6 and `SenderClassRefusal` holds SR1's sending half over
the one `IntrinsicArgumentRules.ClassOf`, with `MoveCategoryLegality` and `MoveFigurativeEditionGates`
delegating. Both arms were fixed, not one: closing SR4 closed §14.9.25.3 SR6 on the EXPLICIT MOVE path too, where
it had never been implemented because a figurative constant has no Table-16 row by construction — `MOVE ZERO TO`
a `PIC A` item is now COBOLNET0819, a deliberate behaviour change whose blast radius was measured statically
(zero hits across `tests/`) and pinned by its own negative. One reading was corrected mid-flight: `ALL ZERO` IS
the figurative constant ZERO (§8.3.3.6.2 format 1 prints ALL un-underlined), so SR6 refuses it; only format 6's
`ALL "0"` is a different constant. New codes COBOLNET2030 (SR1) and COBOLNET2031 (SR4's MOVE half);
`InitializeLaneDriftTests` gains three facts, the third of which asserts that every one of the category-names is
screened by exactly one half of SR4 — falsified once by deleting the ALPHABETIC row, then restored. Golden
`85/pb416_initialize_operand_screens_85` (PB416OS85, identical at all four editions) plus seven negatives. Rows
SR-14.9.20.3-1/-4 NOT-IMPLEMENTED → CONFORMS, -5 DIVERGES → CONFORMS, -7 PARTIAL → CONFORMS. NOT closed:
SR-14.9.25.3-6 is now implemented and pinned but belongs to §14.9.25.3's own inventory — the registrar
re-verdicts it.

**PB426 + PB844 — the alphanumeric sending operand gets its two missing rules, as one mechanism.**
`CobolNum.FromAlphanumeric` accumulated every digit of the sending operand into an unguarded `Int128`, so
§14.9.25.4 GR6 d) 3's 31-character cap — the rule that exists precisely to keep an implementation carrier
unreachable — was not implemented, and `MOVE <PIC X(40) all digits> TO PIC 9(9)` stored the wrap 838277934 where
the rule gives 234567890, identically at all four editions. GR6 d) 1's closing sentence was missing from the same
decode: an alphanumeric sender whose content would be false in a numeric class condition was valued silently
instead of setting EC-DATA-INCOMPATIBLE, even with the condition explicitly TURNed on. Both are the same clause
about the same operand reached from the same five emit sites, so PB844 was FOLDED IN WHOLE and they landed as one
change — numeric design **D25**, one method per rule: `FromAlphanumeric` (the size rule — the rightmost 31
CHARACTER POSITIONS, which makes the carrier unreachable by construction), `FromAlphanumericSending` (the checked
read, the twin of `ParseImageSending`, whose predicate is the one `CobolClass.IsNumeric`) and `DigitMagnitude`
(no size rule at all, for the storage-form bridges — `CobolTable.Occ`, `CobolString.RefModPosition`,
INSPECT ×3 — whose image size their own data description already fixes). Which reference gets the check is
`SendingRef`'s answer through a new `MoveToNumeric` context, so both UNSTRING arms and INITIALIZE inherit it
while an arithmetic operand correctly raises nothing. Two hardenings rode along because the new member would
otherwise have switched a rule off silently: `SendingRefRules.FloatChecked` was rewritten from `r is Normal` to
the standard's exclusion list (EC-DATA-NOT-FINITE), and `RuntimeApi.NumFromAlphanumeric`'s `sending` parameter
was made REQUIRED so no call site can inherit the unchecked read. The note's own plan was corrected in the
landed paragraph: routing the bridges through `DigitMagnitude` is hygiene, not a fix — it was measured to change
no answer, because the position the window would drop is a leading zero in every reachable case. Goldens
`85/pb426_alnum_sender_31_character_cap` (PB426C85, 8 legs), `2002/pb426_alnum_sender_31_digit_receiver`
(PB426W2002) and `2002/pb426_alnum_sender_data_incompatible` (PB426E2002, PB844's positive control), plus the
negative `pb844-ec-data-incompatible-turn-below-2002`. `SendingRefDriftTests` pins all three readings. Row
GR-14.9.25.4-6 DIVERGES → CONFORMS.

**PB482 — a SUM clause's operands are written references, and the value is bound where values are bound.**
`BindSumClause` captured every addend with `KeyReference` — the FILE STATUS key helper, which keeps the base word
and the IN/OF qualifiers and drops the subscript and the reference modifier — and every UPON detail-name with the
raw first-word reduction. That one sentence produced a crash and four silent defects, all measured:
`SUM WS-CELL(2)` compiled and ABORTED the process at the first GENERATE; `SUM WS-TXT(1:2)` summed the whole
six-digit item and `SUM WS-TXT` over a `PIC X(6)` summed its digits, because §13.18.54.3 SR5's "shall specify a
numeric data item" had no site anywhere; UPON a control footing and UPON an undeclared word both compiled clean
and totalled nothing, because SR7's "shall be the name of a detail" had none either; a second `SUM … UPON …`
group silently replaced the first, where §13.18.54.4 GR1 gives the ENTRY one counter; and `SUM OF` — the general
format's optional word, confirmed against the RENDERED PDF page (OF not underlined) — was a parse error. The fix
is the shape the note predicted and the sweep widened: an addend is an ordinary identifier (§8.4.3.1.2 format 2,
qualified-data-name-with-subscripts), so it is captured whole by `SumAddendRef` and BOUND IN THE PROCEDURE PHASE
through the one `ExpressionBinder.BindExpr` — the PRESENT WHEN / VARYING precedent — and rendered by the one
`NumericRenderer`, which is what lets a subscript, an index-name and an `IX + 1` all resolve with no second copy
of the subscript machinery. `ResolveSumAddend` is the one arm choice over SR4 / SR4 g) / SR5, and it makes the
cross-report staging reachable for the first time: `dataReference` had been swallowing the `OF` qualifier before
`sumOperand`'s own alternative could see it, so that arm was dead code. `UponDetailRef` + `ResolveUponDetail`
give SR7 its screen through `ReportGroupResolution` (three consumers now), and the counter carries a LIST of
`ReportSumTerm`s, each with its own UPON filter, in the runtime (`SumEntry`/`SumTerm.Fires`) as well as the
binder. New codes COBOLNET2045 (SR5's category screen) and COBOLNET2046 (SR7). The SOURCE clause's identical
operand is the one arm left deliberately bare — a loud refusal, rewritten by PB506 in train 33 — and that
asymmetry is adjudicated in `ReportSumOperandCaptureDriftTests`'s own table (5 facts) rather than remembered; the
same guard caught PB506's `BindSourceOperand` rename during the rebase. Golden
`85/pb482_sum_addend_written_reference_85` plus four negatives. Rows SR-13.18.54.3-5 and -7 → CONFORMS.

**PB368 — the re-entrancy guard IS the EC-FLOW-USE raise site.** §14.9.49.4 GR2 is one sentence and its whole
normative content is a raise — "During the execution of a USE procedure, if a statement raises an exception
condition that would cause the execution of a USE procedure that had previously been activated and had not yet
returned control to the activating entity, the EC-FLOW-USE exception condition is set to exist." The compiler had
the consequence and not the condition: the generated `__RunUse` returned quietly on `__useActive[__id]` and
EC-FLOW-USE appeared nowhere but its `ExceptionCatalog` row, so a file-error declarative whose body re-OPENs the
same absent file printed `D1-ENTER / D1-EXIT FS=35 / AFTER` — the recursion invisible, the Table 13 Fatal default
unable to fire. EC-FLOW-USE is now an ordinary AMBIENT fatal condition — the five declarations `CheckingFlags`
already documents, no new mechanism — and the RAISE is written in `DispatchEmitter.EmitRunUseBody`, the ONE body
every selection path reaches an active USE procedure through, so `__IoCheck`, `__IoCheckEc`, `__EcDispatch`,
`__EcObjDispatch`, `__RunGlobalUse` and the report engine's BEFORE REPORTING hook all inherit it rather than each
growing a copy (every path measured, not assumed). The gate is ambient per statement and had to be: GR2's subject
is *a statement* that can reach a declarative, which is every I-O verb, every RWCS verb, a RAISE and a CALL — the
repro's own inner OPEN has an EMPTY enabled set, so a precise node-kind query fails the very case, and the
syntactic "inside DECLARATIVES" reading is wrong outright because a declarative may PERFORM a paragraph anywhere.
Two neighbouring guards were asked the same question and answered NO on the record: the exception-checking
PERFORM's imp-2/3/4 handler ranges share `__RunUse` and its `__useActive` array but are §14.9.28.4 GR17, not GR3,
so the guard raises only for `__id < DeclCount`; and `__dbgBusy`'s USE FOR DEBUGGING guard is EDITION-DISJOINT —
that declarative is 85-only, EC-FLOW-USE is 2002+ — which is now said in the code. Determination D11 holds:
checking OFF means decline to re-enter and continue, byte-identical to before, with zero scaffolding emitted. A
look-alike was turned up and REFUTED: `__RunGlobalUse` discards `__RunUse`'s resume action exactly as PB141's
defect did, but §14.9.33.4 GR1 makes a RESUME inside a global declarative equivalent to CONTINUE, so the discard
is the rule — now cited at the call site so the two sites can be told apart. Golden
`2002/pb368_flow_use_reentrancy` (PB368FU: `D1-ENTER / FLOW-USE-HANDLER / D1-EXIT FS=35 / AFTER`) plus the
negative `pb368-flow-use-name-below-2002` (COBOLNET0878 — the exception-NAME's own introduction band, carrying no
`>>TURN` so COBOLNET0900 cannot pre-empt it) and six `FlowUseReentrancyTests`, falsified once. A doc defect was
swept on the way: the conditions/exceptions deep-dive's per-edition list called EC-FLOW-* a 2023 family where VCR
row 40 names only the three commit-and-rollback names — left standing it would have argued this fix into the
wrong edition band. Row GR-14.9.49.4-2 DIVERGES → CONFORMS.

**The train.** Five clusters, one landing, no cluster dropped. Rows 1, 2, 3 and 5 based on `a2255819`/`b929f96e`
(before trains 33 AND 34) and row 4 on `d80cb676`, so every cluster was three-way merged onto train 34's head
`db68907f`. ONE conflict, in `DiagnosticCatalog.cs`: train 33/34's COBOLNET2012–2026 block against PB416's
COBOLNET2030/2031 block, resolved keep-both because each side is a whole set of descriptor declarations. The
train-33 lesson was applied structurally rather than by inspection — no corpus `manifest.json` and no
`traceability-inventory.json` hunk was three-way merged at all, because a conflict INSIDE one JSON list element
is silently lossy: every golden was inserted into its manifest by a script that parses the JSON, asserts the
`enabled` element COUNT before and after, and asserts that no existing element moved (2002 272 → 276, 85 81 → 85,
negative 1084 → 1098), and all five verdict batches were re-applied from their batch files in manifest order onto
the merged inventory. `docs/DIAGNOSTICS.md` was regenerated from the catalogue rather than merged. Per-cluster
conflict-marker checks (working tree and index) and a touched-path cross-check ran before each checkpoint commit;
the one real intra-train overlap — row 1's transfer rewrite against row 5's `DispatchEmitter`/`EcEmitter` hunks —
merged clean and both sides were verified present by name before the gate. Union filter over the clusters'
terms, extended with `~Corpus` (all five clusters add goldens, and only `CorpusRunnerTests` executes them) and
`~Nist`: `~SectionDifferential|~ControlFlowDifferential|~Initialize|~MoveTable16|~move_|~MoveAlphanumericSender|~ReportWriter|~Exception|~Use|~Declarative|~Drift|~EditionGate|~Nist|~Corpus`, every term measured live by `filter_population.py`. Four terms the clusters named are Unit-only drift tests and were DROPPED from the
Conformance filter after `filter_population.py` measured them dead there — `~DispatcherTransferIdiom`,
`~SendingRef`, `~ReportSumOperandCapture`, `~FlowUseReentrancy`, each live in the UNFILTERED Unit leg (5, 4, 5
and 6 tests). **The gate went RED once, and the red was the train's own guard doing its job.** PB482's new
`ReportSumOperandCaptureDriftTests.EveryKeyReferenceCapture_IsANamedOperandHelper` — a containment scan that
requires every `KeyReference` call in `DataBinder.Reports.cs` to sit in a named capture helper — flagged
`ReportOccursOf` (line 778), which is train 34's PB562 code and therefore invisible to every cluster's own gate.
It was ADJUDICATED, not silenced: `OCCURS … DEPENDING ON data-name-1` (§13.18.38.2 format 3) writes
DATA-NAME-1, not identifier-1, and that position admits qualification ONLY — §8.4.2.2.2 format 1 is the
qualified-data-name, which carries no subscript, and §8.4.3.3.3 SR5's NOTE bars reference modification "where
data-name-n is used in a general format or syntax rule" — so the key helper's name-plus-IN/OF pair IS the whole
written reference there and nothing is dropped, the exact opposite of §13.18.54.3 SR5's addend. The row was
added to the guard's adjudication table with that derivation and its residue named in the same comment: the
grammar's `dataReference` will still PARSE `DEPENDING ON WS-T(2)` and drop the subscript in silence, where
COBOLNET2046 refuses the analogous subscripted UPON operand — PB562's clause, reported to the registrar rather
than fixed under someone else's note. That one red is also the guard's falsification: it failed on this tree
before the row and passes after it. Conformance **3137 / 3137**, the UNFILTERED `Cobol.Net.Tests.Unit`
assembly **24027 / 24027**, Characterization **33 / 33**, the legacy `CobolSharp.Tests.Integration` assembly
**503 / 504** (1 skipped, unchanged from train 34). The GnuCOBOL corpus fetched cleanly here, so PB405's two environmental
`ExternalCorpusPopulationDriftTests` reds are green on the merged tree — the attribution confirmed rather than
inherited. Semgrep: no count increased; `cobolnet-raw-diagnostic-code-literal` fell 419 → 416
and the baseline was deliberately left untightened so a concurrent train's CI is not reddened by a ratchet it did
not cause. GAP 2468 → 2456 (12 rows). Notes flipped to `landed`: PB405, PB414, PB416, PB426, PB844, PB482,
PB368. Diagnostic codes claimed: COBOLNET2030, 2031, 2045, 2046 — next free is COBOLNET2047. One finding for the
register, PRE-EXISTING and not this train's: `EmitterState.cs` and `ControlFlowEmitter.cs` quote §14.9.14.4 GR5a
as "the most closely preceding, unterminated inline PERFORM" where the standard reads "the most closely
preceding, and as yet unterminated, inline PERFORM" — the clause number is right, the quotation is elided, and
`cite.py --check` fails on it; it is present at `a2255819` and older.

