## Entry 511 — COBOL.NET: IS NUMERIC over an alphanumeric operand is digits-only (ISO §8.8.4.4 GR2) — NC211A GREEN

The last GF-48 failure. GF-48 is a compound condition exercising every simple-condition type and connector; its truth
hinges on `CLASS-1 NOT NUMERIC` where `CLASS-1` is `PIC X(5)` = `"+1234"`. COBOL.NET evaluated `IS NUMERIC` as true
(so `NOT NUMERIC` false), making the whole condition false. `CobolClass.IsNumeric` was stripping a leading/trailing
separate sign. The spec is decisive: **§8.8.4.4 rule 2** — "If the category of the data item … is not numeric, the
condition is true if the content … consists entirely of the characters 0, 1, 2, 3, …, 9." An operational sign is NOT
a valid character. This runtime test is reached ONLY for a non-numeric (alphanumeric / edited) operand — a
numeric-category COBOL.NET field IS NUMERIC folds to `true` at compile time (rule 1 / GR1, COBOLNET_DESIGN §11.2) —
so `IsNumeric` must implement rule 2: entirely digits, no sign. `"+1234"` and `"12-"` are non-numeric.

**🟢 NC211A IS GREEN** — diff 0 against the NIST golden, locked into `NistDifferentialTests` as the **8th** byte-matching
NC program and the FIRST nucleus *conditional* program. It took this session's whole stack of spec-grounded slices,
each built from the cited §: abbreviated combined relation conditions (§8.8.4.12, DEVLOG 507), level-88 over a
REDEFINES view / on a group (§8.8.4.1/§8.8.4.5, 506), the whole-group image over OCCURS (§14.9, 508),
signed→alphanumeric de-signing (§14.9.25.4 GR6a, 509), `ALL "literal"` (§8.3.3.6.4, 510), and now IS NUMERIC rule 2.

**Tests — SPEC-ANCHORED** (`ClassConditionDifferentialTests`, +3): `"+1234"`/`"12-"` IS NUMERIC → false per §8.8.4.4
rule 2, cross-checked against the legacy (which agrees — the golden requires it). NC211A added to the golden
regression. Conformance 279 → 283; 14 unit; the now-8 green NC golden programs all pass. Greenfield-only.

