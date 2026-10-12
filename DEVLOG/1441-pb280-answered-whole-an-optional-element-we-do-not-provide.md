## Entry 1441 — 2026-09-02 22:55 PDT — PB280 answered whole: an OPTIONAL element we do not provide is DOCUMENTED-NON-SUPPORT, and the anchor exemption had been keyed on the wrong thing since the moment that became true

The owner answered all three PB280 questions today. Two of them ratify what already stands; the first one
changes a verdict, and — the part no question asked about — it moved a rule that had been correct for exactly
as long as `DOCUMENTED-NON-SUPPORT` meant one thing on an Annex A.1 row.

**Q1 — a "Not provided." determination on an A.1-OPTIONAL item closes as DOCUMENTED-NON-SUPPORT.** The ground
is A.1's own preamble, which conditions the documentation duty on provision: *"Documentation required: If the
element is provided by the implementor, the implementor's user documentation shall document the element or
shall reference other documentation that fulfills this requirement."* (`cite.py --check A.1` → OK), with §4.2.7
as the second ground the verdict's own `meaning` already named (`--check` → OK).

**Q2 — no, an anchor-only DOC row stays a GAP.** `DESIGN-spec-conformance-review.md` §1(a) is not widened. The
reasoning is the measurement the note already carried: greenfield source cites 13 of 222 A.1 items, so for the
other 209 "there is nothing in the compiler to observe" would be contradicted by nothing and confirmed by
nothing. Three places had described this as an open question; they now describe it as a decision.

**Q3 — `docs/CONFORMANCE.md` IS the §4.2.16 user documentation, and a row may reference the governing document
but shall also STATE THE VALUE.** Deliberately narrower than the standard, which permits discharge by reference
outright: a by-reference row states no value, so no test can pin it, and the gate can only check that a row
exists — Q2's unfalsifiable shape one layer along. It costs. Several remaining obligations are answered entirely
by the host (item 20's case mapping, item 12's bits per byte, item 144's unseeded seed) and each now owes a
stated value and a witness. That cost is what makes them closeable at all.

### Q1 landed as a predicate, and needed two arm fields that did not exist

Thirty items eventually, two today, so it is a `derived-verdicts` selector rather than adjudications —
`a1-optional-not-provided`. Its arm is an AND over two things somebody else wrote down: the **requirement class
the standard states** (`requirement`, parsed out of A.1 by `extract_rule_catalog.py`: 164 required · 30 optional
· 27 conditionally required · 1 unclassified) and **what §7 says** (`determination-prefix`, read off the register
row keyed by the rule's own id — which is exactly the anchor `kinds.DOC.anchor-template` computes). No `pattern`
over the rule TEXT at all, on purpose: every A.1 item's text is the same boilerplate, so a text arm here would
select by requirement class in regex clothing while looking like a judgement.

`determination-prefix` is the first arm field to read an artifact outside the catalog, and it has to be — a DOC
row's implementation IS its determination. So the §7 table now has ONE parser per language:
`inventory_schema.section7_rows` (moved down out of `audit_annex_a1.py`, which had written it and was no longer
its only reader) and `tests/_shared/ConformanceRegister.cs`. Three readers keying on one markdown table
independently is `feedback_one_rule_one_place` verbatim. The match is on the cell's OPENING words, emphasis
stripped, never `contains`: item 127's own determination discusses what is not provided three sentences in.

**Measured: 2 rows of 4,311, both blank** — held out of the 2026-09-02 back-fill for exactly this open question
— so no adjudication was overwritten, and **GAP unmoved** (3227 → 3227 when measured in the worktree; **3223 →
3223** on the merged tree, cluster D having closed four rows underneath it — the batch was re-validated after the
rebase, not assumed). A declined row closes on a witness proving the
documented posture is what actually happens, never on its verdict. That debt is `kb/Work/PB373`, which says what
each golden must assert: for item 127 an INVARIANCE over three source shapes (named computer, paragraph with no
name, no paragraph) producing byte-identical output, not "it compiles"; for item 206 BOTH edges of all eight
BINARY-CHAR-family bands, because an implementation that DID provide the optional wider range passes the
endpoint leg identically and only differs one step beyond it.

### The correction nobody asked for: `anchor-exempt-verdicts` was keyed on the verdict

`DOCUMENTED-NON-SUPPORT` now has TWO grounds on a DOC row and they differ in exactly one observable. An item an
A.4 module WITHDREW has no §7 row — A.1's preamble makes it "not required if the optional or processor-dependent
feature is not implemented" — and owes no anchor. An OPTIONAL element recorded as not provided HAS a §7 row,
because stating the non-provision IS the determination, and owes its anchor like any CONFORMS row.

Keyed on the verdict, both answer the same, and the second answer is wrong in three places at once: items 127
and 206 would have been excused their own determination; `check_pins` would have stopped holding their witness in
agreement with §7's `Pinned by` — silently, at the moment the witness landed, which is the worst possible time;
and `audit_annex_a1.py`'s REMAINING counter would have printed "⊘ CANNOT ARISE" about an element this register
documents in full, dropping by two for no work done. One item, two registers, two answers. So the exemption is
now keyed on **"does §7 carry a row for it"** — one predicate per language (`Schema.anchor_obliged` /
`AnchorObliged`), one parser per language, and `unreachable_items()` applies the same test.

### The two new axes cannot be falsified by today's document, so they are falsified elsewhere

All 30 A.1-optional items and all 47 §7 rows agree: the only "Not provided." determinations are on optional
items, and the only optional items with a determination say "Not provided.". A predicate that had dropped
`requirement` altogether, or matched ANY determination, selects the same two rows and leaves
`DerivedVerdictDriftTests` green. That is `feedback_green_gates_arent_evidence` waiting to happen, and the axis
that most needs the guard is `requirement`: a REQUIRED item whose §7 row said "Not provided." is a conformance
DEFECT, and stamping it DOCUMENTED-NON-SUPPORT would document non-conformance as if the owner had licensed it.

`derive_verdict_batch.py --self-test` drives each axis against a fabricated catalog and a fabricated register,
one broken thing at a time (11 cases), and `TheSelectorEngine_ProvesEveryAxisCanFail` shells it every build,
asserting the CASE NAMES rather than the exit code, because a shrinking self-test still exits 0. The interpreter
probe moved to `tests/_shared/PythonInstrument.cs` rather than being copied out of `AnnexA1RegisterDriftTests`.

⭐ **And the drift test's failure branch was fired, not assumed.** A §7 determination reading "Not provided."
planted for A.1 item 7 (optional, no row today) turned the gate RED with one attributable message —
`DOC-A.1-7: (blank)` — plus the sharpness fact's own guard. Unplanted; `docs/CONFORMANCE.md` byte-identical.

### Also

`docs/CONFORMANCE.md` §7's preamble had a hand-enumerated list of discharged items that had rotted twice (stale
by three when item 50 landed, and by three again by today: it claimed 38 of 195 where the script says 41, and
157 remaining where the script says 154). The list is gone; `audit_annex_a1.py --json`'s `filed` is the answer,
the rows below are the register, and a list in the preamble could only ever have been a second answer to a
question one artifact already owns.

**Gate (worktree):** `dotnet build CobolSharp.sln -c Debug` — Build succeeded, 0 Warning(s), 0 Error(s).
`dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter "FullyQualifiedName~DerivedVerdict|
FullyQualifiedName~SpecTraceabilityInventory|FullyQualifiedName~AnnexA1Register|
FullyQualifiedName~DefectiveRowCoverage|FullyQualifiedName~TestRepoDrift"` — **Passed! - Failed: 0, Passed: 49,
Skipped: 0**. `audit_annex_a1.py --check` clean (45 filed · 195 in scope · 154 remaining · 4 withdrawn, all
unchanged by this landing); `--self-test` ALL GREEN, 17 cases including the new documented-decline arm;
`derive_verdict_batch.py --self-test` ALL GREEN, 11 cases; `work.py check` — 429 items, all well-formed.
