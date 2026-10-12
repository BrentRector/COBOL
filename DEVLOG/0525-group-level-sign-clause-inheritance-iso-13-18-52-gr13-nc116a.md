## Entry 525 — 2026-06-10 00:35 PDT — Group-level SIGN clause inheritance (ISO §13.18.52 GR1–3) → NC116A GREEN (16th NC byte-match)

**The fix the resume prompt teed up as the closest green.** NC116A had exactly ONE failing test left: GF-17
"PRECEDENCE OF SUBORDINATE SIGN CLAUSE" — `01 TEST-17-DATA SIGN TRAILING.` / `03 TEST-17-GROUP SIGN LEADING
SEPARATE.` / `05 TEST-17-C PIC S9(4).` with a REDEFINES view reading TEST-17-C's first character. Expected `+`
(the group's LEADING SEPARATE applies to the subordinate), got `1` (the binder consumed only the item's OWN
`signClause` — `DataBinder.BindEntry` dropped a group-level SIGN entirely, so TEST-17-C defaulted to
TrailingOverpunch, width 4, and the view read the first digit).

**Spec (the authority):** §13.18.52 GR1 — a SIGN clause on a group applies to EACH signed numeric item subordinate
to it; GR2/GR3 — a clause on a subordinate group/elementary entry takes PRECEDENCE (nearest-ancestor-wins); GR6a —
a SEPARATE sign is its OWN leading/trailing CHARACTER position (not a digit position), `+`/`-`. (Syntax rule 1
allows SIGN on an alphanumeric group item; GR4 keeps the no-clause default implementor-defined = our trailing
over-punch.)

**Implementation (the design's one-computation rule):**
- `PicInfo`: new `SignSpec(bool Leading, bool Separate)` record — THE carrier for a SIGN clause's content;
  `Analyze` now takes `SignSpec?` (was 3 loose bools); `SignKindFor(usage, signed, SignSpec?)` is now public —
  the single SignKind computation, shared by entry-bind and the inheritance pass.
- `DataItem.OwnSign` — the entry's own SIGN clause, captured on EVERY item including groups (was discarded for
  groups since they have no PICTURE).
- `DataBinder.InheritSignClauses()` — new post-build pass, runs FIRST in the post-build sequence: DFS from each
  root threading the nearest enclosing `SignSpec`; an item's own clause replaces the inherited one on the way
  down (GR2/GR3); a signed numeric DISPLAY leaf with NO own clause gets `Pic with { SignKind = … }` re-derived
  from the inherited clause (GR1). **Pass ordering matters:** it must precede `ClassifyRedefinesClasses` because
  a SEPARATE sign adds +1 to `ImageWidth` (§13.18.52 GR6a — `DataItem.ElementaryImageWidth` already counts it),
  which feeds the REDEFINES class-max width. With the width right, the Tier-B machinery does the rest unchanged:
  TEST-17-C (class anchor, StoreAsImage) formats `+1234` and the `PIC X` view at offset 0 reads `+`.

GF-18 (own-clause precedence over the 01's) already passed — own-clause handling predates this; the whole-corpus
sweep stays clean. **Result: NC116A byte-matches the golden → locked into `NistDifferentialTests` (16th NC
program). 308 conformance + 15 unit green, zero regressions.** (GF-16's group-SIGN-on-01 cases were already
passing because TRAILING over-punch ≡ the default — the inheritance only changes behavior where a group says
LEADING and/or SEPARATE, exactly the spec'd delta.)

