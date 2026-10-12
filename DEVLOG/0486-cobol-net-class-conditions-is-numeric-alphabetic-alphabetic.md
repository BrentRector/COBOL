## Entry 486 — COBOL.NET: class conditions (IS NUMERIC / ALPHABETIC / ALPHABETIC-UPPER / -LOWER)

A G2 tail (ISO §8.8.4.1.4) the condition renderer was failing loud on. New `CobolNet.Runtime.CobolClass` predicates:
ALPHABETIC is the closed Latin set {A–Z, a–z, space} (NOT `char.IsLetter` — COBOLNET_DESIGN §11.2); NUMERIC is
digits 0–9 with one optional leading/trailing separate sign. `BoundClassCondition(Operand, ClassKind, Negated)`;
the binder resolves the four built-in classes (user-defined SPECIAL-NAMES CLASS still loud); the renderer folds a
typed-numeric operand's IS NUMERIC to `true` (it can only hold digits, §6.6) and otherwise checks the character
image at run time.

The differential net caught a bug pre-commit: an early IsNumeric honored an over-punched units digit, so `"12A"`
(plain alphanumeric) wrongly tested NUMERIC. Over-punch is honored only for a SIGNED item — and a signed numeric
field already folds to `true` before reaching the runtime — so the runtime check (which only ever sees alphanumeric
content) is digits-only; `"12A"` → NOT NUMERIC, matching the legacy.

Verified: full-solution build clean Debug (0/0); Conformance 129 green (13 new class-condition differential tests:
NUMERIC/ALPHABETIC/UPPER/LOWER, NOT-forms, embedded-space, numeric-field fold) + Unit 3 green; legacy oracle
untouched.

DEVLOG 486.

