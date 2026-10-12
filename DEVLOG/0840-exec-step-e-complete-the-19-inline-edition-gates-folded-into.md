## Entry 840 — 2026-07-12 17:54 PDT — EXEC STEP E COMPLETE — the ~19 inline edition gates FOLDED into the VersionConformancePass; GateId deleted; the "edition-agnostic" claims reconciled

**The fold (plan §4.1 task #13).** Every hardcoded-year inline diagnostic gate left in the binder is now a
`constructs.json` row firing from the pass — 20 new rows (call-returning-2002 0884 · goback-bare-2002 0880 ·
inspect-backward-2023 0845 · the four INITIALIZE phrase rows 0830–0833 · release-from-literal-2002 0871 ·
sort-collating-national-2002 0872 [PENDING — the in-window national collating path is a staged BoundUnsupported]
· table-sort-2002 0870 · raise/resume-statement-2002 0876 · set-last-exception-2002 + statement-raising-2002 0879
· use-after-exception-{object,condition}-2002 0876/0877 [F4 PENDING — OO fixture] · accept-four-digit-year-2002
0815 · program-id-recursive-2002 0885 · options-paragraph-2014 0804 · arithmetic-standard-2014 0807, a
DUAL-window row: 0900 below 2014, 0807-REMOVED at 2023+) — each with a runnable matrix fixture (negative witness
+ INV-1 continuity), which is the audit's real payoff: these gates NEVER entered the version matrix before.
Twelve new ParseArm `Visit*` overrides + four extensions of existing ones (`VisitUseStatement` F3/F4,
`VisitCallStatement` RETURNING, `VisitGobackStatement` bare — preserving the GobackReturning-0900-subsumes-0880
contract AND the method-GOBACK exclusion via `InMethodDefinition`, `VisitAcceptStatement` four-digit-year); the
F2 table-SORT is recognized SYNTACTICALLY (no USING/GIVING/procedure legs — §14.9.40.2 F1 requires an input AND
an output leg), so recognition never needs the resolved name class (the DEVLOG-724 drop-proofing).

**What deliberately STAYS binder-side — now a written ledger.** The new DESIGN-version-conformance-pipeline
§1.1 "gating-exception ledger" is the ONE canonical statement (every other doc now points at it): the UDF
bind-time Check; the catalog-driven per-NAME windows (D8 intrinsic windows + TRIM-arg2, ExceptionCatalog EC
names, PictureAnalyzer symbol rows, the §8.3.1.2 digit caps) whose version facts live in their OWN tables; the
two sanctioned BEHAVIORAL edition reads (keyword-omitted FUNCTION routing <2002, MOVE CORR pair-selection
≥2002); and the owner-disposition SYNCHRONIZED-on-group site (error-strict/WARN-permissive — `Check`'s
introduction verdict errors on both axes, so folding it would flip the owner-chosen accept-inert continuity).

**GateId deleted.** The generated `Gating/GateId.cs` enum (58 members + the `GateIds.ConstructId` map) had ZERO
production consumers — its P2.7 forward-stamping consumer was abandoned (DEVLOG 679) and only its own drift-test
fact touched it. Deleted: the file, gen-constructs.ps1 section 3, and the drift fact; the pass's unrelated
`UsageGateId` helper renamed `UsageConstructId` to kill the name collision the plan's wording invited.

**Message standardization (the one observable text change).** Folded gates now emit the registry band text
("{display} requires COBOL-{N} (targeting COBOL-{Y}) — {where} ({citation})") with their historical codes
PINNED on the rows — codes byte-stable, texts uniform. One test pinned the old bespoke wording
(SortMergeDifferentialTests "requires --std 2002" → the band's "requires COBOL-2002"); the negative corpus
asserts codes only. The arithmetic-standard fold also ALIGNS the removal posture: at 2023 --permissive the
construct now WARNS and compiles (the RemovedMatrix doctrine) where the inline gate errored on both axes.

**Docs.** ~40 "edition-agnostic" claims corrected across 13 docs + the pass's own header — the strong forms
("zero edition checks", "Never sees an EditionInfo", "ALL gating lives in the pass") were over-claims against
the ~19 inline gates and now either state the §1.1 ledger or point at it; the stale `.Syntax`-back-reference
claims (plan :254, PHASE-03 :53/:872) corrected to the as-built two-arm/no-.Syntax invariant; the §1 pipeline
diagram's phantom "HALT if bind produced errors" line corrected to the as-built ONE post-pass gate (bind + pass
diagnostics accumulate — DEVLOG 724's both-true posture).

**Battery.** Conformance 3256 (the matrix grew ~90 cells from the new rows) — round 1 had exactly 2 failures
(the pending-row activation-contract convention + the pinned bespoke text), both fixed one-at-a-time; unit 281
(−1 = the deleted GateId drift fact); characterization 33 byte-exact; no shared-grammar change ⇒ no guard-fast
leg required (CI runs the full net).

