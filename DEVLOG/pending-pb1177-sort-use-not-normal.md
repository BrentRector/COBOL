## PB1177: a USE procedure that does not complete normally terminates the active SORT, whoever invoked it

kb/Work PB1177 was an ANALYSIS note: §14.9.40.4 GR17 ("If a USE procedure invoked while a format 1 SORT statement is
active does not complete normally, the SORT statement is terminated") was verified only for a USE procedure invoked
by an IMPLICIT USING/GIVING transfer, whose hook sits in the SORT statement's own body and jumps to its end label.
The other arm, a USE procedure invoked by an EXPLICIT statement of the INPUT or OUTPUT PROCEDURE, had no wiring, and
the note left open whether GR17 or §14.9.33.4 GR2 a) 1. governs it.

**Re-probe.** It diverged. An OPEN of a missing file in the input procedure whose USE procedure ends in RESUME AT
NEXT STATEMENT printed `DECL-RAN 35 / IN-AFTER-OPEN / SORTED=A / SORTED=B / AFTER-SORT`: the procedure ran on, and
the sequence and output phases followed. The same OPEN in the output procedure ran the rest of the procedure.
PB1036's widening (a RETURN in the input procedure raising EC-SORT-MERGE-ACTIVE, its declarative RESUMEing) printed
`IN-AFTER-RETURN` and both records. RESUME AT procedure-name transferred inside the procedure's nested `__Dispatch`,
so the program's remaining statements ran with the SORT still active, and with EC-SORT-MERGE-ACTIVE checking on, the
next SORT was refused and its record vanished.

**Determination.** GR17 governs. GR10 and GR13 put "all statements in declarative procedures that are executed as a
result of the execution of statements in the range" inside the procedure's range, so the USE procedure was invoked
while the SORT was active. Terminating the SORT ends the procedure with it, and §14.9.33.4 GR2 a)'s "unless general
rules associated with the applicable statement specify otherwise" yields to it. RESUME AT NEXT STATEMENT and a
not-normal fall-off land after the SORT, as they do for the implicit transfers. RESUME AT procedure-name is a GO TO
(GR3) taken after the SORT has ended. MERGE has no GR17, so a statement of its output procedure is its own
applicable statement and the MERGE continues. Recorded in `docs/CONFORMANCE.md` §3.

**Fix: one runtime mechanism for every USE procedure the rule reaches.** The raise site of an explicit statement runs
below the procedure's nested dispatch, so no `goto` can reach the statement's end. The question is asked instead at
the one place every selection path reaches a declarative through, `__RunUse`, once the declarative has finished:

- A format 1 SORT registers its declarative scope (the emitted `__useActive`) first in its body (`CobolSort.RegisterUseScope`)
  (`EcEmitter.SortUseScope`, set exactly when the scope has the EC-model `__RunUse`).
- `__RunUse` hands every DECLARATIVE's action to `CobolSort.AfterUseProcedure`.
- That throws `SortTerminationSignal` when the action is not a normal completion
  (`DispatchResult.CompletedNotNormally`: a RESUME of either form, or the `NotNormal` fall-off) and a SORT that
  registered the scope is active.
- The SORT statement's `catch` (`SortEmitter.TerminationLanding`) lands the signal after the `finally` has released
  the store. RESUME AT procedure-name then transfers from the SORT's own dispatcher.

Because the registration covers the whole statement, the implicit transfers go the same way. So does a USE procedure
invoked by another USE procedure while the SORT is active: the SORT ends where the inner one ends. MERGE registers
nothing. `SequentialIoEmitter.EmitUseHook`'s static end-label arm stays for MERGE's "completes normally" rules and
for an exception-checking PERFORM handler's RESUME, which is not a USE procedure.

**A regression the method arm caught.** The first build broke every OO method that held both USE declaratives and a
SORT. The method's guard array is a LOCAL, and its machinery was emitted after `__MDispatch`, so C# refused the
forward reference (CS0841). `OoEmitter` now emits a method's selection machinery before its dispatch, as a program's
already was.

**Witnesses.** The golden `2002/pb1177_sort_procedure_use_not_normal` has eight legs:

1. the control case
2. input procedure, RESUME NEXT
3. input procedure, RESUME AT
4. output procedure, RESUME NEXT
5. the GR1 RESUME-as-CONTINUE fall-off
6. a USE procedure invoked by a USE procedure
7. the MERGE twin
8. the store released, with -ACTIVE checking on

Also `2002/pb1177_method_sort_use_not_normal` (the method arm), `unit:SortUseTerminationTests`, and a
`CompletedNotNormally` column in `DispatchResultProtocolDriftTests`. `pb367b_sort_use_not_normal`,
`pb1122_declarative_fatal_in_scope_merge` and `pb1036_sort_merge_statement_ec` were re-run unchanged. Row
`GR-14.9.40.4-17` went from PARTIAL to CONFORMS, and the GAP from 101 to 100.

**Left as a lead.** A USE GLOBAL declarative of a CONTAINING program runs in the container's instance through
`__RunGlobalUse`, which returns only "ran". When one is invoked from a contained program's SORT procedure and does
not complete normally (GR1 RESUME-as-CONTINUE), the contained SORT is not terminated.
