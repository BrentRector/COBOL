## Entry 378 — M2-PROC-2: `INSPECT … BACKWARD` (ISO §14.9.21, COBOL-2002)

Plan Wave item. COBOL-2002 added the BACKWARD phrase to INSPECT: inspection proceeds from the rightmost
character position to the left, which changes FIRST (→ rightmost occurrence), LEADING (→ the trailing run),
and BEFORE/AFTER region anchoring. Rather than fork every scan loop, it is realized as a **reverse-wrapper**
in the runtime — the key insight being that *scanning the original right-to-left is identical to scanning the
reversed string left-to-right*, as long as each multi-character operand and delimiter is also reversed (so
"AB" read right-to-left matches "BA" forward). The existing forward `TallyingPass`/`ReplacingPass`/`Convert`
cycles then run unchanged on the reversed inputs:
- TALLYING reverses text + patterns/delimiters; per-operand counts are direction-independent → **no**
  result un-reversal.
- REPLACING reverses text + patterns + replacements + delimiters, runs the cycle, then reverses the result
  buffer back (both the raw-text and the signed-numeric GR-4d paths).
- CONVERTING reverses text + the BEFORE/AFTER delimiters (FROM/TO sets are positional char maps, **not**
  reversed), converts, reverses back.

BEFORE/AFTER roles are preserved under reversal: the region "before the first match" found scanning backward
is exactly "before the first match" in the reversed-forward frame (verified by hand against §14.9.21, then by
test). The whole feature is one `bool backward` threaded end-to-end:

  lexer `BACKWARD` token (new reserved word — not used as an identifier anywhere in the corpus) →
  grammar `INSPECT BACKWARD? dataReference …` → `BoundInspectStatement.Backward` (BindInspect reads
  `ctx.BACKWARD()`) → `IrInspectTallying/Replacing/Convert.Backward` → StringLowerer → CilStringEmitter
  pushes one `ldc.i4` and widens each `GetMethod` signature by `typeof(bool)` → `InspectRuntime` wrapper.

Verified backward vs the exact forward mirror on one program — FIRST "A" on "ABABA": backward `ABAB*`
(rightmost) vs forward `*BABA` (leftmost); LEADING "0" on "12300": backward count 2 (trailing run) vs forward
0; CONVERTING "AB"→"XY" BEFORE INITIAL "-" on "AB-AB": backward `AB-XY` (segment right of "-") vs forward
`XY-AB`. The forward path is byte-for-byte unchanged (`backward=false` default). Conformance
`tests/conformance/2002/inspect_backward`.

Guard ALL GREEN: **1047 unit / 470 integration / 364 NIST**, 0 regressions.

