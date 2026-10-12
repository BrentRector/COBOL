## Entry 354 — M2 CALL RETURNING-into-WS: attempted, found deeper than the recipe, reverted (transparency)

Next M2 item was #9 CALL … RETURNING into WORKING-STORAGE. The recipe said "delete the spurious caller-side
CBL3304 in BoundTreeValidator.ValidateCall (the caller's RETURNING target is unrestricted, ISO §14.9.4.3 SR7)"
and assumed the runtime already worked. It does not.

Deleting CBL3304 lets `CALL "ADDER" USING WS-A WS-B RETURNING WS-RESULT` compile (under `--standard cobol2002`,
which the grammar's is2002() gate requires for the callee's PROCEDURE DIVISION RETURNING), but it then crashes at
runtime — RT0001 buffer access out of range (offset=8, length=4, **bufferLength=0**) — when the callee writes its
RETURNING item. The callee's RETURNING LINKAGE item is not wired to the pointer the caller passes.

Diagnosed: the caller DOES pass the RETURNING target as a trailing BY-REFERENCE arg (CilEmitter.EmitCallProgram
~1280); SemanticBuilder DOES capture the name (`_procedureReturningName` in VisitProcedureDivision) →
Compilation.SetProcedureReturningItem. Hypothesis: the callee never binds the RETURNING item to that extra arg.
Tried appending `ProcedureReturningItem.Name` to `module.UsingParameterNames` (so the entry body maps
args[count] → its _linkage field) — STILL crashes bufferLength=0. So the deeper gap is the RETURNING LINKAGE
item's **location resolution** not using its _linkage pointer (or the caller pushing a 0-length area). That needs
focused debugging (CilLocationEmitter linkage-vs-own-storage), not a tail-of-session fix.

Reverted all CALL-RETURNING changes (CBL3304 restored; the Binder append; the test; a dialect-aware
`CompileMultipleAndRun(DialectMode, …)` overload) via `git checkout` to keep the tree at the SORT-Format-2 commit.
Full diagnosis recorded in docs/SPEC_FIX_RECIPES_M1.md (#9). Moving M2 to #10 READ PREVIOUS (runtime-only boundary
fix). No compiler change committed; guard unaffected (HEAD = 5125b8e). Lesson: an M2 recipe written from a
read-only audit can under-estimate a runtime gap — verify the feature end-to-end before assuming "just delete the
check."

