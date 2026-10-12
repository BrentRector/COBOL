## Entry 1202 — 2026-08-07 10:35 PDT — R01: the constant substituted its literal everywhere except where a function took it

**§13.10.4 GR1 makes these two lines the same program, and only one compiled:**

```
FUNCTION UPPER-CASE(K-TEXT)     ⛔ COBOLNET0844: constant-name 'K-TEXT' substitutes a literal of category
                                   Alphanumeric and is not a numeric operand (ISO §8.8.1.1 / §13.10.3 SR2)
FUNCTION UPPER-CASE("abcdef")   ✅
```

with `01 K-TEXT CONSTANT AS "abcdef".` — GR1: "the effect of specifying constant-name-1 in other than this entry
is as if literal-1 … were written where constant-name-1 is written", and §13.10.3 SR2 admits it "anywhere that a
format specifies a literal of the class and category of constant-name-1".

**CAUSE — one arm short in the argument path.** `IntrinsicBinder.BindArgOperand` handles `OMITTED`, a phrase
word and a written non-numeric literal, then falls through to `BindFunctionArgumentExpr` — the §8.8.1.1
**numeric-expression** bind. A constant-name resolves there, and that path is RIGHT to demand class numeric,
because an arithmetic operand must be. **The argument was never an arithmetic expression**: its legality comes
from the function's own §15.x argument rule, which the file's comment two lines below states in as many words —
for DATA items. Constants never got the same treatment. The fix is one arm ahead of the expression path: a sole
data reference naming a NON-numeric constant becomes a literal operand of the constant's own category.

⚙ **THE SWEEP BOUNDED THE SCOPE RATHER THAN WIDENING IT.** `MOVE K-TEXT TO R`, `IF K-TEXT = "abcdef"`, `INSPECT`
and `DISPLAY K-TEXT` all already substituted correctly — measured, not assumed. So this is not the substitution
MECHANISM being broken, it is the ARGUMENT path alone, exactly as the note's title said. The negative result is
what makes "argument positions" the scope instead of a guess.

⚠ **A NUMERIC CONSTANT DELIBERATELY FALLS THROUGH, and assertion 8 of the golden is why.** The expression path
already substitutes it correctly and it must keep participating in arithmetic: `FUNCTION MAX(K-NUM + 1, 2)` is an
EXPRESSION argument, not a bare literal, and only that path can bind it. Capturing every constant in the new arm
would have traded one rejection of legal source for another.

⭐ **AND THE REJECTION DIRECTION IMPROVED WITHOUT BEING TOUCHED.** `FUNCTION SQRT(K-TEXT)` was rejected before
and is rejected now, but the reason moved from the numeric-expression path's *"is not a numeric operand (ISO
§8.8.1.1)"* to `COBOLNET1627: FUNCTION SQRT argument-1 is of class alphanumeric; ISO §15.3 requires class numeric
(§15.84.3 r1)` — the function's OWN argument rule, which is what the source actually violates. That screen is
PB52's, landed an hour earlier in this same session; the two changes met without either being written for the
other.

**GATE.** Conformance `~Corpus|~Negative|~Constant|~Intrinsic|~VersionMatrix` **2653/2653** zero skipped · Unit
**4029/4029** zero skipped. Golden `r01_constant_name_intrinsic_argument` — 8 assertions built as PAIRS
(constant vs the literal it substitutes), because that equality IS GR1 and is a stronger claim than either value
alone. Negative `r01-constant-name-wrong-class-argument`.

⚠ **PROCESS: the `cd`-persistence mistake bit a second time.** A `cd` into the scratchpad carried into a later
repo-root `python` in the same compound command, so a manifest registration threw `FileNotFoundError` while the
`.out` file (absolute path) was written normally. Caught by reading the output. The habit to drop is combining a
scratchpad `cd` with repo-root operations in one command — the first occurrence, one item earlier, silently
skipped a golden registration AND the whole conformance gate.

