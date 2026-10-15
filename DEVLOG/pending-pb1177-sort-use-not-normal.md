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

- Every format 1 SORT registers itself as the run unit's active format 1 SORT first in its body, with its
  declarative scope (the emitted `__useActive`, or null) (`CobolSort.RegisterFormat1Sort`; `EcEmitter.SortUseScope`,
  set exactly when the scope has the EC-model `__RunUse`).
  It is a new runtime member rather than a parameter added to `CobolSort.Init`: the gate's `RuntimeAbiPinDriftTests`
  caught that changing the shipped `Init` would have been an incompatible runtime change, raising the major and
  stranding every compiled module.
- `__RunUse` hands every DECLARATIVE's action to `CobolSort.AfterUseProcedure`.
- That throws `SortTerminationSignal` for the innermost active format 1 SORT when the action is not a normal
  completion (`DispatchResult.CompletedNotNormally`: a RESUME of either form, or the `NotNormal` fall-off). It carries
  the action for the SORT's own declaratives and `NotNormal` for any other element's.
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

**Review finding R1-1: the rule reaches every element's USE procedure.** The first version answered GR17 only for
the declaratives of the program instance executing the SORT: it matched the declarative's `__useActive` against the
one scope the SORT registered. The reviewer's probe CALLed a contained program from the input procedure; its USE
procedure ended in RESUME AT NEXT STATEMENT, and the run printed `SUB-USE=35 / SUB-AFTER-OPEN / IN-AFTER-CALL /
SORTED=A / SORTED=B / AFTER-SORT`. A containing program's GLOBAL declarative (the first implementer's lead, probe p9)
and an INVOKEd method's declarative behaved the same way. GR17 names "a USE procedure invoked while a format 1 SORT
statement is active", and GR10/GR13 put "all statements that are executed as the result of a transfer of control in
the range" inside the range, so the narrower reading had no text behind it. The finisher widened the mechanism
rather than record that reading as a determination:

- Every format 1 SORT now registers and emits its landing, including one whose own scope has no declaratives. The
  runtime terminates the innermost active one, whichever element's declarative asks.
- Another element's RESUME AT procedure-name names a procedure of an activation the termination ends, so it lands as
  `NotNormal` at the SORT's end (determination in `docs/CONFORMANCE.md` §3).
- An EXIT PROGRAM or GOBACK in another element's USE procedure is that element's return, and it never reaches
  `AfterUseProcedure`. `__RunUse` now notes it with a declining filter on the return signal
  (`CobolSort.UseProcedureExited`). The return completes, and the SORT is terminated in the activator
  (`SortFileTable.AfterActivationReturn`, asked by `ProgramTable.CallProgram` and by the epilogue of a method with
  declaratives). The filter is in every EC-model `__RunUse`, and in the EC-free one from the 2002 edition on, since an
  EC-free declarative still ends not normally by EXIT PROGRAM or GOBACK.
- An activation the signal unwinds still owes what its end owes. The BY REFERENCE copy-out moved into the
  activation's `finally` and runs when it returned or ended this way (`SortTerminationSignal.EndsActivation`, a
  declining filter). An INITIAL program's implicit CANCEL runs too (`ProgramTable.EndInitialActivation`), so its next
  activation can open the same file. A condition that terminates the run unit owes neither.
- `RuntimeAbiPinDriftTests`' boundary hash was re-pinned alone. The callee's copy-out moved into the `finally`, and
  what crosses the boundary (carriers, descriptions, profiles) did not change.

The new goldens are `2002/pb1177_sort_called_use_not_normal` and `2002/pb1177_invoke_sort_use_not_normal`. The
first has seven legs: control, RESUME NEXT, RESUME AT, the output procedure, GOBACK, an INITIAL program run twice,
and the container's GLOBAL declarative for a SORT with no declaratives. The second covers an INVOKEd method: control,
RESUME NEXT and GOBACK. `2002/pb1177_sort_called_goback_ec_free` pins the GOBACK arm in a group with no
exception-condition feature, where `__RunUse` keeps its void form. `SortUseTerminationTests` was rewritten for the widened rule, including the
innermost-of-two case.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261015T215125Z-fbb8c8:
Conformance 6,116/11,713, Unit 33,268/33,673, Characterization 6/36 in leg 1, with both new goldens and all four
`SortUseTerminationTests` passing). The two earlier reds were the citation audit, which read "has no GR17" as a
citation, and the ABI pin above.
