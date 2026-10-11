## Entry 1789 — 2026-09-29 16:16 PDT — PB1078 + PB1090: the ASSIGN literal-1 shape, and the empty SPECIAL-NAMES paragraph

A second batch, fixed while the first (DEVLOG 1788) waited on CI.

- **PB1078** — `assignTarget : cobolWord | STRINGLIT` was wrong in both directions against §12.4.5.2 SR4: the zero-length `ASSIGN TO ""` compiled (OPEN then answered 30) while X"..." and a concatenation expression were parse errors. The grammar now admits `nonNumericLiteral` and `AssignTargetRule.Text` screens SR4 BY NAME (COBOLNET2645). `AssignTargetRule.Resolve` takes the `LiteralEnvironment` so a concatenation folds through the one `ConcatFolder`. Closes SR-12.4.5.2-4 and GR-8.8.3.3-3.
- **PB1090** — `SPECIAL-NAMES. .` (§12.3.7.3 SR31) was COBOL0001 at every edition; the paragraph is now `SPECIAL_NAMES DOT (specialNameEntry+ | DOT)?`. Closes SR-12.3.7.3-31.

GAP 813 to 810.

