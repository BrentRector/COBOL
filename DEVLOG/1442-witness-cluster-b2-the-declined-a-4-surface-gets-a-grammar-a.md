## Entry 1442 — 2026-09-02 23:24 PDT — Witness cluster B2: the declined A.4 surface gets a grammar, a pass
and a name — and the THREE declined-facility seams three implementers built in parallel become ONE, which
is also how a merge defect and a CI-red landing defect of B1's were caught

The last landing before the comprehensive battery. Three declined modules gain the surface they never had:
**A.4.14 VALIDATE's data-division clauses** (COBOLNET1708 — DEFAULT §13.18.17, DESTINATION §13.18.18,
INVALID §13.18.31, PRESENT WHEN format 2 §13.18.41, VARYING's validation leg §13.18.64,
VALIDATE-STATUS/VAL-STATUS §13.18.62, and the §13.18.63 format-5 content-validation entry), **A.4.3's
I-O-CONTROL APPLY COMMIT clause** (COBOLNET1709, §12.4.6.3) and **every declined module's EXCEPTION-NAMES**
(COBOLNET1710) at all six sites Annex item 10 names. Measured before → after: every VALIDATE clause was
`COBOL0001`/`COBOL0307`; `APPLY COMMIT` was `COBOL0001: unexpected 'COMMIT'`; `>>TURN EC-VALIDATE`,
`RAISE EC-VALIDATE-CONTENT` and `USE … EC-FLOW-APPLY-COMMIT` compiled **CLEAN**. 21 negative witnesses plus
three positive controls, including `2023/declined_rw_present_varying_control`, which proves the SHARED
report-writer legs of PRESENT WHEN and VARYING still compile AND RUN — that module IS claimed (A.4.11 items
14 and 20) and the two legs are told apart by WHERE the clause is written.

**The mechanism is a new grammar file plus a THIRD conformance pass.** `Grammar/Core/CobolDeclined.g4` holds
the declined surface, every rule behind a LEFT-EDGE `{is2002()}?`/`{is2023()}?` predicate;
`Validation/DeclinedFacilityPass` is a parse-tree walk, a sibling to `VersionConformancePass` and
`FlagConformancePass` on a third orthogonal axis — edition · directive · CLAIMED SUPPORT. It has to be a
PARSE-level walk because a declined clause has no bound node at all: `BindEntry`/`BindCondition` return early
on levels 66 and 88, and the UNNAMED level-88 IS §13.16.2 format 4. `DeclinedFacilityDriftTests` derives the
obligation FROM the grammar file — every entry-point rule must have a `Visit` refusal override — so a
declined construct cannot ship un-refused.

⛔ **ONE SEAM, NOT THREE. Three implementers cut from the same base each built one**, and all three were on
the tree at merge time: witness A's `EditionContext.Declined(descriptor, seen)`, B1's
`EditionContext.Report(descriptor, message)` (folded at DEVLOG 1438) and now B2's
`EditionContext.Removed(descriptor, message)`. **`Declined` survived** — it already put the severity in the
catalogue rather than at the site, which is the property the other two were re-deriving. What B2 had that
`Declined` did not is the `--permissive` arm, and merging it forced the question `Declined`'s own doc had
answered too broadly: it said a decline never moves under `--permissive`. **That is right for four codes and
wrong for three**, and the axis is the one that already splits Warning from Error — whether an inert compile
could change the ANSWER. 1708/1709/1710 downgrade and bind to a no-op, because their sibling STATEMENTS are
already accept-inert (1579/1580) and a declined module's exception-names raise nothing either way; nothing
observable changes. 1560/1705/1706/1707 do not move in either direction, because an inert screen `ACCEPT` is
re-read as the device ACCEPT and moves the wrong data, an inert `FORMAT` changes which bytes reach the medium,
an inert `WRITE FILE` writes the wrong record. The fact is now a field on `DiagnosticDescriptor`
(`PermissiveInert`), read in the seam and nowhere else — one method, one decision table, the policy in the
catalogue where it is documented and drift-tested.

**And the EC-name refusal collapsed from two mechanisms into one TABLE — which fixed a two-arm miss nobody
had noticed.** B1 put the screen refusal in `EcNameResolution.Advise`, the resolution funnel every writing
site reaches; B2 put its `DeclinedEcFamilies` check at the tail of `TryResolve`, which the two RAISING sites
that call `Advise` DIRECTLY (`EcAddPdRaisingWord`, `DataBinder.Oo`) never enter — B1's own comment names
those two arms as exactly the ones a per-site check would miss (feedback_two_arm_dispatch). The merge keeps
**B2's table and B1's placement**, and the table's row now carries a DESCRIPTOR: EC-SCREEN is one row at
COBOLNET1707 (so B1's four `.err` witnesses keep naming the construct), EC-VALIDATE and the three EC-FLOW
commit names are rows at 1710, and the next declined module is one row. `ScreenFacility.ReportExceptionName`
is deleted; the five inventory rows that pointed at it were re-verdicted onto the funnel, caught by
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` — a drift test earning its seat.

⛔ **A MERGE PUT A GRAMMAR ALTERNATIVE IN THE WRONG RULE, AND ONLY THE FULL SUITE SAW IT.** B2's hunk adds
`| {is2002()}? validationClause` as the last alternative of `dataDescriptionClause`; its diff context is
"the last alternative of a list, then `;`", and witness A had since inserted `selectWhenClause` **and a new
`conditionName` rule** immediately after that list. The three-way merge matched `conditionName`. Nothing
failed to build, no conflict was reported, and the declined-validate-* negatives simply drew the generic
parse error again — a silent merge defect whose only symptom is a test. It is the reason B2's report insisted
on the FULL Conformance project rather than a wave filter, and the reason that insistence was right.

⛔ **AND THE FULL SUITE CAUGHT A LANDING DEFECT OF B1'S THAT HAD BEEN RED ON CI SINCE `60bf02d7`.**
`ReservedWordPositionConformanceTests.DataItemNamedColumn_Rejected0901_EvenAt85` expected COBOLNET0901 and
got `COBOL0001: rule displayStatement failed predicate: {!screenPositionAhead()}?`. B1 wrote
`DISPLAY ({!screenPositionAhead()}? (dataReference | literal | functionCall))+` — the predicate guards EVERY
iteration INCLUDING THE FIRST, so a DISPLAY whose first token is a positioning word has no viable reading at
all and the rule dies on a raw parse error. Its comment defended the guard as safe because "none of them can
legally be a DISPLAY operand, so nothing legal is lost" — true of LEGAL source, and it misses that ILLEGAL
source must still reach its NAMED diagnostic: `01 COLUMN PIC 9.` … `DISPLAY COLUMN.` is a §8.3.2.1 rule-1
violation whose answer is COBOLNET0901 from the §8.9 funnel. Requiring the first operand unconditionally and
guarding only the CONTINUATION keeps both answers, and B1's `DISPLAY SG COLUMN 5` witness stays green. This
is the `left_edge_predicates` memory in its second form: not "the predicate is mid-alternative" but "the
predicate is on the mandatory first iteration of a `+` loop". The two sibling `( … )*` loops in `valueClause`
were checked and already have the correct shape; `acceptStatement` takes one unguarded operand.

**Notes.** `PB261` (A.4.3) and `PB283` (A.4.14) flip to landed, PB261 after correcting the stale premise its
title still carries: COBOLNET1579 has existed since Wave H, what was owed was the ASSERTION that it fires
plus the CLAUSE half, and with APPLY COMMIT now REFUSED, §14.9.7.4 GR4 is unobservable BY CONSTRUCTION —
which is what makes DOCUMENTED-NON-SUPPORT the honest verdict rather than a deferral. `PB284` gains B2's
mechanical re-derivation of the catalog hole: `specs/ISO_COBOL.md` heads §14.9.7.2 "General forms" — PLURAL,
the only such heading against 282 — so `FMT-14.9.7.2` is absent from the population the burn-down measures
against, and `parse_gaps` is `[]` so nothing flags it. Three new notes: **`PB376`** (`gen-grammar-diagrams.ps1
-Check` builds in a FIXED shared temp path and recursively deletes it, so the drift test races across
concurrent agents), **`PB374`** (the MCS module's two remaining silent surfaces —
`>>TURN EC-MCS-ABNORMAL-TERMINATION CHECKING ON` compiles clean and `01 WS-TAG USAGE MESSAGE-TAG.` is a bare
`COBOL0001`, both re-measured here; ⛔ its fix is the accept-and-WARN posture, NOT a refusal row, because MCS
is Annex A.3 processor-dependent and §4.2.6's licence reads the other way), and **`PB375`** — the CLASS clause.

**⚖ OWNER DECISION, taken during this landing: the CLASS clause (§13.18.11) is DECLINED WITH VALIDATE.**
§13.16.2 Format 1's printed `validation-clauses` block INCLUDES `class-clause` while Annex A.4.14 never lists
CLASS, so the two readings differed in which way the compiler is wrong — declined (silent acceptance is an
under-rejection) or mandatory (any refusal rejects legal source). The owner chose declined: it joins the
A.4.14 family in COBOLNET1708's band, its rules go to the `validate-only` selector, and it is witnessed like
the others. `PB375` records the decision as DECIDED and lists what implementing it needs — the grammar
surface behind the 2002 predicate in `CobolDeclined.g4`, the `DeclinedFacilityPass` override, the selector arm
and one witness. **A follow-up implementer owns that work; it was deliberately not done here.**

**Also.** COBOLNET0822 cited "ISO §12063" — a stray line anchor, not a clause — repaired at three sites to
§9.3.5.3 / A.4.10 item 3, and 0822 has its first negative fixture. A `STATUS.md` checkpoint file had reached
main in the two previous landings; it is `git rm --cached`d here and stays untracked in the worktree.

**Rows and gates.** 108 rows DOCUMENTED-NON-SUPPORT, GAP **3223 → 3119** (104 net — four had already closed).
Build 0 W / 0 E; **FULL Conformance Passed 5515/5515** (7 m 47 s, NIST and the '85 acceptance suite included —
the filter would not have reached either, and new lexer tokens must face them); Unit (widened) Passed 158/158;
Characterization 33/33; `work.py check` 431 items; `audit_code_citations --check` 0 findings over 2684 files;
`audit_doc_citations` 6 MISFILED (baseline); `audit_annex_a1 --check` no findings; semgrep unchanged at
36 / 439.

