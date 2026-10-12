## Entry 476 — COBOL.NET G2-0b: differential regression net for the core verbs

Before the G2 data-model rebuild re-routes every operand through `ReferenceResolver`→`Place`, lock current behavior
in against the legacy oracle. `VerbDifferentialTests` (tests/Cobol.Net.Tests.Conformance): 18 fragments —
ADD/SUBTRACT/MULTIPLY/DIVIDE, COMPUTE (precedence/scale/truncation), IF/ELSE (numeric + alphanumeric + AND/OR),
inline PERFORM (TIMES/UNTIL) — all green vs the legacy on the NIST acceptance basis. Scoped to single-paragraph,
straight-line, truncation-only code (out-of-line PERFORM/GO-TO are G4; ROUNDED/ON SIZE ERROR are G3; signed DISPLAY
is G2d), so every result is numeric and the legacy is a sound oracle. Conformance suite 18→36 green. (Commit
d251c02; this entry restores the DEVLOG-per-commit record that commit omitted.)

DEVLOG 476.

