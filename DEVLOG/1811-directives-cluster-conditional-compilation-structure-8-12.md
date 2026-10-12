## Entry 1811 — 2026-09-30 10:59 PDT — Directives cluster: conditional-compilation structure, §8.12 words as data, DEFINE/TURN formats, placement rules as row data, per-unit LEAP-SECOND

Batch of the frontend/directives defects, each probed against the ISO text first (every citation run through `cite.py --check`).
**PB1363** the frame stack validates structure (phase + library-text id per frame; COBOLNET2649). **PB1364** every `>>WHEN` is parsed
and category-checked whether or not an earlier one matched. **PB806** `NoOperandWords` on the if/evaluate rows (COBOLNET1911).
**PB1366** `CompilerDirectiveWords` = §8.12 as data (drift-tested against the spec table), screened at the DEFINE name, the defined
condition and TURN's file-name slot (COBOLNET2650). **PB1367** DEFINE parsed from its general format (COBOLNET2651), SR2 reaches
`AS PARAMETER`. **PB1533** the PARAMETER environment name is the upper-case compilation-variable-name. **PB1365** TURN `CHECKING` alone
means ON (the printed diagram, PDF p115, was rendered and read: only OFF and LOCATION are underlined) and a §8.12 word is never
file-name-1. **PB1377 / PB1378 / PB1065** placement is DATA on the directive rows (`directivePlacement`, three rule shapes), judged by
the text screen in `DirectiveSiteProcessor` (COBOL-WORDS, COBOLNET2652) and the renamed `Validation/DirectivePlacementPass`
(LEAP-SECOND / PROPAGATE outside every unit - error 2652; FLAG-02/14 and PUSH/POP ALL between clauses - warning 2344); a PUSH/POP
naming a directive inherits its rule; the directive-name set is the rows available at the edition (FLAG-85 out at 2023).
LEAP-SECOND is folded per unit (`LeapSecondState`, `DirectiveResults.LeapSecondEvents`), so a directive between sibling units
works; COBOLNET1650 is retired. **PB1820** (new, landed): object/factory/interface DataBinders never received the LEAP-SECOND state.
Not taken (notes carry the exact remaining work): PB1066 SOURCE FORMAT half, PB1368, PB1373, PB1374, PB1383, PB1538, PB1690, PB1575.
Friction: worktree Bash hooks refuse compound commands and heredocs with escapes - probes were run as plain single commands; the 220-turn
cap was hit mid-batch and the work was finished from the coordinator's instructions.

