## Entry 505 — COBOL.NET: figurative ZERO in a level-88 VALUE; the systematic Tier-B `.BB` blocker identified

The third figurative-in-numeric path: a level-88 condition with a figurative ZERO VALUE on a NUMERIC conditional
variable. `ConditionRenderer.RenderMembershipTest` rendered the VALUE via `UnscaledAtScale("ZERO", scale)` → at scale
2 that produced `ZERO00L` (the word + the scale's zero-padding + the `L` suffix), invalid C# (NC250A). New
`NumericMembershipValue` maps a figurative ZERO VALUE to `0` before scaling. Verified: `88 IS-Z VALUE ZERO` on a `9(3)`
/ `9V99` variable compares against 0 correctly. +3 `FigurativeDifferentialTests` pinned to the legacy. Conformance
242 → 245; 14 unit; green.

**Found while here — a SYSTEMATIC Tier-B REDEFINES blocker (NEXT SESSION):** clearing NC250A's `ZERO00L` exposed the
SAME error NC211A has — `'Program._T_n' does not contain a definition for 'BB'`. The shape is an ELEMENTARY item
redefined by a GROUP (`02 BB PIC X(2). 02 BB-2 REDEFINES BB. 03 SUB-SUB-BB …`), and NC252A's `REDEF10.RDF3.RDF3_5_15`
is the deeper-nested variant. The FieldEmitter correctly suppresses the member and emits the single Tier-B backing
(`_redef_BB`), but the **reference site still emits a plain member path** (`IF_D35.BB` / `REDEF10.RDF3.…`) instead of a
`RedefViewPlace` window over the backing — so the two disagree (CS1061). `ReferenceResolver.Resolve` takes the
StringCanonical branch only on `item.Class is { Tier: StringCanonical }`; the diagnosis (why BB / the nested
subordinate doesn't hit that branch — qualified-resolution dropping the class? a group-redefines-elementary
classification gap? a subordinate-of-a-view whose own `Class`/`ClassOffset` isn't a window?) is the next slice. This is
the common blocker for NC211A/250A/252A; fixing it is high-leverage (a Tier-B correctness + several compile-errors).

