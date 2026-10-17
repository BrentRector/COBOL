*>--- conformance-case
*> id: pb432-varying-operand-rules
*> rules: [SR-14.9.28.3-4, SR-14.9.28.3-5, SR-14.9.28.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB432 — ISO §14.9.28.3 SR6: "The literal in the BY phrase shall not be zero." It is
      *> unconditional and it is the one operand rule whose absence changed a program's OUTCOME rather than
      *> its legality: §14.9.28.4 GR12 makes the augment value zero, so the induction variable never changes
      *> and the UNTIL condition can never become true through the phrase — a guaranteed non-terminating loop.
      *> This program used to compile at every --std; it only ever returned because of the counter guard below,
      *> which fired with N = 11 while the loop was still running.
      *> The other six lettered obligations of SR4 and SR5 report the same COBOLNET2120 with their own rule
      *> named; the positive coverage of every legal operand shape is tests/conformance/85/
      *> pb432_varying_brace_group.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB432NEGSR6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 0.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM 1 BY 0 UNTIL I > 5
               ADD 1 TO N
               IF N > 10
                   EXIT PERFORM
               END-IF
           END-PERFORM
           STOP RUN.
