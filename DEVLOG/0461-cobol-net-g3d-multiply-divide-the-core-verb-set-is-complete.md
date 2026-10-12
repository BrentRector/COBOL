## Entry 461 — COBOL.NET G3d: MULTIPLY / DIVIDE — the core verb set is complete

`MULTIPLY a BY b [GIVING c]` and `DIVIDE a INTO/BY b [GIVING c]` round out the arithmetic verbs (REMAINDER + the
non-GIVING multi-target edge cases deferred). They reuse the decimal-intermediate + `EmitArithAssign` machinery, so
division is COBOL-real then truncated into the target. Verified: `MULTIPLY A BY B GIVING R` (12×4=48),
`DIVIDE A INTO B GIVING R` (4/12→0), `DIVIDE A BY B GIVING R` (12/4=3), `MULTIPLY 3 BY A` (A→36). **The greenfield
core verb set — DISPLAY, MOVE, ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE, IF/ELSE, STOP RUN/GOBACK — now compiles
COBOL → clean idiomatic C# → runs correctly, with zero byte substrate.** NEXT (G4): the control-flow engine —
out-of-line PERFORM (paragraph/THRU), GO TO, fall-through, inline PERFORM (TIMES/UNTIL/VARYING) — then groups→record
structs + OCCURS→arrays (G2 cont), then drive the NIST corpus (G5).

