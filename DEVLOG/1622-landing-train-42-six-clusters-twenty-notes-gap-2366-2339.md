## Entry 1622 — 2026-09-20 13:35 PDT — LANDING TRAIN 42: six clusters, twenty notes, GAP 2366 → 2339 — five of the six were a rule written down in ONE place that two paths reach, and the sixth taught the landing loop to remember what it closed

**Cluster 1 — the SET finisher train 40 dropped (PB449 + PB456 + PB458 + PB462).** Both of the reds that dropped
it were one shape: a rule about an OBJECT reference reached through a path with no object in it. §14.9.39.2
writes a format's identity into its RECEIVING brace, so selection reads the whole receiving list — but Format 1's
brace is `{ index-name-1 | identifier-1 } …` and `identifier-1` admits ANY identifier, so a list of one `PIC 9(4)`
item selects Format 1 and the sender is never asked; `SET N4 TO U` then reached the ARITHMETIC screen and was
reported as COBOLNET0844 "'U' is not a numeric operand" (§8.8.1.1) — true, and silent about §14.9.39.3 SR8.
Format 1's own SENDING brace cannot hold that sender either (§8.8.1.1 admits only numeric operands; SR2 makes
identifier-2 class index), and an item of class object — or of category data-pointer, program-pointer,
function-pointer — is named by exactly ONE other format's sending brace, so the statement IS that format and its
receiving rule refuses the receiver BY NAME. That is a COLUMN in the selection table (`Row.SendsOnly`) rather than
an arm for class object, which is why the three pointer siblings were fixed with it and why a drift test can pin
that no kind Format 1 CAN send ever appears in it. The second red was the predefined object reference
EXCEPTION-OBJECT: spelled as an ordinary word, described by §8.4.3.6.3 SR2 and declared by no data description
entry, it resolved through the general path to "'EXCEPTION-OBJECT' is not defined" — false about a name the
standard declares. Classified before the general lookup through the one method that now compares that spelling,
it selects Format 5 and draws §8.4.3.6.3 SR1. Fixing it exposed a third arm of the same kind: the Format-5
receiver screen was a single Resolve-or-category test doing TWO rules at once, stacking SR8 on the resolver's own
diagnostic and calling a declared index-name undefined; three arms now, one rule each. Ten rows re-verdicted.

**Cluster 2 — three general formats the printed standard carries and the grammar had never written down (PB511 +
PB544 + PB553 + PB549).** Each was a different KIND of omission. The EXTERNAL clause's `AS literal-1` was a
TWO-ARM omission: §13.18.22.2 prints `IS EXTERNAL [ AS literal-1 ]` and both the data-description Format 1 and the
file-description Formats 1/2/3 print that slot, but the FD arm was spelt `IS? (GLOBAL | EXTERNAL)` — one grammar
rule over two clauses that do not have the same shape, which is exactly what hid the missing phrase for as long as
it was missing. Both arms now take the SHARED `externalizedNamePhrase`, so PB303's single COBOL-2002 gate and
single literal screen reached them by being written rather than copied, and §13.18.22.4 GR5's two sentences became
one expression at the one cell-keying site. Its SR2 came with it: "the externalized name … shall not be the same
as the externalized name of any other entry" was UNVIOLABLE while every externalized name was a data-name, and the
moment literal-1 could be written, two records could claim one run-unit cell and silently alias each other's
storage. The program-address-identifier's omission was the opposite shape — the semantics were complete and only
the ISO SURFACE was missing, so the Micro Focus / IBM `SET pp TO ENTRY` spelling was the only route into a
program-pointer while two comments in the tree described that spelling AS ISO §14.9.39 Format 9 with the §8.4.3.13
sender. It is neither, and the words "TO ENTRY" appear in no ISO general format; both comments now say so, and the
standard spelling parses, binds through the same body, and screens its three braced operand arms by name. The
third was §13.18.60.3 SR21, whose subject is the SET of usage clauses in an entry — which is why it could not be
written at all while the binder kept a single `usageText` that each successive clause overwrote, and why
`01 M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).` compiled with no diagnostic whatsoever, not even the non-support
decline the usage otherwise draws. A declined usage still has syntax rules over it. ⭐ Two of PB544's three rows
NO LONGER REPRODUCED on today's tree and were re-verdicted on measurement rather than re-implemented (PB487 put
MESSAGE_TAG in `usageKeyword`; PB452 + PB817 closed the elementary FUNCTION-POINTER arm) — a contract finding that
did not hold, recorded as such.

**Cluster 3 — BLANK WHEN ZERO is a rule about the value BEING STORED, and GR3 is a rule about comparisons (PB566 +
PB509).** §13.18.8.4 GR1's guard tested the raw sending pair forty lines before the mask rescale, so
`MOVE 0.4 TO <PIC ZZZ9 BLANK WHEN ZERO>` rendered `   0` while `COMPUTE` of the same value into the same item
blanked — the arithmetic emitters happen to hand the formatter a value already at the resultant's scale, so that
arm was right only BY ACCIDENT. §14.9.25.4 GR6 d) sends the store to §14.6.8 and §14.6.8.2 r4 transfers "with zero
fill or truncation on either end", so the stored value is the truncated one at BOTH ends; the guard moved into the
one shared formatter after the rescale — one test where there were two — and the same correction went to
`CobolLocaleEdit`'s r10/r15 b pair (r14's alignment hoisted above them, still ahead of any locale consult, so an
unavailable locale still blanks a stored zero) and to the two floating-point arms, which already tested the stored
value and now say which rule says so. GR3 — "when the object of an operation is a numeric or numeric-edited data
item … the value of the sending data item is considered to be zero" — was honoured only on the de-editing MOVE;
the relation condition took the first arm whose operand was a string and never asked. It now renders as a run-time
choice between the value comparison and the ordinary dispatch, at the ONE `BoundRelational` site, so EVALUATE,
SEARCH WHEN, PERFORM UNTIL and the abbreviated relations inherit it, with the literal-object and
reference-modified boundaries DERIVED from §8.8.4.2.5 and §8.4.3.3.4 GR6 c) and pinned in the golden. Neither
emitter was the fix; the shared formatter both reach was. COBOLNET2160–2163 were all returned unused.

**Cluster 4 — the landing loop learns to remember what it closed (PB245 + PB875).** A landing had no step that
recorded which traceability-inventory rows it CLOSED: `inventory_rows` is a note's claim while it is OPEN and it
empties as its rows are re-verdicted, so the link vanished at the moment of landing, and "is this row still real?"
could only be answered by measuring the compiler again. `closes_rows:` is the owner's 2026-09-19 decision
implemented whole — spelled snake_case because every multiword key in this frontmatter is, and because
`kb/Work.base` addresses a property as `note.<key>`. Its gate has two arms, BOTH proved RED on the LIVE register
before either was believed, and its back-fill is DERIVED from the inventory's own 258-commit history rather than
asserted: a row that moved from a non-resolving verdict to a resolving one is a row that landing closed, and 425
of the 2,033 observed closures are left UNATTRIBUTED rather than divided, because a train naming five notes and a
row naming none cannot be split without putting a row on a note that did not close it. The prerequisite was ONE
READER PER LANGUAGE: `kb/Work/PB205`'s wrapped `inventory_rows` was read as 4 of its 12 rows by Python and as NONE
by the gate that enforces claims, so both arms were fixed — a value the grammar cannot finish is now an ERROR CODE
with the key ABSENT, never silently empty — and a 12-shape fixture is evaluated by both, with the C# gate RUNNING
`work.py parity --json` rather than assuming agreement. Its own DEVLOG entry is 1621. This cluster claims no
inventory row and moved no GAP.

**Cluster 5 — the VALUE clause has ONE recipe (PB560 + PB556 + PB557 + PB558 + PB499).** §14.9.39.4 GR6 says the
value is "placed in the conditional variable according to the rules for the VALUE clause", but `SetEmitter.EmitSet`
carried a private three-arm copy of them, so `01 E PIC ZZ9.99. 88 E-TEN VALUE 10.` + `SET E-TEN TO TRUE` stored
`10    ` and `IF E-TEN` was then FALSE — a round trip §8.8.4.5.3 GR3 makes an identity. The arm was the STORE: the
TEST already called this recipe's pieces, which is exactly why SET-then-IF localised it. The private recipe is
deleted, `EmitSet` calls the one §13.18.63 recipe, and reading the receiver through `DataItem.OperandPic` instead
of `Pic!` made GR6's three group shapes its business — a whole-group-aliased `PIC 9(4) COMP-3` had not compiled at
all. The same inverted round trip is fixed for FLOAT-LONG, BLANK WHEN ZERO, insertion editing and a LOCALE
`$Z9.99`, the note's "unmeasured fifth arm". Beside it, two SETS the binder had never been able to express: SR33's
four format-3/5-only phrases, screened by ONE test over the set, and SR9's usages that admit no VALUE literal,
where `VALUE NULL` on an OBJECT REFERENCE and on a plain POINTER each failed the BACKEND with CS0029 — a compiler
crash, not an accepted program. ⭐ PB558 DID NOT REPRODUCE: a level-88 report entry is rejected today by
COBOLNET1746 since PB485 landed on 2026-09-05, so a screen was written, measured to double-report the same line,
and DELETED, leaving the derivation (SR33 ∧ §13.18.33.3 SR4 ⇒ SR30, exactly) as a comment. PB499 needed no code at
all — only the witnesses its three rows had none of.

**Cluster 6 — one grammar alternative per PRINTED general format (PB412 + PB421 + PB402).** `goToStatement` and
`moveReceivingPhrase` were each written as the UNION of their clause's printed general formats, and the COMPLEMENT
— every shape no format prints — was accepted and answered downstream: `GO TO A B.` bound its first target and
DISCARDED the rest in silence at every edition, while `GO TO DEPENDING ON X.` and `MOVE A CORRESPONDING G1 TO G2.`
compiled clean and killed the run unit on a LOUD STAGE that told the user a COBOL FEATURE was unimplemented —
about source the standard has no format for. Both rules now carry one alternative per printed format (PDF p.660
and p.756 RENDERED, since the diagrams are load-bearing), the two loud stages are deleted, and the resulting
syntax errors are re-coded as COBOLNET2172/2173 naming the format's own cardinalities. The GO TO format decision,
which lived in three readers' own words, is now `GoToFormats.Of` — total, because the grammar admits no fourth
shape. ⭐ PB402 proved HALF-discharged by train 39: nothing asserted the terse `RAISING LAST` spelling at
2002/2014 or the *named* 85 gate, and §14.9.38.2 Format 2's inline copy in `mcsSendStatement` had kept the word
required, so `SEND … RAISING LAST` was still a bare COBOL0001 — the one-rule-two-spellings shape again, found by
sweeping the grammar for the phrase instead of trusting the note. `COBOLNET_PIPELINE_DESIGN.md` D7 now says what
the wave learned: the "grammar parses the superset" licence is across EDITIONS, never across a clause's printed
general formats.

**The train.** Six clusters, twenty notes, ONE landing, base `b0172adc0`. Every cluster's patch EXCLUDED
`tests/version-matrix/traceability-inventory.json` and `docs/DIAGNOSTICS.md` at generation time rather than after
a conflict, because a conflict inside one JSON object is silently lossy and `DIAGNOSTICS.md` is generated: the
eight `record_verdicts` batches were re-applied to the merged tree in landing order (29 records, 29 rows changed,
CONFORMS 29, 27 of them closing the GAP) and the doc regenerated once, eleven rows for COBOLNET2112/2113,
2155–2159, 2167/2168, 2172/2173 — no range collided across clusters. `gen-constructs.ps1` re-run over the merged
`constructs.json` produced byte-identical `.g.cs`, which is the check that cluster 2's widened
`externalized-name-as-2002` row survived. NINE conflicts in all, eight of them a registry or list tail: the negative
and per-edition corpus manifests EIGHT times across four clusters (resolved as SETS and verified by ELEMENT COUNT — 2002 303, 2023
612 + 1 pending, 85 113, negative 1215, no duplicate) and `SetBinder.cs` once, where cluster 1's `BindSetTo` doc
comment and cluster 2's new `BindSetProgramAddress` block are pure insertions at one point and both were kept.
Marker checks ran on the working tree AND the index after every cluster, and every one was clean. ⛔ ONE
IMPLEMENTER CLAIM WAS FALSE and is repaired here: the PB560-group report states PB499 went `status: landed`, but
its note is `status: half` on the implementer's own branch — not a merge loss, verified against the branch blob —
while its body already carries a `## Landed 2026-09-20` section and its 2026-09-13 re-scope says in terms that the
flip belongs in that change set; all three of its rows are CONFORMS and state OK, so the frontmatter is flipped.
The train's eighteen newly-landed defect notes then had to gain `closes_rows`, which cluster 4's own gate demands
of a landed defect note, so `backfill_closes_rows.py --apply` was re-run on the merged tree — the first landing to
exercise that loop. GATE: the WHOLE `Cobol.Net.Tests.Conformance` assembly, UNFILTERED, plus unfiltered Unit,
Characterization and the legacy `CobolSharp.Tests.Integration` assembly — never a union of the clusters' filter
terms, which is the shape that drew a red first CI run on trains 39, 40 and 41. GAP 2366 → 2339.
GATE (verbatim, one build, four legs, NO filter on any leg): `Passed!  - Failed:     0, Passed:  7632, Skipped:     0, Total:  7632, Duration: 11 m 30 s - Cobol.Net.Tests.Conformance.dll (net10.0)` · `Passed!  - Failed:     0, Passed: 24291, Skipped:     0, Total: 24291, Duration: 1 m 56 s - Cobol.Net.Tests.Unit.dll (net10.0)` · `Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33, Duration: 2 s - Cobol.Net.Tests.Characterization.dll (net10.0)` · `Passed!  - Failed:     0, Passed:   503, Skipped:     1, Total:   504, Duration: 29 s - CobolSharp.Tests.Integration.dll (net10.0)` — `=== TRAIN 42 GATE: GREEN ===`. The GPL GnuCOBOL corpus was FETCHED into this fresh worktree first (36 autotest files), so both `ExternalCorpusPopulationDriftTests` MEASURED their population rather than being excused. No red on the way, no flake, and no filter term to be wrong about.
⚠ SEMGREP: `cobolnet-raw-diagnostic-code-literal` 415 → 416, attributed by diff to cluster 1's new
`ctx.Edition.Error("COBOLNET0867", …)` arm in `OoBinder.cs` — the thirteenth raw site for a code that has no
`DiagnosticCatalog` descriptor at all, matching twelve siblings in the same file. The baseline is re-recorded at
416 with the site named here rather than the cluster dropped; the descriptor for 0867 and its twelve call sites
are one sweep, and belong to a note of their own (the PB175 ratchet's population).

