## Entry 1486 — 2026-09-05 04:07 PDT — Golden-lane round 4: 19 spec-derived goldens close 19 traceability rows (GAP 2883 → 2864), and the four rows left CONFORMS-but-untested are now the whole population — every one blocked on an owner decision, not on a test

Round 4 drew **23 rows** across three writer slugs (`misc-p1` 10, `misc-p2` 11, `misc-p3` 2) and dispositioned
**19 new-golden / 4 not-closable**, with no row closed on an existing golden: each candidate was checked and none
exercised its rule's own branch (nothing in the corpus spelled PICTURE as a clause, no RENAMES witness spelled
THROUGH, no SEARCH ALL golden sat under an OCCURS, no LINAGE golden wrote a zero margin, and no VALUE clause
anywhere in `tests/conformance` wrote THROUGH). The refuter overturned **7 of the 23** — three
`does-not-exercise-rule` (all in §13.18.55: the no-SIGN-clause leg that GR4 disapplies GR5/GR6 to, so GR7's
consequent had no referent; the ONOCCURS tables whose occurrences already sat on even offsets, where an aligning
implementation would have answered the same 6; and the REC leg whose claim on GR10's second sentence was
withdrawn because §13.18.55.4 GR6 holds the size at 3 under either election), one `expected-value`, one
`citation`, two `convention` — and the fixer applied every one of them — 19 recorded actions across the three slugs — with **zero refusals**. **24 draft files
landed** (20 positive across `2023`/`2002`/`85`, 4 negative), all registered and all confirmed RUNNING by name.

**GAP 2883 → 2864**, +19 closed of 4311; 19 records accepted of 19 submitted.

**THE ONE DRAFT-ERROR WAS A RESERVED WORD, AND IT WAS A REAL COMPILER VERDICT.**
`2023/l1_picture_special_insertion.cob` declared `01 EC PIC ***.**.` and was refused COBOLNET0901. The validator
proved the compiler right rather than the fixture, and the landing re-derived it independently: §8.3.2.4.1
— "Reserved words shall not be used as system-names or user-defined words." (`cite.py --check` OK) — and §8.9
carries `EC` as its own list entry (`awk` over the §8.9 region: one standalone `- EC`, between DYNAMIC and
EDITING; `ReservedWords.Table.cs` has it as a 2002 addition). The rule under test was never misread — a renamed
probe printed all six expected lines including the `***.**` rule-7b case — so the repair is the name: EC → EZ at
its four sites, the header table, the `MOVE 0 TO` paragraph and the `.out` label, plus a NAMING paragraph in the
header so nobody "corrects" it back to the mask order.

**The sweep the validator asked for was run mechanically, not by eye.** `sweep_reserved.py` reads the compiler's
own `ReservedWords.Table.cs` — the same §8.9 funnel COBOLNET0901 consults — extracts every user-defined word
from all 24 drafts (level entries, PROGRAM-ID, paragraph names, FD/SD, SELECT, INDEXED BY) and checks each
against the table AT THE EDITION THAT DRAFT RUNS. It fired exactly once, on `EC`, and is clean after the repair.
No structural guard was added, and deliberately: the corpus runner compiles every enabled fixture at its own
edition, so a reserved-word data-name is already caught by the gate — one mechanism, not two.

**THREE HEADER-PROSE DEFECTS WERE REPAIRED BEFORE LANDING, BECAUSE A GOLDEN'S HEADER IS A CITATION.**
(1) `negative/l1-picture-currency-not-equivalent.cob`'s EDITIONS paragraph said that at `--std 85` the source
"fails on the clause rather than on SR28". Measured, it fails on BOTH — COBOLNET0808 (SR28) first, then
COBOLNET0893 (the CURRENCY SIGN WITH PICTURE SYMBOL 2002 gate) — so the paragraph now states the measurement and
says 85 is left out of `reject-at` because its rejection does not DISCRIMINATE SR28, not because SR28 stops
applying there; the header's "SR28 IS THE ONLY GROUND" claim is scoped to the `reject-at` editions so the two
paragraphs cannot contradict. (2) The same header filed §13.18.40.4 rule 13 a) as **SR**13(a); §13.18.40.4 is
General rules (`cite.py --check 13.18.40.4 "at least two identical symbols from the set"` → OK, rule 13), so it
is GR13 a). That one was found by a **new mechanical sweep** (`sweep_srgr.py`) that reads each fixture header's
`§X.Y.Z SR<n>` / `GR<n> of §X.Y.Z` citations and compares the label against the clause's own heading kind in
`specs/ISO_COBOL.md`; run over ALL 250 `l1_*` fixtures it reports **358 labelled citations, 0 suspect** after the
repair — and it was proved to FAIL first, by running it against the pre-repair draft, where it names the one
line. (3) The `misc-p3` writer's report cited `CobolData.g4:617/619` for the `(VALUE | VALUES) (IS | ARE)?` token
position; the real lines are **620 and 621** (verified). That one touched NO landing artifact — no fixture
header and no record note carries it — so nothing in the tree needed changing, but it is the inherited-citation
shape rule 1 warns about, applied to a code site rather than a clause.

**THE GATE FOUND A FOURTH ONE THAT NO HUMAN READ WOULD HAVE.** `audit_code_citations.py --check` went RED on
`l1_picture_special_insertion.cob:1` with a SUBJECT finding: the header cites §13.18.40.5, whose catalog subject
is **PICTURE clause**, and the whole 50-line header spells `PIC` and never once `PICTURE` — so the audit could
not see the construct the citation is about and offered ALIGNED / FULL / INTEGER instead. Both its sibling
PICTURE fixtures write "§13.18.40.3 SR5 PICTURE clause —", this one wrote "editing rule 4 (special insertion
editing)". Naming the clause in line 1 clears it; the audit's baseline is back to **0 findings**, as is
`audit_doc_citations.py` (0 MISFILED).

**THE FOUR NOT-CLOSABLE ROWS — AND THE FACT WORTH THE ENTRY.** After this landing exactly **four** inventory
rows carry verdict `CONFORMS` with an empty `test-ref` and no `derivation`, and they are precisely the four this
round refused to close. The CONFORMS-but-untested population is no longer a mixed bag of missing tests; it is
four rows, each blocked on a decision only the owner can make:

- `GR-14.9.20.4-9` (Annex A.2 item 20, INITIALIZE) and `GR-14.9.28.4-2` (A.2 item 37, overlapping PERFORM) sit
  on PB386's mechanical `undefined-A.2` arm — `extract_annex_a2.py` resolves the standard's own citation in each
  A.2 item to exactly these rule-ids. The standard declares the subject undefined, so there is no expected value
  to derive and any golden would pin an implementation choice; their only closure path is an owner-signed
  `docs/CONFORMANCE.md` §8 derivation row, as `PB205`'s row already waits. The two DRAFT rows — signature cell
  deliberately blank — are held OUT of the tree in the round's scratchpad
  (`out/misc-p1/_proposed-conformance-section8-rows.md`); the orchestrator carries them to the owner.
- `GR-13.18.52.4-4` and `GR-13.18.52.4-5` need `DOC-A.1-177` (GR4's sign position AND representation) and
  `DOC-A.1-178` (GR5 b)'s valid-sign set). `docs/CONFORMANCE.md` carries neither — its A.1 table jumps
  171 → 179 and a grep for both keys returns 0, re-measured at the landing. A golden written now would pin an
  UNDOCUMENTED implementor choice (the over-punch character, the NUMERIC valid-sign set) as though it were
  documented, so none was written and **no A.1 row was added**: documenting a determination is an owner-visible
  decision, not a landing step (`PB160`). What each golden must assert once the rows land is recorded in the
  round's scratchpad at `out/misc-p2/CHECKPOINT.md`.

That constraint is what shaped the nine §13.18.52/§13.18.55 goldens that DID land: every SIGN expectation is
written in the SEPARATE CHARACTER form, whose position and representation are both fully spec-defined by
§13.18.52.4 GR6 a/b, so no shipped golden leans on the missing determinations; where a rule reaches an
implementor-defined representation the golden asserts an EQUALITY between two images and never prints either
character. And the round STRUCK a false coverage claim the b3 caveat had left standing: GR5 a) of §13.18.52.4 is
NOT pinned by the SEPARATE legs of the new goldens — GR5's antecedent is "If the SEPARATE CHARACTER phrase is
NOT specified" and GR6's is "If … IS specified", mutually exclusive by their own text, so those legs pin GR6 a)
and **GR5 a) is pinned by nothing shipped or drafted**.

`SR-13.16.3-20`'s round-3 caveat is discharged: rather than re-citing the level-05 fixture the refuter had
cleared, the row got a NEW two-case witness — `negative/l1-present-when-level-01` and
`negative/l1-present-when-level-77` — each writing the rule's own forbidden construct at the level number SR20
names, refused by name with COBOLNET1708 (the A.4.14 VALIDATE-facility decline) so both go red the day the
module is claimed. Three other rows are closed by pairs rather than singles: `SR-13.18.40.3-28` by the 2023 and
2002 positives PLUS the `$$W9` negative, `SR-13.18.55.3-2` by the abbreviation positive PLUS the negative
proving `SYNC` meets the same 2023 group gate `SYNCHRONIZED` does, and `SR-13.18.63.3-17` / `-28` each by a
default-edition witness and an edition-floor twin.

**GATE** (this worktree, `pwsh scripts/build-local.ps1 -Filter "~Corpus|~Negative|~Intrinsic|~VersionMatrix"`):
citations 0 findings / 0 MISFILED; build 0 warnings 0 errors; Conformance
`Passed! - Failed: 0, Passed: 3840, Skipped: 0, Total: 3840`; Unit
`Failed! - Failed: 2, Passed: 5411, Skipped: 0, Total: 5413` — the two known
`ExternalCorpusPopulationDriftTests` reds a fresh worktree carries because the GPL corpus is not checked out
(never added); Characterization `Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33`;
`GrammarDiagramGeneratorDriftTests.Generator_RunsClean` (PB588) green. All 24 new fixtures were then listed BY
NAME from the runner (`DisplayName~` filter over all 24): 24 discovered, 24 passed, at the editions their
manifests name — a golden that is registered but not discovered would be a red by absence, so the population
was asserted rather than assumed. `work.py check`: 680 items, all well-formed.

`kb/Work/PB370` — the note that holds this population — records the 19 closures and the four blocked rows in
its own section; **no new note was opened**, because the round surfaced no compiler defect: every draft that ran
ran green on the landing worktree's own build, and the single failure was the fixture's, not the compiler's.

