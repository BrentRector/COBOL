## Entry 506 — COBOL.NET: level-88 over a REDEFINES view + on a group — ONE item→Place builder, spec-aligned condition

The RESUME-AT #1 from Entry 505 was the "systematic Tier-B `.BB` blocker" — `'Program._T_n' does not contain a
definition for 'BB'`, common to NC211A/250A/252A. Entry 505's preliminary theory was that
`ReferenceResolver.Resolve`'s `item.Class is { Tier: StringCanonical }` branch was being missed for `BB`. **That theory
is wrong, and the generated C# falsifies it.** In NC211A the MOVE statements emit the CORRECT
`IF_D35._redef_BB = CobolString.SpliceInto(...)` — so `Resolve` resolves `BB` to its Tier-B window perfectly. Only the
**level-88 condition** sites emit the bad `IF_D35.BB` / `IF_D35.BB_2.AAA`. The two paths disagree because they build
the conditional-variable place differently: a verb operand goes through `Resolve` (view-aware), but a level-88 / SET
condition goes through `ReferenceResolver.ResolveItem(DataItem)`, which **always** built a plain `MemberPlace`,
bypassing the whole REDEFINES machinery. Lesson: read the *emitted code*, not the data model, to locate a codegen
disagreement — the contradiction was visible in two adjacent `switch` cases.

**Fix 1 — one item→Place builder (singular-pattern).** Factored the post-resolve "item + subscripts → `Place`" logic
out of `Resolve` into `PlaceForItem(item, indexExprs)` — the ONE place that turns a `DataItem` into a view-aware
`Place` (Tier-B → `RedefViewPlace` window; Tier-A → the canonical's field reinterpreted; else `MemberPlace`). Both
`Resolve` and `ResolveItem` now route through it, so EVERY consumer — verb operands, level-88 / SET conditional
variables, FD record areas — sees identical view resolution. The two `new MemberPlace` sites were the entire pattern.

**Fix 2 — a level-88 test follows the relation-condition rules (a second, pre-existing bug).** Clearing the `.BB`
error in NC250A exposed `CobolString.Compare(TABLE_86, "ABC")` — comparing a *group struct* (`_T_244`) to a string.
`RenderCondition88` rendered the conditional variable with a raw `c.Parent.Read()`. Per **ISO §8.8.4.5 GR2** "the
rules for comparing a conditional variable with a condition-name value are the same as those specified for relation
conditions", and **§8.8.4.1** "for comparison, an alphanumeric group item shall be treated as an elementary
alphanumeric data item" — so the group's character *image* must be compared, not the struct. The string branch now
renders the variable via the same `OperandText.AsString(BoundFieldOperand)` a relation condition uses (→ `.AsImage()`
for a group, the window for a Tier-B view, the field for an elementary). Only the group case changes; every other
case is byte-identical. (Confirmed pre-existing, not a regression, by a stash-and-recompile of NC250A at the baseline:
it already had both the `.BB` and the `_T_244`→string errors; Entry 505 only noted the `.BB` one.)

**Fix 3 — `EmitImageInto` robust to a Tier-B view record.** Making `ResolveItem` view-aware means a multi-01 FD whose
shared area is a synthesized Tier-B REDEFINES now resolves `records[0]` to a `RedefViewPlace` (a string window, no
`FromImage`). `EmitImageInto` (the READ-into-record-area path) now routes a view record through `.Write` (splice the
padded image into the backing), mirroring `EmitGroupImage`. Verified, not speculative — a new Tier-B multi-01 FD
differential test exercises it and matches the legacy.

**Tests (+3, all pinned to the legacy oracle):** `Condition88DifferentialTests.ConditionName_OverRedefinesView`
(88s on the canonical + on Tier-B-view subordinates + `SET cond TO TRUE` through a view) and `…OnGroupItem`
(group-level-88 image comparison); `FileIoDifferentialTests.MultipleRecordsUnderOneFd_TierBSharedArea`. Conformance
245 → 248; 14 unit; the 7 green NC golden programs unaffected. Greenfield-only (no `Cobol.Net.Frontend` / `CobolSharp.*`
touched).

**Honest status — this does NOT yet green an NC program.** NC211A and NC250A now **compile and run**, but stop partway:
both hit a runtime loud guard `unsupported condition form` at the first **ABBREVIATED** test (NC211A: `CC--TEST-GF-11`)
— i.e. **abbreviated combined relation conditions** (ISO §8.8.4.12, `IF A = B OR C OR D`), an unimplemented binder
feature and the clear next slice to green NC211A. **NC252A stays gated on three separate features** (NOT cleared by
this change): a *numeric* Tier-B view level-88 (`RenderCondition88`'s numeric branch emits `string == long` for a
StoreAsImage view — a later slice), a nested Tier-B view whose backing's containing struct is itself a suppressed
Tier-B view (`REDEF11`/`RDF3` — nested-Tier-B-through-suppressed-parent), and a level-66 `RENAMES` that `Resolve`
still loud-rejects (a later slice per the design's commit order). The Entry-505 framing "clears NC211A/250A/252A" was
optimistic; the redefines-view fix is genuine Tier-B correctness, but greening those programs needs the verbs above.

