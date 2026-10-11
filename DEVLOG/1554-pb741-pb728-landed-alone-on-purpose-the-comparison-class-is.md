## Entry 1554 — 2026-09-06 06:15 PDT — PB741 + PB728, LANDED ALONE ON PURPOSE: the comparison class is a property of the PAIR, and it is written down once

**This cluster was landed BY ITSELF, ahead of every other train, because battery #58 found a WRONG ANSWER on
main.** The landing-train economics say five clusters per landing; a blocking fix is the standing exception
(`workstream` §3), and this is one: NIST NC215A's `SEQ-TEST-GF-6`/`-7` (`IF NINE-DU-9V0-1 < SPACE` under
`PROGRAM COLLATING SEQUENCE IS THE-WILD-ONE`) went `PASS` → `FAIL*` in train 14, so every later train would have
been landing on a main that answers a relation condition incorrectly. Trains 17 and 18 now rebase onto a correct
main instead.

**The mechanism: one classifier answering two questions.** `f798397f` folded the relation renderer's collating
argument onto `CollatingSelection.Of`. That classifier is correct — for the question it was written for: *which of
§14.9.40.4 GR5's two separately-determined sequences does this SORT KEY take*, where a numeric key takes neither.
A relation asks a different question, and asks it of BOTH operands: §8.8.4.2.5 makes a numeric integer operand
compared with an alphanumeric one an ALPHANUMERIC comparison, which §8.8.4.2.7 collates under the alphanumeric
program collating sequence (§12.3.6.4 GR9 installs THE-WILD-ONE; §12.3.7.4 GR7 k)3 puts every unspecified
character above the specified ones). The emitted call carried **no collation argument at all**, while the literal
twin `IF NINE-DU-9V0-1 < " "` kept it — the internal inconsistency that names the defect
(`feedback_a_real_clause_can_answer_a_different_question`, and the renderer's own comment three lines above had
already decided the comparison was alphanumeric before it asked the anchor for its category).

**The fix is the split, not a special case.** `CollatingSelection.ForComparison(left, right)` is new and is the
§8.8.4.2 selection written down once, each arm carrying its `cite.py --check`ed clause — boolean if either operand
is boolean (§8.8.4.2.8) → national if either is national (§8.8.4.2.9) → numeric ONLY if both are numeric
(§8.8.4.2.4) → alphanumeric otherwise (§8.8.4.2.7, carrying §8.8.4.2.1's normalizations). `EmitContext.CollateArgFor`
now takes the PAIR and **the one-argument overload is deleted**, so a caller cannot supply half a comparison; that
is the compile-time form of "this cannot come back", and it is why the fix is not a guard at the call site. All
four relation surfaces go through it: the direct relation, the figurative relation, the level-88 membership test
(§8.8.4.5.3 GR2) and the EVALUATE THRU range (§14.7.8 rule 2). `CollatingSelection.Of` keeps its meaning and its
summary now names the SORT/MERGE-key question and forbids being asked a comparison question.

**The rule-4 sweep found three more arms, each measured before and after.** (1) `RenderFigurativeRelational` never
`deSign`ed its anchor, while the direct relation always has — §8.8.4.2.5's move is governed by §14.9.25.4 GR6a
("the operational sign is not moved"), so `IF S9 < SPACE` compared a sign-carrying image; the classic two-arm
dispatch with one arm fixed (`feedback_two_arm_dispatch`), and pre-PB678. (2) The level-88 site and (3) the
EVALUATE THRU site read `Pic`, which is NULL for every group, so a `GROUP-USAGE NATIONAL` conditional variable was
weighed by the ALPHANUMERIC 256-entry table over a national image and a BIT group lost its boolean-zero pad.
**That is `PB728`, filed on main hours earlier — it lands here**, with the golden it asked for
(`pb728_level88_group_usage_collating`: `ALPHABET REV-AN IS "CBA"` against `ALPHABET REV-NAT FOR NATIONAL IS
N"ABC"`, deliberately opposite so no leg can pass for the wrong reason, plus an alphanumeric-group over-fix guard)
and the preventive it asked for, as a drift test rather than a semgrep rule. PB575's binder half is untouched and
stays open, as PB728 itself directed.

**The coverage hole is closed, and the citation sweep came with it.**
`pb297_figurative_sizing_categories` — the `test-ref` on `GR-8.8.4.2.7-1`'s CONFORMS verdict — held `PIC X`,
`PIC N` and `PIC 1` items and **no numeric one**, so the arm PB678 changed had no witness but NC215A, which the
wave-local gate does not run. It gains U11–U16 (the numeric anchor: LOW-VALUE, an `ALL` literal, the
alphanumeric-literal twin that must agree, a signed anchor and a four-digit anchor), every value derived in the
program's own header and **proven red** by rebuilding with PB678's semantics restored — U11/U12/U14/U16 flip while
the literal twins U13/U15 do not, exactly the inconsistency PB741 named. `CollatingComparisonClassDriftTests`
(6 facts) pins the whole category matrix as literal data and the exact pair where the two classifiers *must*
disagree. Separately, `--check` caught **seven** inherited citations of "§8.8.4.2.3 SR2 makes a group operand class
alphanumeric": SR2 is the identifier-CLASS syntax rule and says nothing about groups — the real clause is
§8.8.4.2.1 — corrected in six code sites and one design doc.

**What the lander found that the implementer's report did not.** That citation sweep was scoped to `src/` and
`docs/` and left **two more sites in the test tree** carrying the identical wrong claim:
`tests/Cobol.Net.Tests.Unit/BoundaryImageChannelTests.cs:60` and `tests/conformance/2023/da5_table_sort_group_key.cob:13`.
Both are corrected here (comment-only; the `.cob` swap preserves its columns). A sweep that stops at a directory
boundary is how an inherited citation survives its own correction — every remaining `8.8.4.2.3 SR2` in the tree is
now either the deliberate negative statement ("⛔ NOT §8.8.4.2.3 SR2, which this used to cite") or SR2 used for what
it actually says, plus the dated 2026-08-09 `pb66` design DRAFT, which is history and stays. Nothing else in the
report was false: the attribution was accurate end to end, including the exact commit, the exact arm and the
minimal discriminator, and the two additions it reported (the missing `deSign`, and taking the note's proposed
signature literally by deleting the one-argument overload) both held.

**The landing.** One cluster, one gate, in a fresh worktree cut from `3d75a747`. The row's base `0a24276f`
predates train 16, and the plain patch applied clean — main had touched none of `ConditionRenderer.cs`,
`CollatingModel.cs` or `EmitCore.cs`. The inventory hunk was **excluded from the patch on purpose** and
`GR-8.8.4.2.7-1` re-verdicted from `batch-gr-88427-1.json` on the post-train-16 4,348-row tree instead — the
deterministic mechanism, since the implementer computed its hunk against a 4,371-row denominator. Verdict unchanged
at CONFORMS, **EVIDENCE** refreshed (`code-location` gains `CollatingModel.cs#ForComparison`, `test-ref` gains the
unit drift test, `notes` record why the prior evidence never observed a numeric operand); **GAP 2788 → 2788 of
4348**, `build_inventory.py --check` clean. Filter
`DisplayName~NC215A|~CorpusRunner|~Collating|~Figurative|~SortMerge|~Condition88|~ClassCondition|~Relational|~AbbreviatedCondition|~Evaluate|~SpecPinnedNist`,
plus the FULL Unit assembly and the legacy `CobolSharp.Tests.Integration` assembly — the two suites train 13's and
battery #57's reds proved a filtered gate cannot see. **`NistDifferentialTests_P0("NC215A")` is the acceptance
criterion and it passes (`Passed … NistProgram_MatchesGolden(testName: "NC215A")`, 4/4 under `DisplayName~NC215A`);
the filtered Conformance leg is 1884/1884, the FULL Unit assembly 22570/22572 with the two known
`ExternalCorpusPopulationDriftTests` reds (a fresh worktree has no GPL corpus) and nothing else, Characterization
33/33, and the legacy Integration assembly 503 passed / 1 skipped.** Both citation audits at 0, `semgrep/verify.py` PASS with `raw-diagnostic-code-literal`
flat at 427, `work.py check` clean. COBOLNET1894–1895 were reserved and are UNUSED — a wrong-answer fix adds no
refusal. Battery #59, at the next train's head, must be ALL GREEN with 0 flips.

