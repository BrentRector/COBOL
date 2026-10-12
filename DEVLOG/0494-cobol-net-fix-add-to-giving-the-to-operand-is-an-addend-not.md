## Entry 494 — COBOL.NET: fix ADD … TO … GIVING (the TO operand is an addend, not dropped)

A harness finding from the Tier-B work: `ADD A TO B GIVING C` gave `C = A` instead of `C = B + A` — `BindAdd` returned
`BoundAddGiving(addends, …)` from the GIVING phrase but ignored the TO-phrase operand. Per ISO §14.9.1 Format 3, the
TO operand participates in the sum (and is NOT itself a receiver — only the GIVING operands receive). Fix: when both
a TO phrase and a GIVING phrase are present, the TO operand(s) are appended to the addends. (`SUBTRACT … FROM …
GIVING` already handled its analog correctly via `BoundSubtractGiving`.) 5 differential tests
(`ArithmeticGivingDifferentialTests`): ADD-TO-GIVING (+ B unmodified), ADD-several-GIVING, SUBTRACT-FROM-GIVING,
MULTIPLY-GIVING, DIVIDE-INTO-GIVING. Conformance 169→174 green; legacy oracle untouched. (The other harness finding —
`MULTIPLY … ROUNDED` truncating — remains the next item: ROUNDED is a per-target phrase, `roundedPhrase: ROUNDED
(MODE IS? roundingModeName)?`, needing a per-receiver rounding mode threaded through the arithmetic bound nodes +
emitter; it is on NC101A's pass path, alongside ON SIZE ERROR.)

