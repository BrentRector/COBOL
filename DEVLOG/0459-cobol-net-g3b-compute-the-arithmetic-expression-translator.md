## Entry 459 — COBOL.NET G3b: COMPUTE + the arithmetic-expression translator

`COMPUTE` lands with a recursive COBOL-arithmetic→C# translator (`RenderArith`) that preserves operator precedence
(the grammar encodes additive > multiplicative > power), handles parentheses, unary sign, and `**` (→ `Math.Pow`),
and evaluates the whole expression in `decimal` — every operand cast to `decimal` so division is COBOL-real, not
C# integer division — then truncates into the target's storage type. The same translator feeds `valueOperand`, so
IF/relational conditions reuse it next (G3). Verified: `R = A + B*2` → 16, `(A+B)*2` → 26, `A**2` → 100, with clean
generated C#. ROUNDED / SIZE ERROR / intermediate-precision rules land when arithmetic stores route through the
full ported `CobolNum`.

