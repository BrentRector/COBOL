## Entry 1084 — 2026-07-28 15:55 PDT — CA21+CA22: a NULL program-pointer CALL raised the wrong condition, and nothing was listening for the right one

Step 4 of the EC super-batch. Two one-line defects that are individually invisible and jointly fatal, which is
why the plan requires them in one commit with one golden.

**The rule.** §14.9.4.4 GR3b names TWO DISTINCT conditions: "If the data item referenced by identifier-1
contains the predefined address NULL, the EC-PROGRAM-PTR-NULL exception condition is set to exist. If the
program cannot be located or identifier-1 references a zero-length item, the EC-PROGRAM-NOT-FOUND exception
condition is set to exist." NULL is the FIRST. GR3g's "invalid program address … undefined" governs a NON-null
bad address, which is why the old code's appeal to it was misplaced.

⚠ **The queue's CA21 prose is a PASTE ERROR** and describes an INITIALIZE program-pointer fix already landed as
CA2 (DEVLOG 996). The real CA21 is this one — flagged in plan §0 and the deep-dive §Risks, and confirmed again
here by reading the spec before the code.

**The two halves, and each is useless alone:**
  · CA21 — `ProgramTable.CallPointer` raised `CobolCallException(…, "EC-PROGRAM-NOT-FOUND")` for a NULL
    pointer, so a `USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL` declarative could never select.
  · CA22 — `EC-PROGRAM-PTR-NULL` was absent from `EcBinder.ProgramNames`, so `QueryFor(BoundCallProgram)`
    reported the CALL as unchecked and `CallEmitter` emitted NO guard around it at all.

**Proved the interlock rather than asserting it:** with CA21 alone (runtime name corrected, binder membership
still missing) the golden terminates abnormally with the right EC NAME but the declarative never runs — the
condition escapes because there is no emitted catch. Both together: HANDLED-PTR-NULL then AFTER.

**No new scaffold.** CALL raises through `CobolCallException`, a channel distinct from `CobolFatalException`,
so this needed no ExceptionState flag, no FatalAmbientGates row and no catalog entry — EC-PROGRAM-PTR-NULL was
already registered Fatal. The emitter side needed nothing either: `EnabledProgramNames` filters on the
`EC-PROGRAM-` prefix, which the new name already satisfies.

**Both arms covered, per the batch's own rule that a fatal golden must assert RESUME-continues AND
no-RESUME-terminates.** The corpus golden `2023/ec_program_ptr_null` takes the RESUME arm (it has to run clean
to be a positive golden); the fatal arm is `ExceptionConditionConformanceTests
.CallPointerNull_Enabled_NoHandler_FatalTerminates`, added beside its existing EC-PROGRAM-NOT-FOUND twin in the
class that already owns this family.

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+RunUnitTermination 406/406.

