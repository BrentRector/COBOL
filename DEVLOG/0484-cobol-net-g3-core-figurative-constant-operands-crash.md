## Entry 484 — COBOL.NET G3-core: figurative-constant operands + crash-proofing (driven by a real NC program)

Per the advisor: the real verification is the 364-NIST corpus, not the hand-picked fragment net. So I ran a real
CCVS program (NC101A) through COBOL.NET to scope what's actually needed — and it immediately found what fragment
selection can't:

- **A compiler CRASH (a §1.4 violation).** `NumericRenderer.FieldNum` did `p.Item.Pic!.IsFloat` — but `Pic` is null
  for a **group item used in a comparison** (a group compares as alphanumeric, ISO §8.8.4.1.1) → NullReferenceException
  in the emitter. Fixed: `FieldNum` guards a null/group Pic → loud-failure (never crash); `OperandText.IsString` now
  treats a group operand as a string so the comparison takes the string path.
- **Figurative constants in operand position (ubiquitous).** `figurativeConstant` lives under `nonNumericLiteral`, so
  the binder mis-bound `MOVE ZERO`/`SPACES`/`DISPLAY ZERO` as a numeric literal `"ZERO"` → uncompilable `ZEROL`
  (another §1.4 violation — bind success must emit compilable C#). New `BoundFigurative(Kind)` operand; the binder
  detects it in literal + comparison positions; the backend materializes it per context (ISO §8.3.1.2): DISPLAY → one
  occurrence (GR3); MOVE → filled to the receiver width; comparison → the other operand's category/width (numeric
  anchor ⇒ ZERO is 0; alphanumeric anchor ⇒ a string of the anchor's width). HIGH/LOW = U+00FF/U+0000 (§14.9). ALL
  "x" forms deferred (loud).

Result: **NC101A now compiles cleanly** (was: NPE, then `ZEROL`) and **fails LOUD at run time** at the first real
gap (`OPEN OUTPUT PRINT-FILE` — file I/O, G5) — exactly the §1.4 contract. Its loud-guard tally scopes the remaining
work precisely: **88 GO TO + EXIT (G4 PC dispatcher), 106 whole-group MOVE (G6), file I/O (G5)** — confirming **G4
is the biggest control-flow unlock** (the CCVS framework is GO-TO-heavy).

Verified: full-solution build clean Debug (0/0); Conformance 104 green (11 new figurative differential tests: MOVE/
DISPLAY/comparison) + Unit 3 green; legacy oracle untouched. NEXT → **G4: the PC dispatcher** (GO TO + paragraphs as
pc cases + PERFORM as recursive bounded dispatch + EXIT family), which retires the sequential-paragraph stopgap and
unblocks the first real NC program through the differential harness.

DEVLOG 484.

