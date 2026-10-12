## Entry 619 — 2026-07-05 20:21 PDT — Phase 4 track (a) increment 1: NATIONAL + BOOLEAN data end-to-end (M2-DATA-3/4)

**Both 2002 data categories are LIVE on the string substrate, both goldens byte-exact, and two owner directives
landed mid-flight: a BLANKET grammar grant ("all required grammar changes without further owner approval" —
`feedback_grammar_approval` rewritten) and "use worktrees and maximum parallelism where beneficial" (the
worktree stale-base hazard re-verified CURED: fresh worktrees base at HEAD; the leftover `worktree-agent-a0cfb422`
branch — the likely DEVLOG-334 cause — deleted).**

**The cadence, parallel edition.** Recon workflow #1 (4 readers → xhigh synthesis) produced the decision-complete
M2-DATA-3/4 design (now in `PHASE4_RECONCILIATION.md`); recon workflow #2 ran CONCURRENTLY for increment 2 (the
newly-unblocked B-AND/B-OR/B-XOR/B-NOT leg — its design is spliced beside it, HARD-dependent on this increment);
a worktree test-author wrote the 4 new test batteries in parallel with the compiler work on main (it died on a
session limit mid-partition — files A–D harvested from its worktree, E/F/G finished in-session). Two
spec adjudications verified IN-SPEC before acting: Table 16 (:28847) marks MOVE national→alphanumeric invalid —
the golden's N2A leg was cut (negative case `move-national-to-an` enforces it); the .out re-baselined to
full-width DISPLAY (§14.9.11.4 GR6, the DEVLOG-597 posture; `LegacyDivergent` carries it).

**What landed** (the design's anchor list, all sites): PicInfo un-skeletoned (Analyze N/1 classification +
SR5/SR12/SR13/SR20 usage resolution; ParseUsage → the POINTER pattern; the IsUnimplementedSkeleton guard now
floats-only; `NationalUsagePending`/`BitUsagePending` markers for picture-less entries adjudicated at the
group-fixup pass); N"…"/B"…" literals bind at EVERY funnel (LiteralOperand + ComparisonOperand + EVALUATE +
INSPECT + the SUBSCRIPT-mode intrinsic walker) with the 0814 band (>8191, non-Latin-1); MOVE Table 16 legality
(0819 + SR7, `MoveCategoryLegality` beside the figurative gates); boolean relations equality-only via the ONE
`CheckedRelational` factory at all 5 BoundRelational sites (0844 — EVALUATE THRU ranges ride free, §14.9.13.3
SR4); comparisons category-dispatched (boolean zero-extend value compare / national ordinal, NEVER the
alphanumeric PCS — the `& 0xFF` weight-table alias is unreachable); CobolString gained pad params (Store/
SpliceInto/Compare — defaults byte-identical); VALUE/level-88 validation (0898 both directions + SR29 boolean
THROUGH; national THROUGH staged 0899); INITIALIZE Boolean/National categories (GR6c fills); D-N2 byte-surface
guards (ComputeTier reject, FD/SD record gate, SORT-key guard, the cell gate re-worded — display-form boolean
passes DELIBERATELY, byte=char under D-B1); registry rows LIVE + the new `national-edited-2002` pending row
(json + registry, drift-locked).

**Grammar (under the new grant): SUB_NATLIT/SUB_BOOLLIT** — in SUBSCRIPT mode `N"AB"` lexed as
SUB_IDENTIFIER('N') + SUB_STRINGLIT, silently misbinding `FUNCTION LENGTH(N"AB")`; the proper-token fix
(feedback_proper_fixes, literally the lexer-token case).

**Honest misses caught by the process.** (1) The stale-build trap AGAIN (the standing lesson): the CLI dir held
16:23 Frontend/Runtime dlls while probing 19:51 code — `--no-incremental` after `build-server shutdown` cured it.
(2) A THIRD DecodeCobolString twin (StatementBinder-local) shadowed the two the design named — found only because
the boolean golden probe stored raw `B"01`; all three per-layer twins now strip the N/B prefix and name each
other. (3) W1's blind tests found FIVE real holes the goldens missed: the 88-predicate renderer spliced raw
B-literal text into C# (category-aware isString + pad/collate fix); a B-literal in an ADD emitted raw text
(NumLiteral 0844 arm); INSPECT's usage guard blocked spec-legal national targets (SR1 admits national — widened);
an EXTERNAL record with a rejected leaf SILENTLY dropped its sharing semantics (CallMakeExternal now 0899s with
the RejectReason); rejected-class references now carry WHY at runtime (`RefFailure` threads RejectReason). Two
W1 test sources were themselves non-conforming (bare CALL literals need BY CONTENT §14.9.4.2; COMPUTE's
primaryExpression is structurally numeric-only) — fixed with citations.

**Battery:** greenfield conformance (full run pending at entry time — the NationalBoolean batteries 56/56, the
two goldens byte-exact via CLI probe) · unit 159/159 (was 130) · LEGACY ConformanceTests 55/55 with the ONE new
`LegacyDivergent` (national_data — trailing-trim, §14.9.11.4 GR6) · FULL legacy guard next (the .g4 change).
Deep-dive currency: `COBOLNET_DATA_MODEL_DESIGN.md` D8 records D-N1..D-N4/D-B1 and supersedes the stale
`PIC 1 → bool` mapping sketch. Residue: the reconciliation's 22-item ledger (bit operators = increment 2, design
ready; NX"/BX"; national-edited; -N intrinsics; RESIDUE-11 coordination…).

