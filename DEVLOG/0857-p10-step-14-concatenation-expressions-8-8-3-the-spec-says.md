## Entry 857 — 2026-07-16 04:44 PDT — P10 Step 14 — `&` concatenation expressions (§8.8.3): the spec says LITERAL, not operator — one ConcatFolder chokepoint, no BoundConcat, no emitter leg

**The spec read overrode the plan's shape.** Step 14's recipe (written at catalog time, before the §8.8.3 read)
guessed a `BoundConcat` bound node + an emitter arm + "a runtime concat for boolean bit-string concatenation".
The actual §8.8.3 text kills all three: the general format's operands are ONLY literals / figurative constants /
concatenation expressions — an identifier can never appear — and GR3 (:9467) states the construct "shall be
equivalent to a literal of the same class and value, and may be used anywhere a literal of that class may be
used". That is a COMPILE-TIME literal by definition. So the landed shape is a fold: `Binding/ConcatFolder.cs`
collapses the parse-tree `concatenationExpression` into the equivalent single literal once, and every consumer
rides the pre-existing literal channels. `B"01" & B"10"` folds to `B"0110"` at compile time like any other pair —
there is no runtime leg to write. (Per `feedback_use_the_spec`: derive from the § FIRST; the plan text was the
guess, the spec is the authority.)

**Grammar (SUPERSET PARSE).** `AMPERSAND : '&'` (CobolLexer.g4 — §8.7.3; the "immediately preceded and followed
by a separator space" rule is token-stream lenient, same posture as `::` §8.7.4; `&` had no prior lexical role,
so no collision) + a `concatenationExpression : concatOperand (AMPERSAND concatOperand)+` tier added as the
FIRST alternative of `nonNumericLiteral` (CobolExpressions.g4). Adding it INSIDE `nonNumericLiteral` is the
load-bearing choice: every literal position — VALUE clauses, statement operands, EVALUATE/IF comparisons,
FUNCTION arguments, CALL/CANCEL/STOP literals, ALPHABET/CLASS operands, OPTIONS INITIALIZE — inherits the
construct through the ONE rule, which is exactly GR3's "anywhere a literal may be used". No shared-rule
restructure (the plain alternatives are untouched); `&` appears in no other rule, so ALL(*) prediction is exact.
Numeric literals are excluded from `concatOperand` by construction (SR1 admits only the three classes), so
`5 & "A"` is a parse error, not a semantic case.

**The §8.8.3.2 SRs, one code per rule (DiagnosticCatalog descriptors, the 15xx band):** `COBOLNET1540`
concat-class-mismatch (SR1 same-class; figurative operands adapt per GR1a, both-figurative ⇒ alphanumeric GR1b;
NULL rejected — no character value, §8.3.3.6.3 F8; in class boolean only ZERO folds — a boolean character is 0
or 1, §8.3.3.4), `COBOLNET1541` concat-all-figurative (SR1 second sentence — no ALL-prefixed figurative),
`COBOLNET1545` concat-result-too-long (SR2–SR4, 8,191 positions). Figuratives fold ONE character each
(§8.3.3.6.4 GR3a); HIGH-/LOW-VALUE take the PCS extremes when a PROGRAM COLLATING SEQUENCE is active
(§8.3.3.6.3 F4/F5, `DataBinder.Collating` is bound before any fold site runs) else the native U+00FF/U+0000
pins; class national never takes the alphanumeric PCS (the D-N3 pin, matching `FigurativeConstants.Fill`).
A concat in a numeric context is the same 0844 posture as a bare national/boolean literal (§8.8.1.1).

**Consumers wired through the one fold** (the FigurativeOperand precedent — a shared chokepoint called from each
verb binder): `ExpressionBinder.LiteralOperand`/`ConcatOperand`/`NumLiteral`; `ConditionBinder` comparisons plus
the boolean channel (`IsBooleanValueOperand` classifies via the diagnostic-free `ConcatFolder.ClassOf` so the
fold — and its diagnostics — runs exactly once on the bind path); `EvaluateBinder`; `InspectBinder`;
`IntrinsicBinder.NonNumericOperand` (FUNCTION LENGTH("AB" & "CD") → 4); `CallBinder` CALL/CANCEL literal-1;
`ControlFlowBinder` STOP; `OoBinder` INVOKE method-name + BY CONTENT literal args (and its inline X"…" decode
unified into the new `CobolLiteral.DecodeHex` — the ONE hex codec); `DataBinder.ExtractValue`/
`RawValueOperandText` (01-level + level-88 + Report Writer VALUE — the raw-TEXT pipeline gets the folded
`RawText` re-quoted form, because `GetText()` on a concat GLUES tokens: `"AB" & "CD"` → `"AB"&"CD"`, which
`CobolLiteral.Decode` would silently mis-read — the one real silent-corruption hazard, found by sweeping every
`GetText()`-on-literal site); `DataBinder.Switches` ALPHABET/CLASS `LiteralChars`; `OptionsBinder` INITIALIZE
fill. The 88-range/singleton paths fold ONCE per operand (shared local) so ValidateValueCategory and the stored
value see the same text without doubled diagnostics.

**Edition gate (the Step-E funnel, recognition-based).** `VisitConcatenationExpression` in the
VersionConformancePass parse arm → `Check(Constructs.ConcatOperator2002)` — POSITION-BLIND (unlike the
statement-scoped NATLIT/BOOLLIT gate: no data-side gate covers concat, and GR3 puts it in VALUE clauses too).
`constructs.json` row `concat-operator-2002` flipped pending→ACTIVE (+`expectDiagnostic` 0900), regen via
`gen-constructs.ps1`; `gen-diagnostics-doc.ps1` regenerated `docs/DIAGNOSTICS.md` for the three descriptors.

**Verified by RUNNING.** CLI: `--std 2002 --run` → `ABCD` / `XYZ` / `4` / `A B` (VALUE, MOVE, FUNCTION LENGTH,
figurative SPACE); `--std 85` → one 0900 per concat occurrence; adversarial: `"AB" & N"CD"` → 1540, `ALL "Z"` →
1541, `B"01" & SPACE` → 1540, `N"AB" & N"CD"` VALUE → `ABCD`, `B = B"01" & B"10"` → the boolean relation channel
hit, `X"4142" & "CD"` → `ABCD`. New golden `tests/conformance/2002/literal_concat.cob/.out` ENABLED (every legal
class pair + hex + figuratives + VALUE + 88 + MOVE/DISPLAY/IF/EVALUATE + a chain + LENGTH; output captured from
the run and each line spec-derived first) with the legacy `ConformanceTests` GreenfieldOnly exclusion (the
frozen legacy grammar has no `&` token); negatives `concat_below_2002` (0900 at 85) + `concat_class_mismatch`
(1540 at 2002/2014/2023). Targeted battery: CorpusRunner 188 ✓ (incl. the new golden byte-exact + negatives),
VersionMatrix 1574 ✓ (the row active across all four editions), unit 284 ✓, registry/diagnostic drift 6 ✓,
characterization 33/33 byte-identical ✓ (no existing emission touched), legacy Integration conformance 119 ✓.
The FULL legacy guard (shared .g4 change) rides the supervising battery per the wave contract. Docs: PHASE-10
Step 14 + wave line + CONCAT audit verdict closed; PHASE4_RECONCILIATION boolean-residue item (5) + audit
headline; ISO2023_CONFORMANCE_PLAN new row M2-DATA-4b.

