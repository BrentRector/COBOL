## Entry 018 — 2026-03-14: The Real Bug Was in the Emitter

### Discovery

After a full day of chasing parser bugs, the actual blocker was in the CIL emitter.
`EmitDecimalConstant` crashed on non-integer decimal values due to a Cecil type mismatch
(passing byte to Ldc_I4 opcode). `EmitPerformTimes` crashed on ambiguous `op_Explicit`
method resolution.

NC101A was never failing to PARSE — it was failing to EMIT. The parser was correct.
A full day of parser "fixes" was spent fixing a problem that didn't exist in the parser.

### Fixes
1. `EmitDecimalConstant`: replaced decimal(int,int,int,bool,byte) constructor with
   decimal.Parse for non-integer values
2. `EmitPerformTimes`: filtered op_Explicit by ReturnType to resolve ambiguity

### NIST Results
- Before fix: 79/391 (20%)
- After fix: 95/391 (24.3%)
- 16 programs were parsing correctly but crashing in code gen

### Lesson
When a compilation fails, check WHICH PHASE fails before assuming it's the parser.
The unhandled exception was at the emitter level, not the parser. I spent a day fixing
the wrong component.

---

