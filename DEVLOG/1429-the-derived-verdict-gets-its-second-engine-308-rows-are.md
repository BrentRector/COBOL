## Entry 1429 — 2026-09-02 17:06 PDT — The derived verdict gets its second engine, 308 rows are stamped by predicate instead of by hand — and landing it onto the Annex A.1 lane exposed a rule neither lane could see alone: the anchor is owed by the verdicts that CLAIM a determination

`kb/Work/PB198` measured what recording a declined module by hand costs: sixteen rules on one question carrying
**four different verdicts** — NOT-IMPLEMENTED (9), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1). The
answer written then was that a module's scope belongs in `inventory-schema.json` under `derived-verdicts`, as
DATA, read by both engines. ⚠ **That claim was aspirational and this entry is where it becomes true**: no Python
consumer of `derived-verdicts` existed, so PB198's own batch was hand-made while the schema's `$comment`
asserted a second reader that was not there.

**The shape.** A selector is now `arms` — a disjunction of conjunctions; within an arm every field present must
hold, and a rule is selected when any arm holds and no entry-level `excludes-patterns` matches. The fields exist
because the standard scopes a module in more than one way and each way was, at some point, the one a text-only
predicate missed: `sections` (§8.8.1.4 is *titled* for the declined mode and its rules never repeat the phrase),
`pattern` (the facility stated from inside a mandatory clause), both as an AND-gate (a term that is
STATEMENT-LOCAL, or an OPERAND NAME — §12.3.7's `data-name-1` IS the CURSOR operand), `xref-sections` + `kinds`
(an A.1 obligation citing a declined clause), and per-arm `excludes-kinds` (a general format is evidence about a
CLAUSE, never one of its formats). `DerivedSelector` reads it in Python, driven by the new
`scripts/spec/derive_verdict_batch.py`; `DerivedVerdictDriftTests.Select` reads it in C# and holds the population
every build. Neither carries a selector of its own.

**Two properties live in the MATCHER, not in any selector**, because a convention every author must remember is a
convention one author will forget. Clause prefixes match **component-wise** — §13.18.30 is not inside §13.18.3,
and a raw `StartsWith` had the screen selector taking **543 rules instead of 156**, which would have flipped 32
CONFORMS rows to non-support. ⭐ And the text is **hyphen-normalised**: **29 catalog rules spell an operand name
with U+2011 NON-BREAKING HYPHEN**, so `\bidentifier-1\b` matched nothing there and silently UNDER-selected — the
direction a drift test stays green on.

**Landed.** 308 rows across six selectors — screen-handling-only 156, validate-only 75, commit-and-rollback-only
25, format-select-when-only 37, rewrite-write-file-only 12, oo-optional-items-only 3. Union 308, zero overlap;
297 stamped, 11 already at the verdict and kept as no-ops for the drift test. **The GAP moved by zero, which is
the correct outcome**: DOCUMENTED-NON-SUPPORT resolves, but §1(c) still demands a spec-derived test proving the
documented posture is what actually happens, and for a declined module that is the module's WITNESS.
**DOCUMENTED-NON-SUPPORT 36 → 333; GAP unchanged at 3618.** The batch was REGENERATED on the merged tree rather
than replayed, and the generator's refusal branch was re-driven here: a planted `CONFORMS` on `GR-9.2.3-1` made
it refuse the whole batch (`a derived selector must not overwrite an adjudicated verdict`), and the inventory was
restored byte-identical.

**⭐ THE FINDING THIS LANDING OWNS, because only landing the two lanes together could produce it.** DEVLOG 1428
made every kind-DOC inventory row owe the §7 anchor its own rule-id computes. That rule was written against a
tree where every DOC row WAS a determination. This lane then stamps four DOC rows — items **84**, **85**, **173**
(the FORMAT and SELECT WHEN clauses) and **86** (format validation) — `DOCUMENTED-NON-SUPPORT`, and the writer
refused the entire 308-record batch: *"kind DOC requires the register anchor 'docs/CONFORMANCE.md#DOC-A.1-84'"*.
Adding the anchor would have been the wrong repair — a claim of a §7 row that does not and should not exist.

The right one is that **the anchor is owed by the verdicts that CLAIM a determination, and by no others.**
CONFORMS, PARTIAL and DIVERGES each assert something about what §7 says. DOCUMENTED-NON-SUPPORT asserts the
opposite — the conditioning facility is not implemented — and **Annex A.1's own preamble then withdraws the
item**: *"When the element is part of a feature that is optional or processor-dependent, the item is not required
if the optional or processor-dependent feature is not implemented."* The DOC lane's own schema text already said
this in the `DOCUMENTED-NON-SUPPORT` meaning; nothing had connected it to the anchor rule. So
`kinds.DOC.anchor-exempt-verdicts` names those verdicts **as data**, read by one predicate per language
(`Schema.anchor_obliged` / `AnchorObliged`) — the writer, the C# drift gate and the register gate answer alike —
and `audit_annex_a1.py` derives the withdrawn ITEMS from the same `derived-verdicts` selectors
(`unreachable_items`, now also emitted as `--json`'s `unreachable`, which is what
`EveryVerdictedDocRow_HasItsRegisterDetermination` reads instead of a list). **The next declined module needs no
edit in any of the four places.**

**The exemption is not an escape, and that is asserted rather than argued.** Four cases were added to
`TheseChecks_ActuallyFail_OnAFabricatedInventory`: a declined DOC row with no anchor is NOT misanchored; the same
row still does not CLOSE without its module's witness; the same absent anchor on a CONFORMS row IS still caught;
and the declined row closes exactly when its witness arrives. Driven RED before being believed — with
`anchor-exempt-verdicts` emptied the new case fails `Assert.Empty() Failure: Collection was not empty`, and it
was restored. Both arms of the Python writer were driven by hand too: a fabricated CONFORMS DOC row with no
anchor is REFUSED, the same row as DOCUMENTED-NON-SUPPORT is ACCEPTED.

**Merge conflicts, all four resolved from evidence and both features kept whole.** `inventory_schema.py` (the
`kinds`/`_impl`/`anchored_files` block AND the `derived` selector map, both inside `__init__`);
`audit_annex_a1.py` (the item-key regexes, the escape-aware `cells_of` and the 3-tuple `section7_rows` AND the
new `unreachable_items`); `DESIGN-spec-conformance-review.md` (the new §8.1 in front of a paragraph whose
"nine facts" had become "eleven"); `inventory-schema.json` (a whitespace collision around the `kinds` object).

**§5 corrections, and one the implementer recorded but did not make.** A.4.2's posture is measured non-uniform
(COBOLNET1560 on the SECTION header only; ACCEPT/DISPLAY screen formats silently reinterpreted as device formats;
CURSOR / CRT STATUS and EC-SCREEN draw no diagnostic); A.4.3 splits (the statements are named COBOLNET1579 — the
note's premise that they were a generic parse error was stale — while the APPLY COMMIT clause owning 17 of 25
rows has no grammar surface at any edition); A.4.13's "No surface — parse error" was REPLACED as false (WRITE
FILE compiles and runs; REWRITE FILE compiles and throws at run time with an empty name); A.4.10 becomes
**Partial** because item 2, INTERFACE-ID multiple inheritance, IS supported and the row said "Not claimed" of all
three. ⭐ **That last one exposed a conformance claim contradicting another in the same document**: §2 row 45,
"Parametric-polymorphism method resolution … **Claimed** … single-dispatch OO method resolution over the class
table", while A.4.10 item 3 says parametric polymorphism is NOT claimed (overloading is refused COBOLNET0822 and
the resolution signature is the degenerate one, the method NAME alone). Annex A.3 item 45 applies only *"When
parametric polymorphism is provided in object orientation"*. **Row 45 is now N/A**, the disposition §2's own
header defines for a property of an unsupported facility. PB285 had filed it as "a separate correction, not made
here"; a conformance claim contradicting a conformance claim is not a thing to schedule (rule 6).

**Registered.** PB281–PB285 come with the lane. **PB291 is new**, allocated here after checking that no note
owned the catalog-side defect: the TRANSCRIPTION spells punctuation with typographic look-alikes — **107 U+2011
NON-BREAKING HYPHEN and 34 U+2212 MINUS SIGN in `specs/ISO_COBOL.md`**, reaching **29 and 14 catalog rules**
(79 rules over 47 clauses counting the mixed U+2013 en dash, of whose 65 occurrences 64 are not between digits).
§8.3.2.1 is explicit — a COBOL word's characters come from "the basic special characters hyphen and underscore" —
so `integer‑1` with U+2011 is not the word the standard defines. The selector fold is a CONSUMER fix and is
**not** the repair: the repair is the transcription plus a catalog rebuild, which moves the denominator and
therefore lands with PB284 (cross-referenced both ways), carrying a confusable-character drift check so a fold
never again makes a defect invisible instead of impossible.

**Gate (this worktree, at the landing tree).** Build succeeded, 0 Warning(s), 0 Error(s). Unit filtered
`~DerivedVerdict|~SpecTraceabilityInventory|~DefectiveRowCoverage|~AnnexA1Register`:
`Passed! - Failed: 0, Passed: 43` — 25 `DerivedVerdictDriftTests`, 12 `SpecTraceabilityInventoryDriftTests`,
3 `AnnexA1RegisterDriftTests`, 3 `DefectiveRowCoverageDriftTests`, all by name. Conformance `~Corpus`:
`Passed! - Failed: 0, Passed: 1012`. Full Unit: `Failed! - Failed: 2, Passed: 5177, Total: 5179` — the two
`ExternalCorpusPopulationDriftTests` (GPL corpus absent in a fresh worktree; environmental).
Characterization: `Passed! - Failed: 0, Passed: 33`. `audit_annex_a1.py --check` rc=0 FINDINGS none (39 of 222
items determined, **36 of the 195 in scope**, 159 remaining — the denominator dropped from 199 because four items
are withdrawn); `--self-test` ALL GREEN. `scripts/semgrep/verify.py` unchanged against the PB175 baseline
(36 / 475). `work.py check` ✓ 347.

