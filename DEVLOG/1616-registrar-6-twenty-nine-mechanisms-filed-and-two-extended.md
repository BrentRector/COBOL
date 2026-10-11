## Entry 1616 — 2026-09-19 12:20 PDT — REGISTRAR #6: twenty-nine mechanisms filed and two extended from the post-registrar-#5 reports — and three leads DIED on re-measurement, one of them a fix already landed

**The pass.** Registrar #6 read the "New defects" / leads sections of twenty-four implementer and finisher
reports (PB416, PB426, PB482, PB368, PB391, PB555, PB401, PB403, PB388, PB436, PB408, PB447, PB419, PB427,
PB443, PB396, PB411, PB420, PB425, PB877, PB391-finisher, PB405, PB522, PB502), the lander reports for
trains 33-38 and train-pb877, and DEVLOG entries 1603-1615. **Fifty-two leads.** Every one was grepped
against `kb/Work/` FIRST; every lead that survived was REBUILT as a probe and RE-RUN on this worktree's own
`dotnet build CobolSharp.sln -c Debug` (0 errors) at main `80dd97e2`; every citation was re-derived with
`python scripts/spec/cite.py --check`, and the command line is printed beside the quotation inside each
note. Twenty-nine notes filed as **PB878-PB906**, two notes EXTENDED with real content (PB388, PB446),
fifteen given `cluster:` back-links, and fourteen leads dropped with their owner named. **This pass moves no
verdict**: GAP is 2423 before and after, and `gen_conformance_notes.py` produced no diff.

**⛔ Three leads did not survive re-measurement, and that is the point of re-measuring.**

1. **A lead that was already FIXED.** The PB416 and PB391 reports both named `CorrespondingBinder` as a
   second hand-written copy of Table 16 that skipped §14.9.25.3 SR8. On this tree
   `CorrespondingBinder.cs:253` is one delegation — `MoveTable16.DataItemRefusal(src, dst) is null` — and
   `DataItemRefusal` chains SR8 → SR9 → SR10 in rule order. **PB391 and its finisher closed that arm**, and
   filing it again would have sent an implementer at working code. What is LEFT is two OTHER askers, and
   that is what `PB878` says: `InitializeBinder.cs:735` asks `ShapeRefusal ?? Refusal` and never SR9
   (measured: `INITIALIZE G REPLACING ALPHANUMERIC DATA BY <variable-length group>` aborts the run unit),
   and `OoConformance.cs:550`'s INVOKE BY CONTENT screen asks Table 16 alone where §14.8.2.3.3 rule 2 d)
   — `OK §14.8.2.3.3 2) d)` — says the whole MOVE question.

2. **A probe that was MASKED, and would have pointed the fixer at the wrong screen.** The PB522 report says
   `01 OUTER GROUP-USAGE NATIONAL. 02 INNER SAME AS SRC2.` (with `SRC2 PIC X(4)`) "compiles clean and runs".
   It does not — it is refused COBOLNET0881 under §13.18.60.3 SR12, a PICTURE/USAGE rule, which is a
   different question with the same answer. Remove the collision (`SRC2 PIC N(4)`) and the defect appears:
   `COMPILED-AND-RAN`, rc=0, where §13.18.49.3 SR9 refuses it. The control — the same entry under
   `01 OUTER USAGE DISPLAY` — draws COBOLNET1555 citing SR9 by name. Filed as `PB889`, with the masking
   recorded so nobody re-runs the original program.

3. **An INHERITED QUOTATION, caught by `--check`.** The same report quotes §13.18.57.3 SR5 with
   §13.18.49.3 SR9's wording. `--check 13.18.57.3 "shall not contain a GROUP-USAGE, SIGN, or USAGE clause"`
   **FAILS**; SR5 reads *"**No** group item to which the subject of the entry is subordinate **shall
   contain** a GROUP-USAGE, SIGN, or USAGE clause"* (`OK §13.18.57.3 5)`). Same obligation, different
   sentence; the ordinal was right all along. This is exactly the CA10 failure mode CLAUDE.md rule 1 names,
   caught one hop before it entered a note.

**⛔ And one probe of the registrar's OWN did not reach its defect — recorded rather than quietly re-scoped.**
`q20_fn_expr_ec.cob`, rebuilt for PB408's expression-position EC lead, was refused COBOLNET0717 under
§14.9.18.3 SR2 because the probe omitted the procedure division header's RAISING phrase. The refusal is
CORRECT. `PB892` is therefore filed from the CODE SITE, which is decisive about what is emitted
(`CallEmitter.cs:214`'s own comment says the invocation goes out WITHOUT `siteHandlesPropagation`), and says
in the note that the behavioural golden is the fixer's first step.

**The twenty-nine notes, by cluster.** Each carries a `cluster:` list so the fill hands ONE implementer the
whole group (owner decision 2026-09-13).

* **§14.9.25.3's askers** — `PB878` (two askers ask a subset) · `PB879` (SR5's three EDITION rows live in
  `VersionConformancePass.GateMove`, which matches a `BoundMove` INITIALIZE never builds, so
  `INITIALIZE G REPLACING NUMERIC DATA BY SPACE` compiles clean at 2023 and dies at run time where the
  explicit `MOVE SPACE TO N` is a clean COBOLNET0902) · `PB880` (the three remaining EMITTER-BUILT implicit
  moves are constructed downstream of `MarkFillImageStorage`/`StorageFormPass`;
  `ImplicitMoveConstructionDriftTests` enumerates all three and pins the hole open rather than closing it).
* **Receiving-operand routing** — `PB881`: ACCEPT and INSPECT resolve their receiver with `ctx.Refs.Resolve`,
  not `ResolveReceiving`, so §13.18.15.3 SR2 / COBOLNET1548 never fires. MEASURED: `INSPECT … REPLACING`
  rewrites a CONSTANT RECORD to `[bbbbb]` and `ACCEPT … FROM DATE YYYYMMDD` writes today's date into one,
  while the identical MOVE is refused. Claims `SR-13.18.15.3-2`.
* **Report writer** — `PB882` (a sum counter's identity is its SPELLING: two legal entries sharing a
  data-name share one counter; measured `0022  0022` where §13.18.54.4 GR1 owes `0011  0022`) · `PB883`
  (SUM's `arithmetic-expression-1` has no grammar surface) · `PB884` (a REPEATING entry emits its
  entry-level diagnostics once per repetition — COBOLNET1559 measured THREE times, identical text, identical
  (16,16) position) · `PB885` (a report `OCCURS … DEPENDING ON` operand loses its SUBSCRIPT in silence —
  three occurrences printed where `DEPENDING ON WS-TE (2)` = 2 owes two, while the converse UPON operand
  four lines away is refused COBOLNET2046).
* **MOVE intermediates** — `PB886` (a multi-receiver MOVE of a BOOLEAN reference-modified slice keeps only
  the leading bit: `ONE=[1011]` against `TWO=[1000][1000]`) · `PB896` (a zero-length GROUP sender does not
  take §14.9.25.4 GR1's ELEMENTARY route) · `PB895` (a zero-length literal is not edition-gated —
  `MOVE "" TO X` compiles at `--std 85`).
* **ACCEPT** — `PB887`: the transfer is a hand list of receiver categories where §14.9.1.4 GR6 says
  "according to the rules for the MOVE statement", and the list is one arm short — `ACCEPT` into
  `USAGE COMP-2` aborts the run unit. Claims `GR-14.9.1.4-6`.
* **Data description** — `PB888` (a compiler temp loses GROUP-USAGE: `LEN-FN=8` against `LEN-CTL=4` for the
  same description; the 3rd and 4th hand-listed description copiers) · `PB889` (above).
* **VALUE / condition-names** — `PB890`: §13.18.63.3 SR31's "only when" is disarmed by a guard whose reason
  is a GREEN GOLDEN that violates the rule — `2002/pb695_value_false_optional_words.cob:32` is
  `88 CN-ORDER VALUE 1 IN AL1 WHEN SET TO FALSE 0.`, a numeric singleton with IN. Both excluded shapes
  measured compiling clean at STRICT 2023.
* **Exception machinery** — `PB891` (the ambient checking flags are SET/RESET with no saved local, so a
  nested statement clears an enclosing statement's enable and an unguarded inner statement inherits it — and
  the correct `PushAllCheckingOff`/`PopAllChecking` shape is in the SAME FILE at `:637`) · `PB892` (above).
* **I-O** — `PB893`: CLOSE's EC-REPORT-NOT-TERMINATED dispatch falls straight through into the implicit
  CLOSE a RESUME NEXT STATEMENT abandoned, against §14.9.6.4 GR10. Claims `GR-14.9.6.4-10`.
* **Frontend** — `PB894`: **a PROGRAM PROTOTYPE DEFINITION cannot be written.** `PROGRAM-ID. X IS PROTOTYPE.`
  is COBOLNET0901 ("'PROTOTYPE' is a reserved word … and cannot be used as a user-defined word") where the
  byte-identical `FUNCTION-ID. X IS PROTOTYPE.` compiles rc=0. §11.10.2's general format prints
  `PROGRAM-ID. program-prototype-name-1 [ AS literal-1 ] IS PROTOTYPE .`. One construct, two arms, one
  built. Claims `FMT-11.10.2` and `SR-14.9.4.3-13` (the NESTED screen admits the element SR13 excludes).
  §14.9.14.3 SR7's program-prototype arm is unexercisable for exactly this reason, and a GREEN drift test
  pins the 0901.
* **Tooling and process** — `PB897` (`fetch-gnucobol-tests.ps1` DELETES `tests/external/gnucobol` and then
  runs an extraction GNU tar refuses on a drive-letter path, so "the two `ExternalCorpusPopulation` reds are
  the known worktree shape" is a gate that has stopped gating; `--force-local` is the WRONG repair — bsdtar
  rejects it) · `PB898` (`guard.sh`'s `LEGACY_DIVERGENT` has 12 names, `tests/nist/corpus.tsv` declares 13,
  SQ212A is the difference — and the count has moved since the report, which is the argument for the
  assertion rather than for the edit) · `PB899` (the CLI writes diagnostics in the OEM code page: `(ISO §8.9)`
  arrives as the byte `0x15`, verified by `od -c`, while `ProgramTable.cs:126` forces UTF-8 for a RUN
  program — and the corpus runner matches `.err` on an ASCII code substring, so no golden can see it).
* **Evidence and audits** — `PB900` (the ordinal arm of `audit_code_citations.py` is LINE-scoped, the exact
  weakness train 36 repaired for FORMAT names with `_owning_cite` and not for ordinals — train 37 measured
  the false finding at 203 → 204 and worked around it IN THE COMMENT; and the complementary hole is live:
  `EmitterState.cs:29` and `ControlFlowEmitter.cs:70` quote §14.9.14.4 GR5a with "and as yet" elided, which
  `--check` FAILS on and no gate runs) · `PB901` (~35 `*DifferentialTests` classes whose doc-comments claim
  spec behaviour over bodies that only assert agreement with the frozen oracle) · `PB902` (the burn-down's
  own witness-axis blindness — `GR-8.4.2.3.4-1` sat CONFORMS for weeks on five goldens that all vary the
  subscript's VALUE or CARRIER and hold its SHAPE fixed).
* **Standing questions** — `PB903` (two `specs/ISO_COBOL.md` transcription defects: §14.9.18.3 SR4's
  sub-items lost their indentation so `--check` answers `4) a)` for d)'s text, and §8.5.3.1 reads "relative
  byte **pr** bit position") · `PB904` (the MOVE de-editing EDITION axis is unsourced — a deleted gate said
  2002 with no §, `MoveTable16` admits it at every edition, the VCR has no row, and if the gate was right
  the compiler is under-strict at `--std 85` for the far more common direct MOVE) · `PB905` (`SEARCH ALL` is
  an O(n) ordered probe; §14.9.37.4 GR9 makes the method implementor-specified so NOTHING is owed for
  conformance — filed under the performance review dimension, and the note says so first) · `PB906`
  (COBOL0307's "A period may be missing" hint now WINS on a complete statement carrying a phrase the general
  format does not print, because the SLL duplicate that used to compete with it is gone).

**Two notes extended rather than duplicated.** `PB388` gains the current count (**203**, not the report's
195 — measured identically by trains 36, 37 and 38), the statement that flipping `--check-all` to `--check`
IS the definition of done for its remaining half, and the note that `SR-14.9.39.3-2` is an ADJUDICATION
before it is a repair (whether SR2 refuses `SET index-name TO integer-item` decides whether a very common
statement stops compiling). `PB446` gains a SIXTH divergence on `FMT-14.9.37.2` — `searchAllWhenClause+`
admits several WHEN phrases where the RENDERED Format 2 diagram puts the ellipsis on the AND bracket, not on
the WHEN brace.

**Fourteen leads dropped, with the owner named** — the pass working as intended. `SR26` → `PB552`;
`setAddressStatement`'s arity-one receiver → `PB450` (whose title already states it); the 203 ordinal
citations and `SR-14.9.39.3-2` → `PB388`; the audits' blindness to a bare ordinal and to a mis-read rule →
`PB838`; `searchAllWhenClause+` → `PB446`; `COBOLNET1535` allocated twice → `PB608`; PB427's strongly-typed
screen → FIXED in PB427; PB396's grammar comment naming a non-existent test → repaired in train 38; the
legacy oracle's SLL double-report → deliberately left (frozen oracle, deleted at P15); train 33's two
determinations → owner determinations, not defects; and `MoveClassifier.NeedsLengthFreeze` → **not on main
at all** (it came in with PB425's patch, which train 38 dropped whole; verified absent from
`MoveClassifier.cs` on this tree).

**One routing question the reports asked, answered.** The PB401 report asked the registrar to "file ONE note
and decide which row carries" the `EVALUATE … THRU … IN alphabet-name-1` rejection. **Both arms are already
owned and they are different notes**: `PB594` (open) owns the LITERAL form (`no viable alternative at input
'IN'`, `CobolExpressions.g4#valueRange`) and already claims `SR-14.9.13.3-3`; `PB843` (open) owns the
IDENTIFIER form, where the qualification connective claims the word and the result is COBOLNET1639. Nothing
was re-filed and no row moved.

**Gate, verbatim.** `dotnet build CobolSharp.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
`dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter "FullyQualifiedName~SpecTraceabilityInventory|FullyQualifiedName~DefectiveRowCoverage|FullyQualifiedName~DerivedVerdict"`
→ `Passed!  - Failed: 0, Passed: 47, Skipped: 0, Total: 47`. `python scripts/spec/work.py check` →
`✓ 939 work items, all well-formed`. `python scripts/spec/gen_conformance_notes.py` →
`4348 items · 2423 GAP · 14 clause notes + 1 dashboard` (no diff — this pass moves no verdict).
`audit_doc_citations.py --check` → `470 verbatim-shaped citations checked · 412 correct · ⛔ 0 MISFILED`;
`audit_code_citations.py --check` → rc=0, non-gating rule-ordinal arm **203**, unchanged from trains 36-38.
Every `inventory_rows` list is on ONE line (PB875: a wrapped list claims nothing and the
`DefectiveRowCoverage` gate fails OPEN). No compiler code changed and no diagnostic code was allocated — a
registrar changes neither.

