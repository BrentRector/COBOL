## Entry 481 — COBOL.NET: widen the differential net before the G2-2 bound-tree rebuild

Per the reviewing advisor: the bound-tree rebuild (G2-2) is behavior-preserving, so it is only as safe as the net is
wide. Before starting, pin the expression + multi-target verb forms the rebuild re-routes. `ExpressionDifferentialTests`
(16): operator precedence, parentheses, nesting, exponentiation, negative/large literals, scaled COMPUTE with
division, multi-target MOVE / ADD TO / ADD GIVING / multi-operand SUBTRACT / multi-target COMPUTE, and a DISPLAY
mixing operand kinds. 15 pin to the legacy oracle; 1 (mixed DISPLAY with a mid-list alphanumeric operand) is
spec-pinned — it re-surfaces the legacy DISPLAY trailing-trim non-conformance (ISO §14.9.11.4 GR6), confirming the
G2-0 finding holds for interior operands too.

Conformance suite 77→93 green. Next: the G2-2 bound-tree rebuild (the advisor's pivot — build the real semantic
model before G3/G4 route through it; commit (a) semantic rebuild, commit (b) §2.2 decomposition).

DEVLOG 481.

