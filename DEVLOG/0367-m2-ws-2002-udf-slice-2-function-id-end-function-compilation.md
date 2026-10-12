## Entry 367 — M2 (WS-2002-UDF, slice 2): FUNCTION-ID … END FUNCTION compilation unit (ISO §11.5)

UDF slice 2: a user-defined function is, structurally, an ordinary source unit whose IDENTIFICATION DIVISION
heads with `FUNCTION-ID. name.` instead of `PROGRAM-ID. name.` and closes with `END FUNCTION name.`. So the whole
program-compilation pipeline (SemanticBuilder → Binder → CilEmitter, multi-unit + registration) is reused — a
function unit compiles to a callable program named after the function, returning its value through the
now-working `PROCEDURE DIVISION USING … RETURNING` path (365).

Changes:
- lexer token `FUNCTION_ID : 'FUNCTION-ID'` (mirrors `PROGRAM_ID`; corpus-safe — 0 occurrences in NIST/fixtures;
  maximal munch keeps any hyphenated identifier intact, and the guard confirms no regression).
- grammar: `identificationBody` gains a `functionIdParagraph identificationParagraph*` alternative;
  `functionIdParagraph : FUNCTION_ID DOT programName DOT`; `endProgramHeader : END (PROGRAM | FUNCTION) programName DOT`.
- `Compilation`: the program-name extractors (`ExtractProgramIdFromContext`, `ExtractProgramId`) now read the name
  from a PROGRAM-ID **or** a FUNCTION-ID paragraph, via a shared `UnitName(identificationBody)` helper — so a
  FUNCTION-ID unit gets its function-name as its program id, registers under it, and is resolvable by CALL.

**Verified END-TO-END** (compile + run): a `FUNCTION-ID. DOUBLER.` unit (`PROCEDURE DIVISION USING L-X
RETURNING L-R. … COMPUTE L-R = L-X * 2`) invoked from a separate program as
`CALL "DOUBLER" USING WS-X RETURNING WS-R` → `D=0042`. The function-as-CALL-target proves the unit compiles,
registers, and returns correctly; the `FUNCTION user-name(args)` *expression* invocation is **slice 3** (binder
routes a REPOSITORY-declared name to a user-function call, result type from the function's RETURNING item, emit
CALL + load temp).

Test `FunctionIdUnit_CompilesAsCallableProgram` (dialect cobol2002).

Guard ALL GREEN: **1047 unit / 453 integration / 364 NIST**, 0 regressions.

