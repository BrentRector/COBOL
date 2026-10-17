      *>--- conformance-case
      *> id: pb1177_sort_called_use_not_normal
      *> rules: [GR-14.9.40.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1177 - 14.9.40.4 GR17 for a USE procedure of ANOTHER
      *> runtime element: "If a USE procedure invoked while a format 1
      *> SORT statement is active does not complete normally, the SORT
      *> statement is terminated." (pb1177_sort_procedure_use_not_normal
      *> pins the SORT's own USE procedures, pb1177_invoke_sort_use_
      *> not_normal a method INVOKEd from the range.)
      *>
      *> THE RULES, LINE BY LINE:
      *>  . 14.9.40.4 GR10 / GR13: the range of the input (output)
      *>    procedure "includes all statements that are executed as the
      *>    result of a transfer of control in the range ..., as well as
      *>    all statements in declarative procedures that are executed
      *>    as a result of the execution of statements in the range" -
      *>    a CALLed program and the USE procedures its statements
      *>    invoke run while the SORT is active.
      *>  . 14.6.13.1.2 1): a declarative does not complete normally
      *>    when "An EXIT PROGRAM, GOBACK, RESUME, or STOP statement
      *>    ... is executed, or a fatal exception occurs within the
      *>    scope of the declarative". 14.9.33.4 GR1 makes a RESUME in
      *>    the scope of a global declarative a CONTINUE - executed all
      *>    the same.
      *>  . GR17 then terminates the SORT: nothing more of its range -
      *>    the rest of the CALLed program, the input procedure, the
      *>    sequence phase, the output procedure - runs, and control
      *>    goes to the end of the SORT statement (=> Ln-AFTER).
      *>  . 14.2.3 GR8: a BY REFERENCE formal "occupies the same
      *>    storage area as the argument", so what the CALLed program
      *>    stored before the failing OPEN is the activator's
      *>    (=> Ln-ARG=Y).
      *>  . DETERMINATION (docs/CONFORMANCE.md 3, kb/Work PB1177): the
      *>    CALLed program's RESUME AT procedure-name names a procedure
      *>    of an activation the termination has ended, so it is not
      *>    taken (L2); a GOBACK in its USE procedure returns to the
      *>    activator, and the SORT is terminated there (L4).
      *>
      *> THE LEGS (the expected output is computed from the rules
      *> above, not from a run):
      *>  L0  control - the CALLed program's USE procedure completes
      *>      normally: L0-SUB-USE=35 L0-SUB-AFTER-OPEN L0-IN-AFTER-CALL
      *>      L0-OUT=A L0-OUT=B L0-ARG=Y L0-AFTER.
      *>  L1  input procedure, RESUME AT NEXT STATEMENT => L1-SUB-USE=35
      *>      L1-ARG=Y L1-AFTER.
      *>  L2  input procedure, RESUME AT procedure-name => L2-SUB-USE=35
      *>      L2-ARG=Y L2-AFTER (never L2-SUB-LANDED).
      *>  L3  output procedure, RESUME AT NEXT STATEMENT => L3-OUT=A
      *>      L3-SUB-USE=35 L3-ARG=Y L3-AFTER.
      *>  L4  input procedure, GOBACK => L4-SUB-USE=35 L4-ARG=Y
      *>      L4-AFTER.
      *>  L5  an INITIAL program (twice): L5-INI-OPEN=00 L5-INI-USE=35
      *>      L5-AFTER - its activation ended with the SORT's range, and
      *>      the next activation opens the same file again
      *>      (14.9.18.4 GR2 closed it).
      *>  L6  the containing program's GLOBAL USE procedure, for a
      *>      contained program's SORT with no declaratives of its own
      *>      (14.9.49.4 GR4 b)) => L6-G-USE=35 L6-G-END L6-AFTER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1177x-sort-wk.dat".
           SELECT NG ASSIGN TO "pb1177x-absent-g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FSG.
           SELECT NX ASSIGN TO "pb1177x-absent-x.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  SW-REC.
           05 SW-KEY PIC X.
       FD  NG IS GLOBAL.
       01  NG-REC PIC X.
       FD  NX.
       01  NX-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FSG PIC XX IS GLOBAL.
       01  W-EOF PIC X.
       01  W-LEG PIC 9.
       01  W-ARG.
           05 W-ARG-1 PIC X.
           05 W-ARG-N PIC 9(3) COMP.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-G SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON NG.
       D-G-P.
           DISPLAY "L6-G-USE=" FSG.
           PERFORM D-X-P.
           DISPLAY "L6-G-END".
       D-X SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NX.
       D-X-P.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM RUN-LEG VARYING W-LEG FROM 0 BY 1 UNTIL W-LEG > 5.
           MOVE 5 TO W-LEG.
           PERFORM RUN-LEG.
           CALL "PB1177XG".
           STOP RUN.
       RUN-LEG.
           MOVE "N" TO W-ARG-1.
           EVALUATE W-LEG
             WHEN 3
               SORT SW ON ASCENDING KEY SW-KEY
                   INPUT PROCEDURE IS IN-PLAIN
                   OUTPUT PROCEDURE IS OUT-CALL
             WHEN 5
               SORT SW ON ASCENDING KEY SW-KEY
                   INPUT PROCEDURE IS IN-INI
                   OUTPUT PROCEDURE IS OUT-P
             WHEN OTHER
               SORT SW ON ASCENDING KEY SW-KEY
                   INPUT PROCEDURE IS IN-CALL
                   OUTPUT PROCEDURE IS OUT-P
           END-EVALUATE.
           IF W-LEG NOT = 5
               DISPLAY "L" W-LEG "-ARG=" W-ARG-1
           END-IF.
           DISPLAY "L" W-LEG "-AFTER".
       IN-CALL.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           CALL "PB1177XS" USING W-LEG W-ARG.
           DISPLAY "L" W-LEG "-IN-AFTER-CALL".
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-PLAIN.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-INI.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           CALL "PB1177XI".
           DISPLAY "L5-IN-AFTER-CALL".
       OUT-P.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SW AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "L" W-LEG "-OUT=" SW-KEY
               END-RETURN
           END-PERFORM.
       OUT-CALL.
           RETURN SW AT END CONTINUE
               NOT AT END DISPLAY "L" W-LEG "-OUT=" SW-KEY
           END-RETURN.
           CALL "PB1177XS" USING W-LEG W-ARG.
           DISPLAY "L" W-LEG "-OUT-AFTER-CALL".
           RETURN SW AT END CONTINUE
               NOT AT END DISPLAY "L" W-LEG "-OUT=" SW-KEY
           END-RETURN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177XS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb1177x-absent-s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  NF.
       01  NF-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       LINKAGE SECTION.
       01  L-LEG PIC 9.
       01  L-ARG.
           05 L-ARG-1 PIC X.
           05 L-ARG-N PIC 9(3) COMP.
       PROCEDURE DIVISION USING L-LEG L-ARG.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NF-P.
           DISPLAY "L" L-LEG "-SUB-USE=" FS.
           EVALUATE L-LEG
             WHEN 1
             WHEN 3
               RESUME AT NEXT STATEMENT
             WHEN 2
               RESUME AT S-LANDED
             WHEN 4
               GOBACK
           END-EVALUATE.
       END DECLARATIVES.
       S-MAIN SECTION.
       S-1.
           MOVE "Y" TO L-ARG-1.
           OPEN INPUT NF.
           DISPLAY "L" L-LEG "-SUB-AFTER-OPEN".
           GOBACK.
       S-LANDED.
           DISPLAY "L" L-LEG "-SUB-LANDED".
           GOBACK.
       END PROGRAM PB1177XS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177XI IS INITIAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OKF ASSIGN TO "pb1177x-initial.tmp"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-OK.
           SELECT NF5 ASSIGN TO "pb1177x-absent-5.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS5.
       DATA DIVISION.
       FILE SECTION.
       FD  OKF.
       01  OKF-REC PIC X.
       FD  NF5.
       01  NF5-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS-OK PIC XX.
       01  FS5 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-5 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF5.
       D-5-P.
           DISPLAY "L5-INI-USE=" FS5.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       I-MAIN SECTION.
       I-1.
           OPEN OUTPUT OKF.
           DISPLAY "L5-INI-OPEN=" FS-OK.
           OPEN INPUT NF5.
           DISPLAY "L5-INI-AFTER-OPEN".
           CLOSE OKF.
           GOBACK.
       END PROGRAM PB1177XI.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177XG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SG ASSIGN TO "pb1177x-sort-g.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SG.
       01  SG-REC PIC X.
       WORKING-STORAGE SECTION.
       01  G-EOF PIC X.
       PROCEDURE DIVISION.
       G-1.
           SORT SG ON ASCENDING KEY SG-REC
               INPUT PROCEDURE IS G-IN
               OUTPUT PROCEDURE IS G-OUT.
           DISPLAY "L6-AFTER".
           GOBACK.
       G-IN.
           MOVE "B" TO SG-REC.
           RELEASE SG-REC.
           OPEN INPUT NG.
           DISPLAY "L6-IN-AFTER-OPEN".
       G-OUT.
           MOVE "N" TO G-EOF.
           PERFORM UNTIL G-EOF = "Y"
               RETURN SG AT END MOVE "Y" TO G-EOF
                   NOT AT END DISPLAY "L6-OUT=" SG-REC
               END-RETURN
           END-PERFORM.
       END PROGRAM PB1177XG.
       END PROGRAM PB1177X.