## Entry 1313 — 2026-08-18 08:30 PDT — PB78 lands: OBJECT-COMPUTER's computer-name is optional before its clauses, and CHARACTER CLASSIFICATION is diagnosed, not swallowed

**PB78.** `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.` was `COBOL0001: unexpected 'PROGRAM'`. §12.3.6.2's
format is `OBJECT-COMPUTER. [computer-name-1] [ | CHARACTER CLASSIFICATION … | PROGRAM COLLATING SEQUENCE … | ] .`
— the name optional, the two clauses in any order, each at most once (the figure notes; §5.2.6.4). The grammar had
X3.23-1985's shape, every clause hung off a required name, and every golden happened to write one.

- **The grammar.** `objectComputerParagraph : OBJECT_COMPUTER DOT ((computerName computerAttributes?)?
  objectComputerClause* DOT)?`; `sourceComputerParagraph` the same shape. Two things learned the hard way: the '85
  attribute SINK (`~(DOT | PROGRAM | CHARACTER)+`, MEMORY SIZE / SEGMENT-LIMIT / DEBUGGING MODE for the version
  pass's token scan) must stay BEHIND the name — the first cut let it start without one and it swallowed the next
  paragraph header (`SOURCE-COMPUTER.` ate `OBJECT-COMPUTER`); and it now stops at CHARACTER as well as PROGRAM,
  so both standard clauses are recognized rather than eaten. The name-less clause form is the 2002 relaxation of
  the '85 required-name format: `computer-name-optional-2002` (COBOLNET0900 below 2002,
  `VisitObjectComputerParagraph`; the empty paragraph was legal at '85). A duplicate clause is COBOLNET1652.
- **CHARACTER CLASSIFICATION** — Annex A.4.9 item 7 of the optional locale module — is PARSED SO IT CAN BE
  DIAGNOSED (`CHARACTER {classificationAhead()}? cobolWord …`; CLASSIFICATION is a plain word): the binder rejects
  it with the LOCALE clause's COBOLNET1518, and its words are exempt from the §8.9 funnel (SYSTEM-DEFAULT drew a
  false "used as a user-defined word" beside the true diagnostic — the PB27 shape). Before: swallowed silently by
  the sink after a name, a parse error without one — CONFORMANCE.md §4 item 5 had it on record.
- **Measured.** Golden `pb78_object_computer_optional_name` (`SOURCE-COMPUTER.` / `OBJECT-COMPUTER. PROGRAM
  COLLATING SEQUENCE IS REV.` → `PCS=REV`, "A" > "B" under REV); negatives `pb78-object-computer-clause-without-
  name-85` (0900), `-character-classification` (1518, exactly one diagnostic), `-duplicate-clause` (1652); the
  named forms (`pb3_ord_collating_tail`, the sink with MEMORY SIZE at 85) unchanged. Fourteen verdicts (FMT-12.3.6.2,
  FMT-12.3.5.2, SR-12.3.6.3-4, SR-12.3.5.3-1, GR-12.3.6.4-1/-2/-3/-4/-6/-9/-11 CONFORMS; -5/-7/-8
  DOCUMENTED-NON-SUPPORT), GAP 4003 → 3989. The A.1-127 row (no equipment configuration) is on CONFORMANCE.md.
- **Registered PB92** (the sibling the sweep found): the SET LOCALE formats 11/12 (A.4.9 item 9) are still
  1639/0901 noise or a parse error rather than 1518.

**Gates (wave-local).** Solution build (grammar) · Characterization 33/33 · Conformance
`Corpus|VersionMatrix|Collating|Alphabet|Configuration|Nist|Special|Environment|Reserved|Locale|Ord|Debug`
**3412/3412** · Unit `Grammar|Parser|Registry|Manifest|Guard|Vcr|Construct|Diagnostic|SpecTraceabilityInventory|
ReservedWord|Catalog` **110/110** · CLI probes. Docs: DESIGN-frontend-grammar.md §3.3e, CONFORMANCE.md (§4 item 5,
A.1 item 127), DIAGNOSTICS.md + VERSION_CHANGE_REFERENCE.md regenerated, constructs.json, kb/Work PB78 → landed,
PB92 registered, plan §0. Battery owed for the PB78 batch with the next landing (battery #11, tree `ed88ef94`,
was ALL GREEN: Conformance 4698/4698 · Unit 4221/4221 · Characterization 33/33 · NIST 353/0 audit-clean ·
differential 0 flips).

