## Entry 424 — byte engine: VALUE on an OCCURS item now initializes EVERY occurrence (ISO 2023 §13.18.63.4 GR 9)

Owner chose "fix the byte engine" for the numeric-OCCURS blocker (423). A 3-agent investigation workflow nailed it:
the byte-path VALUE-init has two branches and **only the alphanumeric one was OCCURS-aware**. The numeric branch
(`CilEmitter.EmitValueClauseInitialization`, ~792) called `PicRuntime.MoveNumericLiteral` once with `destLength =
loc.Length` (the FULL OCCURS span = elementSize × MaxOccurs), so the value was right-justified across the *entire*
span — landing only in the LAST occurrence, every earlier one showing the leading-zero fill (`000`). The string
branch already looped via `MoveStringToOccursField`, which is why char tables were correct. Spec confirms the target:
**§13.18.63.4 GR 9 — a VALUE on (or subordinate to) an OCCURS clause assigns the value to EVERY occurrence** (the
COBOL-2002 table-format VALUE; COBOL-85 prohibited it).

Fix mirrors the alphanumeric path: new `PicRuntime.MoveNumericLiteralToOccursField(area, baseOffset, elementSize,
occursCount, elementPic, value, rounding)` loops `MoveNumericLiteral` per element; the emitter's numeric branch now
passes `elementSize` + `totalOccurs` (from the existing `ComputeOccursExtent`) instead of the full span. Placement
uses the per-element `destLength` (not `pic.StorageLength`), so no separate element PIC was needed. **For a non-OCCURS
numeric, `totalOccurs==1` / `elementSize==loc.Length`, so it emits exactly one call — byte-identical to before.**

Verified: `05 N PIC 9(3) OCCURS 3 VALUE 7` → all three now `007` (was `000,000,007`); scaled `D OCCURS 2 VALUE 1.5`
→ both `1.5`. **Blast radius turned out empty: the 65 programs the investigator flagged were over-counted (level-88
condition VALUEs, adjacent `FILLER … VALUE SPACE` siblings, or char VALUEs on the already-correct string branch) —
the full guard shows ZERO baseline shifts and an IDENTICAL NO-BASELINE set.** New conformance test
`tests/conformance/2002/table_value_occurs.cob` (numeric + scaled + char occurrences). Guard **ALL GREEN: 1196 unit
/ 501 integration / 364 NIST**. This unblocks OCCURS slice 2 (numeric typed tables) — next.

